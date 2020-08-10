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

using System;
using System.Threading;
using System.Text.RegularExpressions;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;
using OTAPI;

namespace FragLand.TerracordPlugin
{
  [ApiVersion(2, 1)]
  public class Terracord:TerrariaPlugin
  {
    ///<summary>
    /// Plugin name
    /// </summary>
    public override string Name => "Terracord";

    /// <summary>
    /// Plugin version
    /// </summary>
    public override Version Version => new Version(1, 2, 1);

    /// <summary>
    /// Plugin author(s)
    /// </summary>
    public override string Author => "Lloyd Dilley";

    /// <summary>
    /// Plugin description
    /// </summary>
    public override string Description => "A Discord <-> Terraria bridge plugin for TShock";

    // Plugin version
    public const string PluginVersion = "1.2.1";
    // Discord bot client
    private readonly Discord discord;
    // Plugin start time
    public static readonly DateTime startTime = DateTime.Now;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="game">TShock game</param>
    public Terracord(Main game):base(game)
    {
      // Parse terracord.xml configuration file
      Config.Parse();
      // Populate emoji dictionary
      //Util.PopulateEmojiDict();
      // Initialize Discord bot
      discord = new Discord();
    }

    /// <summary>
    /// Plugin initialization
    /// </summary>
    public override void Initialize()
    {
      ServerApi.Hooks.GameInitialize.Register(this, OnInitialize);
      ServerApi.Hooks.GamePostInitialize.Register(this, OnPostInitialize);
      ServerApi.Hooks.ServerJoin.Register(this, OnJoin);
      ServerApi.Hooks.ServerBroadcast.Register(this, OnBroadcast);
      //ServerApi.Hooks.ServerChat.Register(this, OnChat);
      PlayerHooks.PlayerChat += OnChat;
      ServerApi.Hooks.ServerLeave.Register(this, OnLeave);
      GeneralHooks.ReloadEvent += OnReload;
    }

    /// <summary>
    /// Plugin destruction 
    /// </summary>
    protected override void Dispose(bool disposing)
    {
      if(disposing)
      {
        Util.Log("Relay shutting down.", Util.Severity.Info);
        discord.Send(Properties.Strings.RelayShutdownString);
        discord.SetTopic(Config.OfflineTopic).ConfigureAwait(true);
        Thread.Sleep(1000); // allow time for topic to be set above
        ServerApi.Hooks.GameInitialize.Deregister(this, OnInitialize);
        ServerApi.Hooks.GamePostInitialize.Deregister(this, OnPostInitialize);
        ServerApi.Hooks.ServerJoin.Deregister(this, OnJoin);
        ServerApi.Hooks.ServerBroadcast.Deregister(this, OnBroadcast);
        //ServerApi.Hooks.ServerChat.Deregister(this, OnChat);
        PlayerHooks.PlayerChat -= OnChat;
        ServerApi.Hooks.ServerLeave.Deregister(this, OnLeave);
        GeneralHooks.ReloadEvent -= OnReload;
        discord.Client.Dispose();
      }
      base.Dispose(disposing);
    }

    /// <summary>
    /// Called when TShock is initialized
    /// </summary>
    /// <param name="args">event arguments passed by hook</param>
    private void OnInitialize(EventArgs args)
    {
      Util.Log("Server has started.", Util.Severity.Info);
    }

    /// <summary>
    /// Called after TShock is initialized
    /// </summary>
    /// <param name="args">event arguments passed by hook</param>
    private void OnPostInitialize(EventArgs args)
    {
      // Launch Discord bot in an asynchronous context
      // The line below blocks and prevents TShock console input when await Task.Delay(-1) is used in Connect()
      discord.Connect().GetAwaiter().GetResult();
      // Execute synchronously instead and use discard to suppress await warning
      //_ = discord.Connect();
    }

    /// <summary>
    /// Called when a player joins the server
    /// </summary>
    /// <param name="args">event arguments passed by hook</param>
    private void OnJoin(JoinEventArgs args)
    {
      PlayerEventNotify(args, Properties.Strings.PlayerJoinedString);
    }

