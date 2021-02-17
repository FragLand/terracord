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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using TShockAPI;

namespace FragLand.TerracordPlugin
{
  class Config
  {
    // Terracord path
    public static string TerracordPath = $"{TShock.SavePath}{Path.DirectorySeparatorChar}Terracord{Path.DirectorySeparatorChar}";
    // terracord.xml options
    public static string BotToken { get; private set; }
    public static ulong ChannelId { get; private set; }
    public static ulong OwnerId { get; private set; }
    public static string CommandPrefix { get; private set; }
    public static bool RelayCommands { get; private set; }
    public static bool RemoteCommands { get; private set; }
    public static string AuthorizedRoles { get; private set; }
    public static string BotGame { get; private set; }
    public static uint TopicInterval { get; private set; }
    public static string OfflineTopic { get; private set; }
    public static byte[] BroadcastColor { get; private set; }
    public static bool SilenceBroadcasts { get; private set; }
    public static bool SilenceChat { get; private set; }
    public static bool SilenceSaves { get; private set; }
    public static bool AnnounceReconnect { get; private set; }
    public static string AvailableText { get; private set; }
    public static string UnavailableText { get; private set; }
    public static string JoinText { get; private set; }
    public static string LeaveText { get; private set; }
    public static string BroadcastText { get; private set; }
    public static string PlayerText { get; private set; }
    public static string ChatText { get; private set; }
    public static bool IgnoreChat { get; private set; }
    public static bool LogChat { get; private set; }
    public static bool ConvertEmoticons { get; private set; }
    public static int MessageLength { get; private set; }
    public static bool DebugMode { get; private set; }
    public static string LocaleString { get; private set; }
    public static CultureInfo Locale { get; private set; }
    public static string TimestampFormat { get; private set; }
    public static bool AbortOnError { get; private set; }
    public static Dictionary<string, string> EmoDict = new Dictionary<string, string>(); // holds emoticon to emoji mappings

    /// <summary>
    /// Parses the terracord.xml configuration file
    /// </summary>
    public static void Parse()
    {
      // Set default locale if value cannot be read from terracord.xml
      LocaleString = "en-US";
      Locale = new CultureInfo(LocaleString);
      // Set default timestamp format for Util.Log() called in exception in case terracord.xml cannot be parsed
      TimestampFormat = "MM/dd/yyyy HH:mm:ss zzz";
      // Do not terminate TShock by default if terracord.xml is unable to be parsed
      AbortOnError = false;

      try
      {
        // Create Terracord directory if it does not exist
        Directory.CreateDirectory(TerracordPath);
        // terracord.xml configuration file
        XDocument configFile = XDocument.Load($"{TerracordPath}terracord.xml");
        // terracord.xml root element
        XElement configOptions = configFile.Element("configuration");

        LocaleString = configOptions.Element("locale").Attribute("string").Value.ToString(Locale);
        ChangeLocale();
        BotToken = configOptions.Element("bot").Attribute("token").Value.ToString(Locale);
        ChannelId = ulong.Parse(configOptions.Element("channel").Attribute("id").Value.ToString(Locale), Locale);
        OwnerId = ulong.Parse(configOptions.Element("owner").Attribute("id").Value.ToString(Locale), Locale);
        CommandPrefix =  configOptions.Element("command").Attribute("prefix").Value.ToString(Locale);
        if(CommandPrefix.Length == 0 || CommandPrefix.Trim().Length == 0)
          throw new IOException("Command prefix is empty!");
        RelayCommands = bool.Parse(configOptions.Element("relay").Attribute("commands").Value.ToString(Locale));
        RemoteCommands = bool.Parse(configOptions.Element("remote").Attribute("commands").Value.ToString(Locale));
        AuthorizedRoles = configOptions.Element("authorized").Attribute("roles").Value.ToString(Locale);
        BotGame = configOptions.Element("game").Attribute("status").Value.ToString(Locale);
        TopicInterval = uint.Parse(configOptions.Element("topic").Attribute("interval").Value.ToString(Locale), Locale);
        OfflineTopic = configOptions.Element("topic").Attribute("offline").Value.ToString(Locale);

        // Populate broadcast RGB array values
        BroadcastColor = new byte[3]
        {
          byte.Parse(configOptions.Element("color").Attribute("red").Value.ToString(Locale), Locale),
          byte.Parse(configOptions.Element("color").Attribute("green").Value.ToString(Locale), Locale),
          byte.Parse(configOptions.Element("color").Attribute("blue").Value.ToString(Locale), Locale)
        };

        SilenceBroadcasts = bool.Parse(configOptions.Element("silence").Attribute("broadcasts").Value.ToString(Locale));
        SilenceChat = bool.Parse(configOptions.Element("silence").Attribute("chat").Value.ToString(Locale));
        SilenceSaves = bool.Parse(configOptions.Element("silence").Attribute("saves").Value.ToString(Locale));
        AnnounceReconnect = bool.Parse(configOptions.Element("announce").Attribute("reconnect").Value.ToString(Locale));
        AvailableText = configOptions.Element("available").Attribute("text").Value.ToString(Locale);
        UnavailableText = configOptions.Element("unavailable").Attribute("text").Value.ToString(Locale);
        JoinText = configOptions.Element("join").Attribute("text").Value.ToString(Locale);
        LeaveText = configOptions.Element("leave").Attribute("text").Value.ToString(Locale);
        BroadcastText = configOptions.Element("broadcast").Attribute("text").Value.ToString(Locale);
        PlayerText = configOptions.Element("player").Attribute("text").Value.ToString(Locale);
        ChatText = configOptions.Element("chat").Attribute("text").Value.ToString(Locale);
        IgnoreChat = bool.Parse(configOptions.Element("ignore").Attribute("chat").Value.ToString(Locale));
        LogChat = bool.Parse(configOptions.Element("log").Attribute("chat").Value.ToString(Locale));
        ConvertEmoticons = bool.Parse(configOptions.Element("convert").Attribute("emoticons").Value.ToString(Locale));
        MessageLength = int.Parse(configOptions.Element("message").Attribute("length").Value.ToString(Locale), Locale);
        DebugMode = bool.Parse(configOptions.Element("debug").Attribute("mode").Value.ToString(Locale));
        TimestampFormat = configOptions.Element("timestamp").Attribute("format").Value.ToString(Locale);
        AbortOnError = bool.Parse(configOptions.Element("exception").Attribute("abort").Value.ToString(Locale));
        if(ConvertEmoticons)
          EmoDict = configFile.Descendants("map").ToDictionary(m => (string)m.Attribute("emoticon"), m => (string)m.Attribute("emoji"));
      }
      catch(FileNotFoundException fnfe)
      {
        Util.Log($"Unable to parse terracord.xml: {fnfe.Message}", Util.Severity.Error);
        Generate();
      }
      // This will catch and log anything else such as SecurityException for a permission issue, FormatException during conversion, etc.
      catch(Exception e)
      {
        Util.Log($"Unable to parse terracord.xml: {e.Message}", Util.Severity.Error);
        throw;
      }
      Util.Log("terracord.xml parsed.", Util.Severity.Info);
      // Display configuration values
      if(Config.DebugMode)
        Display();
    }

