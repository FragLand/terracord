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
  public class Terracord : TerrariaPlugin
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

    // Exit values
    //private const int ExitSuccess = 0; // unused for now
    private const int ExitFailure = -1;
    // Discord bot client
    private readonly DiscordSocketClient botClient;
    // Discord relay channel
    private static IMessageChannel channel = null;
    // terracord.xml configuration file
    private readonly XDocument configFile;
    // terracord.xml root element
    private readonly XElement configOptions;
    // terracord.xml options
    private readonly string botToken;
    private readonly ulong channelId;
    private readonly char commandPrefix;
    private readonly string botGame;
    private readonly byte[] broadcastColor = new byte[3]{255, 215, 0}; // gold by default
    private readonly bool logChat;
    private readonly bool debugMode;
    private readonly string timestampFormat;
    private readonly bool abortOnError;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="game">TShock game</param>
    public Terracord(Main game):base(game)
    {
      try
      {
        // Parse terracord.xml configuration file
        configFile = XDocument.Load($"tshock{Path.DirectorySeparatorChar}terracord.xml");
        configOptions = configFile.Element("configuration");
        botToken = configOptions.Element("bot").Attribute("token").Value.ToString();
        channelId = UInt64.Parse(configOptions.Element("channel").Attribute("id").Value.ToString());
        commandPrefix = Char.Parse(configOptions.Element("command").Attribute("prefix").Value.ToString());
        botGame = configOptions.Element("game").Attribute("status").Value.ToString();
        // Populate broadcast RGB array values
        broadcastColor[0] = Byte.Parse(configOptions.Element("broadcast").Attribute("red").Value.ToString());
        broadcastColor[1] = Byte.Parse(configOptions.Element("broadcast").Attribute("green").Value.ToString());
        broadcastColor[2] = Byte.Parse(configOptions.Element("broadcast").Attribute("blue").Value.ToString());
        logChat = Boolean.Parse(configOptions.Element("log").Attribute("chat").Value.ToString());
        debugMode = Boolean.Parse(configOptions.Element("debug").Attribute("mode").Value.ToString());
        timestampFormat = configOptions.Element("timestamp").Attribute("format").Value.ToString();
        abortOnError = Boolean.Parse(configOptions.Element("exception").Attribute("abort").Value.ToString());
      }
      catch(FileNotFoundException fnfe)
      {
        Log($"Unable to parse terracord.xml: {fnfe.Message}");
        // Do not terminate TShock by default if terracord.xml is missing
        abortOnError = false;
        GenerateConfigFile();
      }
      // This will catch and log anything else such as SecurityException for a permission issue, FormatException during conversion, etc.
      catch(Exception e)
      {
        Log($"Unable to parse terracord.xml: {e.Message}");
      }

      // Initialize Discord bot
      // Set AlwaysDownloadUsers to update Discord server member list for servers containing >=100 members
      // This is required to properly support mentions in more populated servers
      if(debugMode)
      {
        botClient = new DiscordSocketClient(new DiscordSocketConfig
        {
          AlwaysDownloadUsers = true,
          LogLevel = LogSeverity.Debug
        });
      }
      else
      {
        botClient = new DiscordSocketClient(new DiscordSocketConfig
        {
          AlwaysDownloadUsers = true,
          LogLevel = LogSeverity.Info
        });
      }
      botClient.Log += BotLog;
      botClient.Ready += BotReady;
      botClient.MessageReceived += BotMessageReceived;
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
        SendDiscordMessage("**:octagonal_sign: Relay shutting down.**");
        Log("Relay shutting down.");
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
    /// Writes a log message to terracord.log and the TShock console
    /// </summary>
    /// <param name="logText">the text to log</param>
    private void Log(string logText)
    {
      try
      {
        StreamWriter logFile = new StreamWriter($"tshock{Path.DirectorySeparatorChar}terracord.log", true);
        // Write to console first in case file is unavailable
        Console.WriteLine($"Terracord: [{DateTime.Now.ToString(timestampFormat)}] {logText.ToString()}");
        logFile.WriteLine($"[{DateTime.Now.ToString(timestampFormat)}] {logText.ToString()}");
        logFile.Close();
      }
      catch(Exception e)
      {
        Log($"Unable to write to terracord.log: {e.Message}");
        if(abortOnError)
          Environment.Exit(ExitFailure);
      }
    }

    /// <summary>
    /// Generates a default terracord.xml configuration file
    /// </summary>
    private void GenerateConfigFile()
    {
      Log($"Attempting to generate tshock{Path.DirectorySeparatorChar}terracord.xml since the file did not exist...");
      try
      {
        StreamWriter newConfigFile = new StreamWriter($"tshock{Path.DirectorySeparatorChar}terracord.xml", false);
        newConfigFile.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\" ?>\n");
        newConfigFile.WriteLine("<!-- Terracord configuration -->");
        newConfigFile.WriteLine("<configuration>\n");
        newConfigFile.WriteLine("  <!-- Discord bot token -->");
        newConfigFile.WriteLine("  <bot token=\"ABC\" />\n");
        newConfigFile.WriteLine("  <!-- Discord channel ID -->");
        newConfigFile.WriteLine("  <channel id=\"123\" />\n");
        newConfigFile.WriteLine("  <!-- Bot command prefix -->");
        newConfigFile.WriteLine("  <command prefix=\"!\" />\n");
        newConfigFile.WriteLine("  <!-- Discord bot game for \"playing\" status -->");
        newConfigFile.WriteLine("  <game status=\"Terraria\" />\n");
        newConfigFile.WriteLine("  <!-- Terraria broadcast color in RGB -->");
        newConfigFile.WriteLine("  <broadcast red=\"255\" green=\"215\" blue=\"0\" />\n");
        newConfigFile.WriteLine("  <!-- Log all chat messages -->");
        newConfigFile.WriteLine("  <log chat=\"true\" />\n");
        newConfigFile.WriteLine("  <!-- Debug mode -->");
        newConfigFile.WriteLine("  <debug mode=\"false\" />\n");
        newConfigFile.WriteLine("  <!-- Timestamp format -->");
        newConfigFile.WriteLine("  <timestamp format=\"MM/dd/yyyy HH:mm:ss zzz\" />\n");
        newConfigFile.WriteLine("  <!-- Terminate TShock when an error is encountered -->");
        newConfigFile.WriteLine("  <exception abort=\"false\" />\n");
        newConfigFile.WriteLine("</configuration>");
        newConfigFile.Close();
        Log($"tshock{Path.DirectorySeparatorChar}terracord.xml created successfully.");
        Log("Please configure your bot token and channel ID before loading the Terracord plugin.");
        if(abortOnError)
          Environment.Exit(ExitFailure);
      }
      catch(Exception e)
      {
        Log($"Unable to create terracord.xml: {e.Message}");
        if(abortOnError)
          Environment.Exit(ExitFailure);
      }
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
      // If we made it here, terracord.xml was successfully parsed
      Log("terracord.xml parsed.");

      // Log configuration values
      if(debugMode)
      {
        Log("Configuration Values");
        Log("--------------------");
        Log($"Bot Token: {botToken}");
        Log($"Channel ID: {channelId}");
        Log($"Command Prefix: {commandPrefix}");
        Log($"Bot Game: {botGame}");
        Log($"Broadcast Color (RGB): {broadcastColor[0]}, {broadcastColor[1]}, {broadcastColor[2]}");
        Log($"Log Chat: {logChat}");
        Log($"Debug Mode: {debugMode}");
      }

      Log("Connecting to Discord...");
      // Launch Discord bot in an asynchronous context
      // The line below blocks and prevents TShock console input when await Task.Delay(-1) is used in the called method
      new Terracord(Game).BotConnect().GetAwaiter().GetResult();
      // Execute synchronously instead and use discard to suppress await warning
      //_ = BotConnect();
    }

    /// <summary>
    /// Called when a player joins the server
    /// </summary>
    /// <param name="args">event arguments passed by hook</param>
    // OnGreet is redundant with OnJoin.
    //private void OnGreet(GreetPlayerEventArgs args)
    //{
    //  Log($"{TShock.Players[args.Who].Name} has joined the server.");
    //  SendDiscordMessage($"**:heavy_plus_sign: {TShock.Players[args.Who].Name} has joined the server.**");
    //}

    /// <summary>
    /// Called when a player joins the server
    /// </summary>
    /// <param name="args">event arguments passed by hook</param>
    private void OnJoin(JoinEventArgs args)
    {
      Log($"{TShock.Players[args.Who].Name} has joined the server.");
      SendDiscordMessage($"**:heavy_plus_sign: {TShock.Players[args.Who].Name} has joined the server.**");
    }

    /// <summary>
    /// Called when a broadcast message is intercepted
    /// </summary>
    /// <param name="args">event arguments passed by hook</param>
    private void OnBroadcast(ServerBroadcastEventArgs args)
    {
      Log($"Server broadcast: {args.Message}");
      SendDiscordMessage($"**:mega: Broadcast:** {args.Message}");
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

      if(logChat)
        Log($"{TShock.Players[args.Who].Name} said: {args.Text}");
      SendDiscordMessage($"**<{TShock.Players[args.Who].Name}>** {args.Text}");
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
        Log($"{player} has left the server.");
        SendDiscordMessage($"**:heavy_minus_sign: {player} has left the server.**");
      }
      catch(NullReferenceException nre)
      {
        Log($"Exception caught after player left TShock server: {nre.Message}");
      }
    }

    /// <summary>
    /// Connects the bot to the Discord network
    /// </summary>
    /// <returns>void</returns>
    public async Task BotConnect()
    {
      try
      {
        // Connect to Discord
        await botClient.LoginAsync(TokenType.Bot, botToken);
        await botClient.StartAsync();
      }
      catch(Exception e)
      {
        Log($"Unable to connect to Discord: {e.Message}");
        if(abortOnError)
          Environment.Exit(ExitFailure);
      }

      try
      {
        // Set game/playing status
        await botClient.SetGameAsync(botGame);
      }
      catch(Exception e)
      {
        Log($"Unable to set game/playing status: {e.Message}");
        if(abortOnError)
          Environment.Exit(ExitFailure);
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
    private Task BotLog(LogMessage message)
    {
      Log(message.ToString());
      return Task.CompletedTask;
    }

    /// <summary>
    /// Called after the bot establishes the connection to Discord
    /// </summary>
    /// <returns>Task.CompletedTask</returns>
    private Task BotReady()
    {
      try
      {
        channel = botClient.GetChannel(channelId) as IMessageChannel;
      }
      catch(Exception e)
      {
        Log($"Unable to acquire Discord channel: {e.Message}");
      }

      // The message below is sent to Discord every time the bot connects/reconnects
      Log("Relay available.");
      SendDiscordMessage("**:white_check_mark: Relay available.**");
      return Task.CompletedTask;
    }

    /// <summary>
    /// Called when a new message is received by the Discord bot
    /// </summary>
    /// <param name="message">message received by Discord bot</param>
    /// <returns>Task.CompletedTask</returns>
    private Task BotMessageReceived(SocketMessage message)
    {
      try
      {
        // Only accept messages from configured Discord text channel
        if(message.Channel.Id != channelId)
          return Task.CompletedTask;

        // Do not send duplicates messages from Discord bot to Terraria players
        if(message.Author.Id == botClient.CurrentUser.Id)
          return Task.CompletedTask;

        // Relay Discord message to Terraria players
        if(logChat)
          Log($"<{message.Author.Username}@Discord> {message.Content}");
        TShock.Utils.Broadcast($"<{message.Author.Username}@Discord> {message.Content}", broadcastColor[0], broadcastColor[1], broadcastColor[2]);
      }
      catch(Exception e)
      {
        Log($"Unable to broadcast TShock message: {e.Message}");
      }

      return Task.CompletedTask;
    }

    /// <summary>
    /// Sends a message to a Discord channel
    /// </summary>
    /// <param name="message">message to send to Discord channel</param>
    private void SendDiscordMessage(string message)
    {
      try
      {
        // channel? checks if object is null prior to sending message
        channel?.SendMessageAsync(message);
      }
      catch(Exception e)
      {
        Log($"Unable to send Discord message: {e.Message}");
      }
    }
  }
}