    /// <summary>
    /// Called when a broadcast message is intercepted
    /// </summary>
    /// <param name="args">event arguments passed by hook</param>
    private void OnBroadcast(ServerBroadcastEventArgs args)
    {
      // Do not relay game broadcasts to Discord if this option is enabled
      if(Config.SilenceBroadcasts)
        return;

      // Filter broadcast messages based on content
      if(Util.FilterBroadcast($"{args.Message}"))
        return;

      Util.Log($"Server broadcast: {args.Message}", Util.Severity.Info);
      discord.Send($"**:mega: Broadcast:** {args.Message}");
    }

    /// <summary>
    /// Called when a chat message is intercepted
    /// </summary>
    /// <param name="args">event arguments passed by hook</param>
    //private void OnChat(ServerChatEventArgs args)
    private void OnChat(TShockAPI.Hooks.PlayerChatEventArgs args)
    {
      // Do not relay game chat to Discord if this option is enabled
      if(Config.SilenceChat)
        return;

      // PlayerHooks.OnPlayerChat() already handles filtering commands and muted players.
      // Do not relay commands or messages from muted players
      //if(args.Text.StartsWith(TShock.Config.CommandSpecifier, StringComparison.InvariantCulture) || args.Text.StartsWith(TShock.Config.CommandSilentSpecifier, StringComparison.InvariantCulture) || TShock.Players[args.Who].mute)
      //  return;

      // Attempt to convert any channel mentions
      //string modifiedMessage = args.Text;
      string modifiedMessage = args.RawText;
      if(Regex.IsMatch(modifiedMessage, @"#.+"))
        modifiedMessage = Util.ConvertChannelMentions(modifiedMessage, discord.Client);

      // Attempt to convert any role/user mentions
      if(Regex.IsMatch(modifiedMessage, @"@.+"))
        modifiedMessage = Util.ConvertRoleUserMentions(modifiedMessage, discord.Client);

      if(Config.LogChat)
      {
        //Util.Log($"{TShock.Players[args.Who].Name} said: {modifiedMessage}", Util.Severity.Info);
        Util.Log($"{args.Player.Name} said: {modifiedMessage}", Util.Severity.Info);
      }
      //discord.Send($"**<{TShock.Players[args.Who].Name}>** {modifiedMessage}");
      discord.Send($"**<{args.Player.Name}>** {modifiedMessage}");
    }

    /// <summary>
    /// Called when a player leaves the server
    /// </summary>
    /// <param name="args">event arguments passed by hook</param>
    private void OnLeave(LeaveEventArgs args)
    {
      PlayerEventNotify(args, Properties.Strings.PlayerLeftString);
    }

    /// <summary>
    /// Called when the TShock reload command is issued
    /// </summary>
    /// <param name="args">event arguments passed by hook</param>
    private void OnReload(ReloadEventArgs args)
    {
      Util.Log("Reload triggered. Please note that Discord bot token changes require a restart to take effect.", Util.Severity.Info);
      Config.Parse();
      Util.Log("Reload complete.", Util.Severity.Info);
    }

    /// <summary>
    /// Sends Terraria player join/leave events to the Discord text channel
    /// </summary>
    /// <param name="eventArgs">event arguments</param>
    /// <param name="message">message</param>
    private void PlayerEventNotify(object eventArgs, string message)
    {
      try
      {
        // This check should help prevent unnecessary exceptions from being logged after TShock reaps incomplete connections
        if(eventArgs != null)
        {
          string playerName = null;
          string joinLeaveEmoji = null;
          if(eventArgs is JoinEventArgs joinEventArgs)
          {
            playerName = TShock.Players[joinEventArgs.Who].Name;
            joinLeaveEmoji = ":heavy_plus_sign:";
          }
          if(eventArgs is LeaveEventArgs leaveEventArgs)
          {
            playerName = TShock.Players[leaveEventArgs.Who].Name;
            joinLeaveEmoji = ":heavy_minus_sign:";
          }
          if(!String.IsNullOrEmpty(playerName))
          {
            Util.Log($"{playerName} {message}", Util.Severity.Info);
            discord.Send($"**{joinLeaveEmoji} {playerName} {message}**");
          }
        }
      }
      catch(NullReferenceException nre)
      {
        if(Config.DebugMode)
        {
          Util.Log($"Exception caught after player joined or left TShock server: {nre.Message}", Util.Severity.Error);
          throw;
        }
      }
    }
  }
}
