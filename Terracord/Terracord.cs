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
using Discord.WebSocket;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Linq;
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

    private DiscordSocketClient botClient;
    private static readonly XDocument configFile = XDocument.Load($"tshock{Path.DirectorySeparatorChar}terracord.xml");
    private static readonly XElement configOptions = configFile.Element("configuration");
    private static readonly string botToken = configOptions.Element("bot").Attribute("token").Value.ToString();
    private static readonly ulong channelId = UInt64.Parse(configOptions.Element("channel").Attribute("id").Value.ToString());
    private static readonly char commandPrefix = Char.Parse(configOptions.Element("command").Attribute("prefix").Value.ToString());
    private static readonly string botGame = configOptions.Element("game").Attribute("status").Value.ToString();
    private static readonly byte[] broadcastColor = new byte[3] {0, 0, 0};
    private static readonly bool logChat = Boolean.Parse(configOptions.Element("log").Attribute("chat").Value.ToString());
    private static IMessageChannel channel = null;

    public Terracord(Main game):base(game)
    {
      broadcastColor[0] = Byte.Parse(configOptions.Element("broadcast").Attribute("red").Value.ToString());
      broadcastColor[1] = Byte.Parse(configOptions.Element("broadcast").Attribute("green").Value.ToString());
      broadcastColor[2] = Byte.Parse(configOptions.Element("broadcast").Attribute("blue").Value.ToString());
      Console.WriteLine($"{botToken} {channelId} {commandPrefix} {botGame} {broadcastColor[0]} {broadcastColor[1]} {broadcastColor[2]}");
    }

    /// <summary>
    /// Plugin initialization
    /// </summary>
    public override void Initialize()
    {
      ServerApi.Hooks.GameInitialize.Register(this, OnInitialize);
      ServerApi.Hooks.GamePostInitialize.Register(this, OnPostInitialize);
      //ServerApi.Hooks.NetGreetPlayer.Register(this, OnGreet);
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
        channel.SendMessageAsync("**:octagonal_sign: Server is shutting down.**");
        Log("Server is shutting down.");
        ServerApi.Hooks.GameInitialize.Deregister(this, OnInitialize);
        ServerApi.Hooks.GamePostInitialize.Deregister(this, OnPostInitialize);
        //ServerApi.Hooks.NetGreetPlayer.Deregister(this, OnGreet);
        ServerApi.Hooks.ServerJoin.Deregister(this, OnJoin);
        ServerApi.Hooks.ServerBroadcast.Deregister(this, OnBroadcast);
        ServerApi.Hooks.ServerChat.Deregister(this, OnChat);
        ServerApi.Hooks.ServerLeave.Deregister(this, OnLeave);
        botClient.Dispose();
      }
      base.Dispose(disposing);
    }

    /// <summary>
    /// Called when a log message needs to be written
    /// </summary>
    /// <param name="logText">the text to log</param>
    public static void Log(string logText)
    {
      StreamWriter logFile = new StreamWriter($"tshock{Path.DirectorySeparatorChar}terracord.log", true);
      logFile.WriteLine($"[{DateTime.Now.ToString()}] {logText.ToString()}");
      Console.WriteLine($"Terracord: [{DateTime.Now.ToString()}] {logText.ToString()}");
      logFile.Close();
    }

    /// <summary>
    /// Called when TShock is initialized
    /// </summary>
    /// <param name="args">event arguments passed by hook</param>
    private void OnInitialize(EventArgs args)
    {
      Log("Server has started.");
    }

    /// <summary>
    /// Called after TShock is initialized
    /// </summary>
    /// <param name="args">event arguments passed by hook</param>
    private void OnPostInitialize(EventArgs args)
    {
      Log("Connecting to Discord...");
      _ = BotConnect(); // suppress await warning via discard
    }

    /// <summary>
    /// Called when a player joins the server
    /// </summary>
    /// <param name="args">event arguments passed by hook</param>
    // OnGreet is redundant with OnJoin.
    //private void OnGreet(GreetPlayerEventArgs args)
    //{
    //  Log($"{TShock.Players[args.Who].Name} has joined the server.");
    //  channel.SendMessageAsync($"**:heavy_plus_sign: {TShock.Players[args.Who].Name} has joined the server.**");
    //}

    /// <summary>
    /// Called when a player joins the server
    /// </summary>
    /// <param name="args">event arguments passed by hook</param>
    private void OnJoin(JoinEventArgs args)
    {
      Log($"{TShock.Players[args.Who].Name} has joined the server.");
      channel.SendMessageAsync($"**:heavy_plus_sign: {TShock.Players[args.Who].Name} has joined the server.**");
    }

    /// <summary>
    /// Called when a broadcast message is intercepted
    /// </summary>
    /// <param name="args">event arguments passed by hook</param>
    private void OnBroadcast(ServerBroadcastEventArgs args)
    {
      Log($"Server broadcast: {args.Message}");
      channel.SendMessageAsync($"**:mega: Broadcast:** {args.Message}");
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
      if(logChat)
        Log($"{TShock.Players[args.Who].Name} said: {args.Text}");
      channel.SendMessageAsync($"**<{TShock.Players[args.Who].Name}>** {args.Text}");
    }

    /// <summary>
    /// Called when a player leaves the server
    /// </summary>
    /// <param name="args">event arguments passed by hook</param>
    private void OnLeave(LeaveEventArgs args)
    {
      Log($"{TShock.Players[args.Who].Name} has left the server.");
      channel.SendMessageAsync($"**:heavy_minus_sign: {TShock.Players[args.Who].Name} has left the server.**");
    }

    /// <summary>
    /// Initializes the Discord bot
    /// </summary>
    /// <returns></returns>
    public async Task BotConnect()
    {
      botClient = new DiscordSocketClient();
      botClient.Log += BotLog;

      await botClient.LoginAsync(TokenType.Bot, botToken);
      await botClient.StartAsync();
      botClient.Ready += BotReady;
      botClient.MessageReceived += BotMessageReceived;

      // Set game/playing status
      await botClient.SetGameAsync(botGame);

      // Block task until program termination
      await Task.Delay(-1);
    }

    /// <summary>
    /// Called when a Discord.Net message requires logging
    /// </summary>
    /// <returns></returns>
    private async Task BotLog(LogMessage message)
    {
      Log(message.ToString());
      await Task.CompletedTask;
    }

    /// <summary>
    /// Called after the bot establishes the connection to Discord
    /// </summary>
    /// <returns></returns>
    private async Task BotReady()
    {
      channel = botClient.GetChannel(channelId) as IMessageChannel;
      await channel.SendMessageAsync("**:white_check_mark: Server has started.**");
    }

    /// <summary>
    /// Called when a new message is received by the Discord bot
    /// </summary>
    /// <param name="message">message received by Discord bot</param>
    /// <returns></returns>
    private async Task BotMessageReceived(SocketMessage message)
    {
      // Only accept messages from configured Discord text channel
      if(message.Channel.Id != channelId)
        return;

      // Do not send duplicates messages from Discord bot to Terraria players
      if(message.Author.Id == botClient.CurrentUser.Id)
        return;

      // Relay Discord message to Terraria players
      if(logChat)
        Log($"<{message.Author.Username}@Discord> {message.Content}");
      TShock.Utils.Broadcast($"<{message.Author.Username}@Discord> {message.Content}", broadcastColor[0], broadcastColor[1], broadcastColor[2]);

      await Task.CompletedTask;
    }
  }
}
