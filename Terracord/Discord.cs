/*
 * Terracord.cs - A Discord <-> Terraria bridge plugin for TShock
 * Copyright (C) 2019-2021 Lloyd Dilley
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
    // Tracks the number of connections to the Discord server during lifetime of plugin
    private static uint connectionCounter = 0;

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
        string status = Config.BotGame.Replace("$server_name", TShock.Config.ServerName);
        status = status.Replace("$player_count", TShock.Utils.GetActivePlayerCount().ToString());
        status = status.Replace("$player_slots", TShock.Config.MaxSlots.ToString());
        await Client.SetGameAsync(status).ConfigureAwait(true);
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

      // Only announce reconnections if configured to do so
      connectionCounter++;
      if(Config.AnnounceReconnect || connectionCounter <= 1)
      {
        Util.Log($"Relay available. Connected to Discord as {Client.CurrentUser}.", Util.Severity.Info);
        Send(Config.AvailableText);
      }
      return Task.CompletedTask;
    }

    /// <summary>
    /// Preprocess Discord messages prior to broadcasting to Terraria players
    /// </summary>
    /// <param name="message">message received by Discord bot</param>
    /// <returns>true if message should be broadcasted to Terraria players or false if not</returns>
    private bool PreprocessMessage(SocketMessage message, ref string messageContent)
    {
      // Check for null or empty messages
      if(message == null || (String.IsNullOrEmpty(message.Content) && message.Attachments.Count == 0))
        return false;

      // Only accept messages from configured Discord text channel
      if(message.Channel.Id != Config.ChannelId)
        return false;

      // Do not send duplicates messages from Discord bot to Terraria players
      if(message.Author.Id == Client.CurrentUser.Id)
        return false;

      // Handle commands
      if(message.Content.StartsWith(Config.CommandPrefix.ToString(Config.Locale), StringComparison.InvariantCulture) && message.Content.Length > Config.CommandPrefix.Length)
      {
        _ = Command.CommandHandler(Client, message.Author, channel, message.Content); // avoid blocking in MessageReceived() by using discard
        if(!Config.RelayCommands)
          return false;
      }

      // Do not relay Discord chat to players if this option is enabled
      if(Config.IgnoreChat)
        return false;

      // Check for mentions and convert them to friendly names if found
      messageContent = Util.ConvertMentions(message);

      // Check for emojis/emotes and convert them if necessary
      messageContent = Util.ConvertEmotes(messageContent);

      // Check for newlines/carriage returns and replace them to prevent multi-line broadcasts without a Discord author prefixed (TShock 4.4 broadcast fix)
      messageContent = Util.FixMultiline(messageContent);

      // Check for attachments
      if(message.Attachments.Count > 0)
        messageContent = Util.CheckMessageAttachments(message, messageContent);

      // Truncate messages that exceed allowed threshold
      if(Config.MessageLength > 0 && messageContent.Length > Config.MessageLength)
        messageContent = messageContent.Substring(0, (Config.MessageLength - 1));

      return true;
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
        string messageContent = null;
        bool relayMessage = PreprocessMessage(message, ref messageContent);
        // Relay Discord message to Terraria players
        if(relayMessage)
        {
          string text = "";
          text = Config.ChatText.Replace("$user_name", message.Author.Username);
          text = text.Replace("$message", messageContent);
          if(Config.LogChat)
            Util.Log(text, Util.Severity.Info);
          TShock.Utils.Broadcast(text, Config.BroadcastColor[0], Config.BroadcastColor[1], Config.BroadcastColor[2]);
        }
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
    /// Sets the Discord text channel topic
    /// </summary>
    /// <param name="topic">new channel topic</param>
    /// <returns>void</returns>
    public async Task SetTopic(string topic)
    {
      ITextChannel topicChannel = Client.GetChannel(Config.ChannelId) as ITextChannel;
      await topicChannel.ModifyAsync(chan =>
      {
        chan.Topic = topic;
      }).ConfigureAwait(true);
    }

    /// <summary>
    /// Periodically updates Discord channel topic
    /// </summary>
    /// <returns>void</returns>
    private async Task UpdateTopic()
    {
      UpdateTopicRunning = true;
      while(true)
      {
        await SetTopic($"{TShock.Config.ServerName} | {TShock.Utils.GetActivePlayerCount()}/{TShock.Config.MaxSlots} players online " +
                       $"| Server online for {Command.Uptime()} | Last update: {DateTime.Now.ToString(Config.TimestampFormat, Config.Locale)}").ConfigureAwait(true);
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

    /// <summary>
    /// Set bot playing status
    /// </summary>
    /// <param name="status">new bot status</param>
    /// <returns>void</returns>
    public static async Task UpdateBotGame(DiscordSocketClient client, string status)
    {
      await client.SetGameAsync(status).ConfigureAwait(true);
    }
  }
}
