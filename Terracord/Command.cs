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
using System.Collections.Generic;
using System.Threading.Tasks;
using TShockAPI;

namespace FragLand.TerracordPlugin
{
  class Command
  {
    // This class is instanced as a TShock client object that issues commands
    class TerracordPlayer:TSPlayer
    {
      public List<string> Output = new List<string>();
      public TerracordPlayer(string player):base(player){}
      public override void SendMessage(string msg, byte red, byte green, byte blue)
      {
        Output.Add(msg); // command output
      }
    }

    /// <summary>
    /// Handles Discord commands
    /// </summary>
    /// <param name="user">user issuing the command</param>
    /// <param name="channel">Discord text channel</param>
    /// <param name="command">command sent by a Discord user</param>
    /// <returns>void</returns>
    public static async Task CommandHandler(SocketUser user, IMessageChannel channel, string command)
    {
      command = command.Substring(Config.CommandPrefix.Length).TrimStart(); // remove command prefix
      Util.Log($"Command sent: {command}", Util.Severity.Info);

      if(command.Equals("help", StringComparison.OrdinalIgnoreCase))
        await CommandResponse(channel, "Help", Help()).ConfigureAwait(true);
      else if(command.Equals("playerlist", StringComparison.OrdinalIgnoreCase))
        await CommandResponse(channel, "Player List", PlayerList()).ConfigureAwait(true);
      else if(command.Equals("serverinfo", StringComparison.OrdinalIgnoreCase))
        await CommandResponse(channel, "Server Information", ServerInfo()).ConfigureAwait(true);
      else if(command.Equals("uptime", StringComparison.OrdinalIgnoreCase))
        await CommandResponse(channel, "Uptime", Uptime()).ConfigureAwait(true);
      else // let TShock attempt to handle the command
      {
        if(Config.RemoteCommands)
          _ = ExecuteTShockCommand(user, channel, command);
      }

      await Task.CompletedTask.ConfigureAwait(true);
    }

    /// <summary>
    /// Attempts to execute a TShock command sent from Discord
    /// </summary>
    /// <param name="user">user issuing the command</param>
    /// <param name="channel">Discord text channel</param>
    /// <param name="command">command sent by a Discord user</param>
    /// <returns>void</returns>
    private static async Task ExecuteTShockCommand(SocketUser user, IMessageChannel channel, string command)
    {
      if(user.Id == Config.OwnerId || Util.AuthorizedUser(user)) // check if user is authorized
      {
        TerracordPlayer terracordPlayer = new TerracordPlayer(user.Username){Group = new SuperAdminGroup()};
        //if(Commands.HandleCommand(TSPlayer.Server, $"{TShock.Config.CommandSpecifier}{command}"))
        if(Commands.HandleCommand(terracordPlayer, $"{TShock.Config.CommandSpecifier}{command}"))
        {
          foreach(string commandOutput in terracordPlayer.Output)
            await CommandResponse(channel, "Command Status", $"Command output: {commandOutput}", Color.Green).ConfigureAwait(true);
        }
        else
        {
          await CommandResponse(channel, "Command Status", $"Failed to execute: {command}", Color.Red).ConfigureAwait(true);
        }
      }
      else
        await CommandResponse(channel, "Command Status", $"Access denied for: {command}", Color.Red).ConfigureAwait(true);

      await Task.CompletedTask.ConfigureAwait(true);
    }

    /// <summary>
    /// Provides command help
    /// </summary>
    /// <returns>command details</returns>
    public static string Help()
    {
      string commandList = "__**Commands**__\n" +
                           "**help**       - Display command list\n" +
                           "**playerlist** - Display online players\n" +
                           "**serverinfo** - Display server details\n" +
                           "**uptime**     - Display plugin uptime\n\n";
      return commandList;
    }

    /// <summary>
    /// Provides player list
    /// </summary>
    /// <returns>player count and list of players</returns>
    public static string PlayerList()
    {
      string playerList = $"{TShock.Utils.GetActivePlayerCount()}/{TShock.Config.MaxSlots}\n\n";
      //foreach(var player in TShock.Utils.GetPlayers(false))
      //  playerList += $"{player.Name}\n";
      foreach(TSPlayer player in TShock.Players)
      {
        if(player != null && player.Active)
          playerList += $"{player.Name}\n";
      }
      return playerList;
    }

    /// <summary>
    /// Provides server information
    /// </summary>
    /// <returns>server information</returns>
    public static string ServerInfo()
    {
      return $"**Server Name:** {TShock.Config.ServerName}\n" +
             $"**Players:** {TShock.Utils.GetActivePlayerCount()}/{TShock.Config.MaxSlots}\n" +
             $"**TShock Version:** {TShock.VersionNum}";
    }

    /// <summary>
    /// Calculates plugin uptime
    /// </summary>
    /// <returns>uptime as a string</returns>
    public static string Uptime()
    {
      TimeSpan elapsed = DateTime.Now.Subtract(Terracord.startTime);
      return $"{elapsed.Days} day(s), {elapsed.Hours} hour(s), {elapsed.Minutes} minute(s), and {elapsed.Seconds} second(s)";
    }

    /// <summary>
    /// Responds to commands
    /// </summary>
    /// <param name="channel">Discord text channel</param>
    /// <param name="title">command title</param>
    /// <param name="description">command output</param>
    /// <param name="color">embed color</param>
    /// <returns>void</returns>
    private static async Task CommandResponse(IMessageChannel channel, string title, string description, Color? color = null)
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
  }
}