    /// <summary>
    /// Changes locale
    /// </summary>
    public static void ChangeLocale()
    {
      Locale = new CultureInfo(LocaleString);
      CultureInfo.CurrentCulture = Locale;
      CultureInfo.CurrentUICulture = Locale;
      CultureInfo.DefaultThreadCurrentCulture = Locale;
      CultureInfo.DefaultThreadCurrentUICulture = Locale;
      Thread.CurrentThread.CurrentCulture = Locale;
      Thread.CurrentThread.CurrentUICulture = Locale;
    }

    /// <summary>
    /// Displays the terracord.xml configuration options
    /// </summary>
    public static void Display()
    {
      Util.Log("Configuration Values", Util.Severity.Debug);
      Util.Log("--------------------", Util.Severity.Debug);
      Util.Log($"Bot Token: {BotToken}", Util.Severity.Debug);
      Util.Log($"Channel ID: {ChannelId}", Util.Severity.Debug);
      Util.Log($"Owner ID: {OwnerId}", Util.Severity.Debug);
      Util.Log($"Command Prefix: {CommandPrefix}", Util.Severity.Debug);
      Util.Log($"Relay Commands: {RelayCommands}", Util.Severity.Debug);
      Util.Log($"Remote Commands: {RemoteCommands}", Util.Severity.Debug);
      Util.Log($"Authorized Roles: {AuthorizedRoles}", Util.Severity.Debug);
      Util.Log($"Bot Game: {BotGame}", Util.Severity.Debug);
      Util.Log($"Topic Interval: {TopicInterval}", Util.Severity.Debug);
      Util.Log($"Offline Topic: {OfflineTopic}", Util.Severity.Debug);
      Util.Log($"Broadcast Color (RGB): {BroadcastColor[0]}, {BroadcastColor[1]}, {BroadcastColor[2]}", Util.Severity.Debug);
      Util.Log($"Silence Broadcasts: {SilenceBroadcasts}", Util.Severity.Debug);
      Util.Log($"Silence Chat: {SilenceChat}", Util.Severity.Debug);
      Util.Log($"Silence Saves: {SilenceSaves}", Util.Severity.Debug);
      Util.Log($"Announce Reconnect: {AnnounceReconnect}", Util.Severity.Debug);
      Util.Log($"Available Text: {AvailableText}", Util.Severity.Debug);
      Util.Log($"Unavailable Text: {UnavailableText}", Util.Severity.Debug);
      Util.Log($"Join Text: {JoinText}", Util.Severity.Debug);
      Util.Log($"Leave Text: {LeaveText}", Util.Severity.Debug);
      Util.Log($"Broadcast Text: {BroadcastText}", Util.Severity.Debug);
      Util.Log($"Player Text: {PlayerText}", Util.Severity.Debug);
      Util.Log($"Chat Text: {ChatText}", Util.Severity.Debug);
      Util.Log($"Ignore Chat: {IgnoreChat}", Util.Severity.Debug);
      Util.Log($"Log Chat: {LogChat}", Util.Severity.Debug);
      Util.Log($"Convert Emoticons: {ConvertEmoticons}", Util.Severity.Debug);
      Util.Log($"Message Length: {MessageLength}", Util.Severity.Debug);
      Util.Log($"Debug Mode: {DebugMode}", Util.Severity.Debug);
      Util.Log($"Locale String: {LocaleString}", Util.Severity.Debug);
      Util.Log($"Timestamp Format: {TimestampFormat}", Util.Severity.Debug);
      Util.Log($"Exception Abort: {AbortOnError}", Util.Severity.Debug);
    }

