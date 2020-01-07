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
using Terraria;
using TerrariaApi.Server;
using TShockAPI;

namespace Terracord
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
    public override Version Version => new Version(1, 0, 0);

    /// <summary>
    /// Plugin author(s)
    /// </summary>
    public override string Author => "Lloyd Dilley";

    /// <summary>
    /// Plugin description
    /// </summary>
    public override string Description => "A Discord <-> Terraria bridge plugin for TShock";

    // Discord bot client
    private readonly Discord discord;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="game">TShock game</param>
    public Terracord(Main game):base(game)
    {
      // Parse terracord.xml configuration file
      Config.Parse();
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
      ServerApi.Hooks.ServerChat.Register(this, OnChat);
      ServerApi.Hooks.ServerLeave.Register(this, OnLeave);
    }

    /// <summary>
    /// Plugin destruction 
    /// </summary>
    protected override void Dispose(bool disposing)
    {
      if(disposing)
      {
        Util.Log("Relay shutting down.", Util.Severity.Info);
        discord.Send("**:octagonal_sign: Relay shutting down.**");
        ServerApi.Hooks.GameInitialize.Deregister(this, OnInitialize);
        ServerApi.Hooks.GamePostInitialize.Deregister(this, OnPostInitialize);
        ServerApi.Hooks.ServerJoin.Deregister(this, OnJoin);
        ServerApi.Hooks.ServerBroadcast.Deregister(this, OnBroadcast);
        ServerApi.Hooks.ServerChat.Deregister(this, OnChat);
        ServerApi.Hooks.ServerLeave.Deregister(this, OnLeave);
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
      Util.Log($"{TShock.Players[args.Who].Name} has joined the server.", Util.Severity.Info);
      discord.Send($"**:heavy_plus_sign: {TShock.Players[args.Who].Name} has joined the server.**");
    }

    /// <summary>
    /// Called when a broadcast message is intercepted
    /// </summary>
    /// <param name="args">event arguments passed by hook</param>
    private void OnBroadcast(ServerBroadcastEventArgs args)
    {
      Util.Log($"Server broadcast: {args.Message}", Util.Severity.Info);
      discord.Send($"**:mega: Broadcast:** {args.Message}");
    }

    /// <summary>
    /// Called when a chat message is intercepted
    /// </summary>
    /// <param name="args">event arguments passed by hook</param>
    private void OnChat(ServerChatEventArgs args)
    {
      // Do not relay commands
      if(args.Text.StartsWith("/"))
        return;

      /* Initial work on Discord mentions from Terraria
       * Sequence:
       * 1. Check args.Text for regex match of a tag.
       * 2. If 1 or more instances found, iterate through Discord server members to find potential username matches.
       * 3. Replace @user tags in args.Text with user.Mention and send the message
      var guilds = botClient.Guilds;
      foreach(var guild in guilds)
      {
        var members = guild.Users;
        foreach(var member in members)
        {
          if("test".Equals(member.Username, StringComparison.OrdinalIgnoreCase))
            Console.WriteLine($"{member.Mention}");
        }
      }
      */

      if(Config.LogChat)
        Util.Log($"{TShock.Players[args.Who].Name} said: {args.Text}", Util.Severity.Info);
      discord.Send($"**<{TShock.Players[args.Who].Name}>** {args.Text}");
    }

    /// <summary>
    /// Called when a player leaves the server
    /// </summary>
    /// <param name="args">event arguments passed by hook</param>
    private void OnLeave(LeaveEventArgs args)
    {
      try
      {
        string player = TShock.Players[args.Who].Name;
        Util.Log($"{player} has left the server.", Util.Severity.Info);
        discord.Send($"**:heavy_minus_sign: {player} has left the server.**");
      }
      catch(NullReferenceException nre)
      {
        Util.Log($"Exception caught after player left TShock server: {nre.Message}", Util.Severity.Error);
      }
    }
  }
}
