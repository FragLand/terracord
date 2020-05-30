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
using System;
using System.Threading.Tasks;
using TShockAPI;

namespace FragLand.TerracordPlugin
{
  class Command
  {
    private const string CommandList = "annoy ban broadcast confuse firework give godmode heal kick kill mute reload restart rocket save slap stop warp whisper";

    /// <summary>
    /// Handles Discord commands
    /// </summary>
    /// <param name="userId">ID of user issuing the command</param>
    /// <param name="channel">Discord text channel</param>
    /// <param name="command">command sent by a Discord user</param>
    /// <returns>void</returns>
    public static async Task CommandHandler(ulong userId, IMessageChannel channel, string command)
    {
      command = command.Substring(1); // remove command prefix
      Util.Log($"Command sent: {command}", Util.Severity.Info);

      if(command.Equals("playerlist", StringComparison.OrdinalIgnoreCase))
      {
        string playerList = $"{TShock.Utils.GetActivePlayerCount()}/{TShock.Config.MaxSlots}\n\n";
        //foreach(var player in TShock.Utils.GetPlayers(false))
        //  playerList += $"{player.Name}\n";
        foreach(TSPlayer player in TShock.Players)
        {
          if(player != null && player.Active)
            playerList += $"{player.Name}";
        }
        await CommandResponse(channel, "Player List", playerList).ConfigureAwait(true);
      }
      else if(command.Equals("serverinfo", StringComparison.OrdinalIgnoreCase))
        await CommandResponse(channel, "Server Information",
                              $"**Server Name:** {TShock.Config.ServerName}\n**Players:** {TShock.Utils.GetActivePlayerCount()}/{TShock.Config.MaxSlots}\n**TShock Version:** {TShock.VersionNum.ToString()}")
                              .ConfigureAwait(true);
      else if(command.Equals("uptime", StringComparison.OrdinalIgnoreCase))
        //Send($"**__Uptime__**\n```\n{Util.Uptime()}\n```");
        await CommandResponse(channel, "Uptime", Util.Uptime()).ConfigureAwait(true);
      else // let TShock attempt to handle the command
        _ = ExecuteTShockCommand(userId, channel, command);

      await Task.CompletedTask.ConfigureAwait(true);
    }

    /// <summary>
    /// Attempts to execute a TShock command sent from Discord
    /// </summary>
    /// <param name="userId">ID if user issuing the command</param>
    /// <param name="channel">Discord text channel</param>
    /// <param name="command">command sent by a Discord user</param>
    /// <returns>void</returns>
    private static async Task ExecuteTShockCommand(ulong userId, IMessageChannel channel, string command)
    {
      if(CommandList.Contains(command.Split(' ')[0].ToLower(Config.Locale))) // check if command is valid
      {
        if(userId == Config.OwnerId) // check if user is authorized
        {
          if(Commands.HandleCommand(TSPlayer.Server, $"{TShock.Config.CommandSpecifier}{command}"))
            await CommandResponse(channel, "Command Status", $"Remotely executed: {command}", Color.Green).ConfigureAwait(true);
          else
            await CommandResponse(channel, "Command Status", $"Failed to execute: {command}", Color.Red).ConfigureAwait(true);
        }
        else
          await CommandResponse(channel, "Command Status", $"Access denied for: {command}", Color.Red).ConfigureAwait(true);
      }

      await Task.CompletedTask.ConfigureAwait(true);
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