    /// <summary>
    /// Generates a default terracord.xml configuration file
    /// </summary>
    public static void Generate()
    {
      try
      {
        Directory.CreateDirectory(TerracordPath);
        Util.Log($"Attempting to generate {TerracordPath}terracord.xml since the file did not exist...", Util.Severity.Info);
        StreamWriter newConfigFile = new StreamWriter($"{TerracordPath}terracord.xml", false);
        newConfigFile.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\" ?>\n");
        newConfigFile.WriteLine("<!-- Terracord configuration version 1.2.3 -->");
        newConfigFile.WriteLine("<configuration>\n");
        newConfigFile.WriteLine("  <!-- Discord bot token -->");
        newConfigFile.WriteLine("  <bot token=\"ABC\" />\n");
        newConfigFile.WriteLine("  <!-- Discord channel ID -->");
        newConfigFile.WriteLine("  <channel id=\"123\" />\n");
        newConfigFile.WriteLine("  <!-- Discord bot owner ID -->");
        newConfigFile.WriteLine("  <owner id=\"123\" />\n");
        newConfigFile.WriteLine("  <!-- Bot command prefix -->");
        newConfigFile.WriteLine("  <command prefix=\"!\" />\n");
        newConfigFile.WriteLine("  <!-- Relay bot commands from Discord to players -->");
        newConfigFile.WriteLine("  <relay commands=\"true\" />\n");
        newConfigFile.WriteLine("  <!-- Toggle execution of TShock commands submitted remotely by Discord bot owner -->");
        newConfigFile.WriteLine("  <remote commands=\"true\" />\n");
        newConfigFile.WriteLine("  <!-- List of space-separated Discord roles authorized to execute TShock commands remotely -->");
        newConfigFile.WriteLine("  <authorized roles=\"Administrators Moderators\" />\n");
        newConfigFile.WriteLine("  <!-- Discord bot game for \"playing\" status -->");
        newConfigFile.WriteLine("  <game status=\"Terraria\" />\n");
        newConfigFile.WriteLine("  <!-- Topic update interval in seconds and topic to set when relay is offline -->");
        newConfigFile.WriteLine("  <topic interval=\"300\" offline=\"Relay offline\" />\n");
        newConfigFile.WriteLine("  <!-- Terraria broadcast color in RGB -->");
        newConfigFile.WriteLine("  <color red=\"255\" green=\"215\" blue=\"0\" />\n");
        newConfigFile.WriteLine("  <!-- Toggle broadcasts, chat, and world saves displayed in Discord -->");
        newConfigFile.WriteLine("  <silence broadcasts=\"false\" chat=\"false\" saves=\"false\" />\n");
        newConfigFile.WriteLine("  <!-- Notify Discord channel of relay availability after restoring the connection -->");
        newConfigFile.WriteLine("  <announce reconnect=\"false\" />\n");
        newConfigFile.WriteLine("  <!-- Message sent to Discord text channel when the relay becomes available -->");
        newConfigFile.WriteLine("  <available text=\"**:white_check_mark: Relay available.**\" />\n");
        newConfigFile.WriteLine("  <!-- Message sent to Discord text channel when the relay is shutting down -->");
        newConfigFile.WriteLine("  <unavailable text=\"**:octagonal_sign: Relay shutting down.**\" />\n");
        newConfigFile.WriteLine("  <!-- Message sent to Discord text channel when a player joins the game -->");
        newConfigFile.WriteLine("  <join text=\"**:green_circle: %p% has joined the server.**\" />\n");
        newConfigFile.WriteLine("  <!-- Message sent to Discord text channel when a player leaves the game -->");
        newConfigFile.WriteLine("  <leave text=\"**:red_circle: %p% has left the server.**\" />\n");
        newConfigFile.WriteLine("  <!-- Message sent to Discord text channel when a broadcast is sent from the game -->");
        newConfigFile.WriteLine("  <broadcast text=\"**:mega: Broadcast:** %m%\" />\n");
        newConfigFile.WriteLine("  <!-- Message sent to Discord text channel when a player chats in the game -->");
        newConfigFile.WriteLine("  <player text=\"**&lt;%p%&gt;** %m%\" />\n");
        newConfigFile.WriteLine("  <!-- Message sent to game when a user chats in the Discord text channel -->");
        newConfigFile.WriteLine("  <chat text=\"&lt;%u%@Discord&gt; %m%\" />\n");
        newConfigFile.WriteLine("  <!-- Toggle Discord chat displayed in game -->");
        newConfigFile.WriteLine("  <ignore chat=\"false\" />\n");
        newConfigFile.WriteLine("  <!-- Log all chat messages -->");
        newConfigFile.WriteLine("  <log chat=\"true\" />\n");
        newConfigFile.WriteLine("  <!-- Convert emoticons from Terraria players to emojis in Discord -->");
        newConfigFile.WriteLine("  <convert emoticons=\"false\" />\n");
        newConfigFile.WriteLine("  <!-- Maximum length allowed in game for received Discord messages (0 = unlimited) -->");
        newConfigFile.WriteLine("  <message length=\"0\" />\n");
        newConfigFile.WriteLine("  <!-- Debug mode -->");
        newConfigFile.WriteLine("  <debug mode=\"false\" />\n");
        newConfigFile.WriteLine("  <!-- Locale -->");
        newConfigFile.WriteLine("  <locale string=\"en-US\" />\n");
        newConfigFile.WriteLine("  <!-- Timestamp format -->");
        newConfigFile.WriteLine("  <timestamp format=\"MM/dd/yyyy HH:mm:ss zzz\" />\n");
        newConfigFile.WriteLine("  <!-- Terminate TShock when an error is encountered -->");
        newConfigFile.WriteLine("  <exception abort=\"false\" />\n");
        newConfigFile.WriteLine("  <!-- Emoticon to emoji mappings -->");
        newConfigFile.WriteLine("  <map emoticon=\":~(\" emoji=\":cry:\" />");
        newConfigFile.WriteLine("  <map emoticon=\":E\" emoji=\":nerd:\" />");
        newConfigFile.WriteLine("  <map emoticon=\";)\" emoji=\":wink:\" />");
        newConfigFile.WriteLine("  <map emoticon=\":*\" emoji=\":kissing:\" />");
        newConfigFile.WriteLine("  <map emoticon=\":D\" emoji=\":grinning:\" />");
        newConfigFile.WriteLine("  <map emoticon=\":\\\" emoji=\":confused:\" />");
        newConfigFile.WriteLine("  <map emoticon=\":/\" emoji=\":confused:\" />");
        newConfigFile.WriteLine("  <map emoticon=\"&lt;3\" emoji=\":heart:\" />");
        newConfigFile.WriteLine("  <map emoticon=\":o\" emoji=\":open_mouth:\" />");
        newConfigFile.WriteLine("  <map emoticon=\":O\" emoji=\":open_mouth:\" />");
        newConfigFile.WriteLine("  <map emoticon=\":)\" emoji=\":slight_smile:\" />");
        newConfigFile.WriteLine("  <map emoticon=\":(\" emoji=\":slight_frown:\" />");
        newConfigFile.WriteLine("  <map emoticon=\":|\" emoji=\":neutral_face:\" />");
        newConfigFile.WriteLine("  <map emoticon=\":p\" emoji=\":stuck_out_tongue:\" />");
        newConfigFile.WriteLine("  <map emoticon=\":P\" emoji=\":stuck_out_tongue:\" />");
        newConfigFile.WriteLine("  <map emoticon=\"&lt;/3\" emoji=\":broken_heart:\" />");
        newConfigFile.WriteLine("  <map emoticon=\";p\" emoji=\":stuck_out_tongue_winking_eye:\" />");
        newConfigFile.WriteLine("  <map emoticon=\";P\" emoji=\":stuck_out_tongue_winking_eye:\" />\n");
        newConfigFile.WriteLine("</configuration>");
        newConfigFile.Close();
        Util.Log($"{TerracordPath}terracord.xml created successfully.", Util.Severity.Info);
        Util.Log("Please configure your bot token and channel ID before loading the Terracord plugin.", Util.Severity.Warning);
        if(AbortOnError)
          Environment.Exit(Util.ExitFailure);
      }
      catch(Exception e)
      {
        Util.HandleFatalError($"Unable to create terracord.xml: {e.Message}");
        throw;
      }
    }
  }
}
