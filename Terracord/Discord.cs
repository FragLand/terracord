/*
 * Terracord.cs - A Discord <-> Terraria bridge plugin for TShock
 * Copyright (C) 2019-2020 Lloyd Dilley
 * http://www.frag.land/
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License along
 * with this program; if not, write to the Free Software Foundation, Inc.,
 * 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using Discord;
using Discord.WebSocket;
using System;
using System.Text;
using System.Threading.Tasks;
using TShockAPI;

namespace FragLand.TerracordPlugin
{
  class Discord
  {
    // Discord bot client
    public DiscordSocketClient Client { get; }
    // Discord relay channel
    private IMessageChannel channel;
    // Topic thread running?
    public bool UpdateTopicRunning { get; private set; }

    public Discord()
    {
      // Set AlwaysDownloadUsers to update Discord server member list for servers containing >=100 members
      // This is required to properly support mentions in more populated servers
      if(Config.DebugMode)
      {
        Client = new DiscordSocketClient(new DiscordSocketConfig
        {
          AlwaysDownloadUsers = true,
          LogLevel = LogSeverity.Debug
        });
      }
      else
      {
        Client = new DiscordSocketClient(new DiscordSocketConfig
        {
          AlwaysDownloadUsers = true,
          LogLevel = LogSeverity.Info
        });
      }
      Client.Log += Log;
      Client.Ready += Ready;
      Client.MessageReceived += MessageReceived;
      UpdateTopicRunning = false;
    }

    /// <summary>
    /// Connects the bot to the Discord network
    /// </summary>
    /// <returns>void</returns>
    public async Task Connect()
    {
      Util.Log("Connecting to Discord...", Util.Severity.Info);
      try
      {
        // Connect to Discord
        await Client.LoginAsync(TokenType.Bot, Config.BotToken).ConfigureAwait(true);
        await Client.StartAsync().ConfigureAwait(true);
      }
      catch(Exception e)
      {
        Util.HandleFatalError($"Unable to connect to Discord: {e.Message}");
        throw;
      }

      try
      {
        // Set game/playing status
        await Client.SetGameAsync(Config.BotGame).ConfigureAwait(true);
      }
      catch(Exception e)
      {
        Util.HandleFatalError($"Unable to set game/playing status: {e.Message}");
        throw;
      }

      // Block task until program termination
      //await Task.Delay(-1);
      // Do not block since it prevents TShock console input when this method is called asynchronously
      await Task.CompletedTask.ConfigureAwait(true);
    }

    /// <summary>
    /// Called when a Discord.Net message requires logging
    /// </summary>
    /// <returns>Task.CompletedTask</returns>
    private Task Log(LogMessage message)
    {
      // Consolidate Discord.Net LogSeverity with Terracord Util.Severity
      Util.Severity severity;
      switch(message.Severity)
      {
        case LogSeverity.Debug:
        case LogSeverity.Verbose:
          severity = Util.Severity.Debug;
          break;
        case LogSeverity.Info:
          severity = Util.Severity.Info;
          break;
        case LogSeverity.Warning:
          severity = Util.Severity.Warning;
          break;
        case LogSeverity.Error:
        case LogSeverity.Critical:
          severity = Util.Severity.Error;
          break;
        default:
          severity = Util.Severity.Info;
          break;
      }
      Util.Log(message.ToString(), severity);
      return Task.CompletedTask;
    }

    /// <summary>
    /// Called after the bot establishes the connection to Discord
    /// </summary>
    /// <returns>Task.CompletedTask</returns>
    private Task Ready()
    {
      try
      {
        channel = Client.GetChannel(Config.ChannelId) as IMessageChannel;
      }
      catch(Exception e)
      {
        Util.Log($"Unable to acquire Discord channel: {e.Message}", Util.Severity.Error);
        throw;
      }

      if(!UpdateTopicRunning)
        _ = UpdateTopic(); // fire and forget topic update thread

      // The message below is sent to Discord every time the bot connects/reconnects
      Util.Log($"Relay available. Connected to Discord as {Client.CurrentUser.ToString()}.", Util.Severity.Info);
      Send(Properties.Strings.RelayAvailableString);
      return Task.CompletedTask;
    }

    /// <summary>
    /// Called when a new message is received by the Discord bot
    /// </summary>
    /// <param name="message">message received by Discord bot</param>
    /// <returns>Task.CompletedTask</returns>
    private Task MessageReceived(SocketMessage message)
    {
      try
      {
        // Only accept messages from configured Discord text channel
        if(message.Channel.Id != Config.ChannelId)
          return Task.CompletedTask;

        // Do not send duplicates messages from Discord bot to Terraria players
        if(message.Author.Id == Client.CurrentUser.Id)
          return Task.CompletedTask;

        // Handle commands
        if(message.Content.StartsWith(Config.CommandPrefix.ToString(Config.Locale), StringComparison.InvariantCulture) && message.Content.Length > 1)
          _ = CommandHandler(message.Content); // avoid blocking in MessageReceived() by using discard

        // Check for mentions and convert them to friendly names if found
        string messageContent = ConvertMentions(message);

        // Relay Discord message to Terraria players
        if(Config.LogChat)
          Util.Log($"<{message.Author.Username}@Discord> {messageContent}", Util.Severity.Info);
        TShock.Utils.Broadcast($"<{message.Author.Username}@Discord> {messageContent}", Config.BroadcastColor[0], Config.BroadcastColor[1], Config.BroadcastColor[2]);
      }
      catch(Exception e)
      {
        Util.Log($"Unable to broadcast TShock message: {e.Message}", Util.Severity.Error);
        throw;
      }

      return Task.CompletedTask;
    }

    /// <summary>
    /// Sends a message to a Discord channel
    /// </summary>
    /// <param name="message">message to send to Discord channel</param>
    public void Send(string message)
    {
      try
      {
        // channel? checks if object is null prior to sending message
        channel?.SendMessageAsync(message);
      }
      catch(Exception e)
      {
        Util.Log($"Unable to send Discord message: {e.Message}", Util.Severity.Error);
        throw;
      }
    }

    /// <summary>
    /// Handles Discord commands
    /// </summary>
    /// <param name="command">command sent by a Discord user</param>
    private async Task CommandHandler(string command)
    {
      command = command.Substring(1); // remove command prefix
      Util.Log($"Command sent: {command}", Util.Severity.Info);

      if(command.Equals("playerlist", StringComparison.OrdinalIgnoreCase))
      {
        string playerList = $"{TShock.Utils.ActivePlayers()}/{TShock.Config.MaxSlots}\n\n";
        foreach(var player in TShock.Utils.GetPlayers(false))
          playerList += $"{player}\n";
        await CommandResponse("Player List", playerList).ConfigureAwait(true);
      }

      if(command.Equals("serverinfo", StringComparison.OrdinalIgnoreCase))
        await CommandResponse("Server Information", 
                              $"**Server Name:** {TShock.Config.ServerName}\n**Players:** {TShock.Utils.ActivePlayers()}/{TShock.Config.MaxSlots}\n**TShock Version:** {TShock.VersionNum.ToString()}")
                              .ConfigureAwait(true);

      if(command.Equals("uptime", StringComparison.OrdinalIgnoreCase))
        //Send($"**__Uptime__**\n```\n{Util.Uptime()}\n```");
        await CommandResponse("Uptime", Util.Uptime()).ConfigureAwait(true);

      await Task.CompletedTask.ConfigureAwait(true);
    }

    /// <summary>
    /// Responds to commands
    /// </summary>
    /// <param name="title">command title</param>
    /// <param name="description">command output</param>
    /// <param name="color">embed color</param>
    /// <returns>void</returns>
    private async Task CommandResponse(string title, string description, Color? color = null)
    {
      try
      {
        Color embedColor = color ?? Color.Blue;
        await channel.TriggerTypingAsync().ConfigureAwait(true);
        await Task.Delay(1500).ConfigureAwait(true); // pause for 1.5 seconds
        EmbedBuilder embed = new EmbedBuilder();
        embed.WithColor(embedColor)
          .WithDescription(description)
          .WithFooter(footer => footer.Text = $"Terracord {Terracord.PluginVersion}")
          .WithCurrentTimestamp()
          .WithTitle(title);
        await channel.SendMessageAsync("", false, embed.Build()).ConfigureAwait(true);
      }
      catch(Exception e)
      {
        Util.Log($"Unable to send command response: {e.Message}", Util.Severity.Error);
        throw;
      }

      await Task.CompletedTask.ConfigureAwait(true);
    }

    /// <summary>
    /// Converts channel, role, and user mentions to friendly names before being broadcasted to TShock players
    /// </summary>
    /// <param name="message">message received by Discord bot</param>
    /// <returns>modified message text</returns>
    private static string ConvertMentions(SocketMessage message)
    {
      StringBuilder modifiedMessageText = new StringBuilder(message.Content);

      if(message.MentionedChannels.Count > 0)
      {
        foreach(var channel in message.MentionedChannels)
          modifiedMessageText = modifiedMessageText.Replace($"<#{channel.Id}>", $"#{channel.Name}");
      }

      if(message.MentionedRoles.Count > 0)
      {
        foreach(var role in message.MentionedRoles)
          modifiedMessageText = modifiedMessageText.Replace($"<@&{role.Id}>", $"@{role.Name}");
      }

      if(message.MentionedUsers.Count > 0)
      {
        foreach(var user in message.MentionedUsers)
          modifiedMessageText = modifiedMessageText.Replace($"<@!{user.Id}>", $"@{user.Username}");
      }

      return modifiedMessageText.ToString();
    }

    /// <summary>
    /// Periodically updates Discord channel topic
    /// </summary>
    /// <returns>void</returns>
    private async Task UpdateTopic()
    {
      ITextChannel topicChannel = Client.GetChannel(Config.ChannelId) as ITextChannel;
      UpdateTopicRunning = true;
      while(true)
      {
        await topicChannel.ModifyAsync(chan =>
        {
          chan.Topic = $"{TShock.Utils.ActivePlayers()}/{TShock.Config.MaxSlots} players online " +
                       $"| Server online for {Util.Uptime()} | Last update: {DateTime.Now.ToString(Config.TimestampFormat, Config.Locale)}";
        }).ConfigureAwait(true);
        try
        {
          await Task.Delay(Convert.ToInt32(Config.TopicInterval * 1000)).ConfigureAwait(true); // seconds to milliseconds
        }
        catch(OverflowException oe)
        {
          Util.Log($"Topic interval value exceeds limit: {oe.Message}", Util.Severity.Error);
          throw;
        }
      }
    }
  }
}
