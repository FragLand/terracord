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
using System.Threading.Tasks;
using TShockAPI;

namespace Terracord
{
  class Discord
  {
    // Discord bot client
    public DiscordSocketClient Client { get; }
    // Discord relay channel
    private IMessageChannel channel;

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
        await Client.LoginAsync(TokenType.Bot, Config.BotToken);
        await Client.StartAsync();
      }
      catch(Exception e)
      {
        Util.Log($"Unable to connect to Discord: {e.Message}", Util.Severity.Error);
        if(Config.AbortOnError)
          Environment.Exit(Util.ExitFailure);
      }

      try
      {
        // Set game/playing status
        await Client.SetGameAsync(Config.BotGame);
      }
      catch(Exception e)
      {
        Util.Log($"Unable to set game/playing status: {e.Message}", Util.Severity.Error);
        if(Config.AbortOnError)
          Environment.Exit(Util.ExitFailure);
      }

      // Block task until program termination
      //await Task.Delay(-1);
      // Do not block since it prevents TShock console input when this method is called asynchronously
      await Task.CompletedTask;
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
      }

      // The message below is sent to Discord every time the bot connects/reconnects
      Util.Log($"Relay available. Connected to Discord as {Client.CurrentUser.ToString()}.", Util.Severity.Info);
      Send("**:white_check_mark: Relay available.**");
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

        // Relay Discord message to Terraria players
        if(Config.LogChat)
          Util.Log($"<{message.Author.Username}@Discord> {message.Content}", Util.Severity.Info);
        TShock.Utils.Broadcast($"<{message.Author.Username}@Discord> {message.Content}", Config.BroadcastColor[0], Config.BroadcastColor[1], Config.BroadcastColor[2]);
      }
      catch(Exception e)
      {
        Util.Log($"Unable to broadcast TShock message: {e.Message}", Util.Severity.Error);
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
      }
    }
  }
}
