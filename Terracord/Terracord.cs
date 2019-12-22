/*
 * Terracord.cs - A Discord <-> Terraria bridge plugin for TShock
 * Copyright (C) 2019 Lloyd Dilley
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
    public override Version Version => new Version(0, 1, 0);

    /// <summary>
    /// Plugin author(s)
    /// </summary>
    public override string Author => "Lloyd Dilley";

    /// <summary>
    /// Plugin description
    /// </summary>
    public override string Description => "A Discord <-> Terraria bridge plugin for TShock";

    public static string configFile = Path.Combine(TShock.SavePath, "terracord", "terracord.cfg");

    public Terracord(Main game):base(game)
    {
      // ToDo: Construct stuff here if needed.
    }

    /// <summary>
    /// Plugin initialization
    /// </summary>
    public override void Initialize()
    {
      ServerApi.Hooks.GameInitialize.Register(this, onInitialize);
      ServerApi.Hooks.GamePostInitialize.Register(this, onPostInitialize);
      ServerApi.Hooks.NetGreetPlayer.Register(this, onGreet);
      ServerApi.Hooks.ServerJoin.Register(this, onJoin);
      ServerApi.Hooks.ServerBroadcast.Register(this, onBroadcast);
      ServerApi.Hooks.ServerChat.Register(this, onChat);
      ServerApi.Hooks.ServerLeave.Register(this, onLeave);
    }

    /// <summary>
    /// Plugin destruction 
    /// </summary>
    protected override void Dispose(bool disposing)
    {
      if(disposing)
      {
        ServerApi.Hooks.GameInitialize.Deregister(this, onInitialize);
        ServerApi.Hooks.GamePostInitialize.Deregister(this, onPostInitialize);
        ServerApi.Hooks.NetGreetPlayer.Deregister(this, onGreet);
        ServerApi.Hooks.ServerJoin.Deregister(this, onJoin);
        ServerApi.Hooks.ServerBroadcast.Deregister(this, onBroadcast);
        ServerApi.Hooks.ServerChat.Deregister(this, onChat);
        ServerApi.Hooks.ServerLeave.Deregister(this, onLeave);
      }
      base.Dispose(disposing);
      Console.WriteLine("Server is shutting down.");
    }

    // The output for the methods below will eventually go to a configurable Discord channel. -ldilley

    /// <summary>
    /// Called when TShock is initialized
    /// </summary>
    /// <param name="args">event arguments passed by hook</param>
    private void onInitialize(EventArgs args)
    {
      // ToDo: Parse configuration.
      Console.WriteLine("Server started.");
    }

    /// <summary>
    /// Called after TShock is initialized
    /// </summary>
    /// <param name="args">event arguments passed by hook</param>
    private void onPostInitialize(EventArgs args)
    {
      // ToDo: Connect to Discord.
      Console.WriteLine("This is when we should connect to Discord.");
    }

    /// <summary>
    /// Called when a player joins the server
    /// </summary>
    /// <param name="args">event arguments passed by hook</param>
    private void onGreet(GreetPlayerEventArgs args)
    {
      Console.WriteLine($"{TShock.Players[args.Who].Name} has joined the server.");
    }

    /// <summary>
    /// Called when a player joins the server
    /// </summary>
    /// <param name="args">event arguments passed by hook</param>
    private void onJoin(JoinEventArgs args)
    {
      Console.WriteLine($"{TShock.Players[args.Who].Name} has joined the server.");
    }

    /// <summary>
    /// Called when a broadcast message is intercepted
    /// </summary>
    /// <param name="args">event arguments passed by hook</param>
    private void onBroadcast(ServerBroadcastEventArgs args)
    {
      Console.WriteLine($"Server broadcast: {args.Message}");
    }

    /// <summary>
    /// Called when a chat message is intercepted
    /// </summary>
    /// <param name="args">event arguments passed by hook</param>
    private void onChat(ServerChatEventArgs args)
    {
      Console.WriteLine($"{TShock.Players[args.Who].Name} said: {args.Text}");     
    }

    /// <summary>
    /// Called when a player leaves the server
    /// </summary>
    /// <param name="args">event arguments passed by hook</param>
    private void onLeave(LeaveEventArgs args)
    {
      Console.WriteLine($"{TShock.Players[args.Who].Name} has left the server.");
    }
  }
}
