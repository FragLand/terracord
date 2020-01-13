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
using System.IO;
using System.Xml.Linq;

namespace Terracord
{
  class Config
  {
    // terracord.xml options
    public static string BotToken { get; private set; }
    public static ulong ChannelId { get; private set; }
    public static char CommandPrefix { get; private set; }
    public static string BotGame { get; private set; }
    public static uint TopicInterval { get; private set; }
    public static byte[] BroadcastColor { get; private set; }
    public static bool LogChat { get; private set; }
    public static bool DebugMode { get; private set; }
    public static string TimestampFormat { get; private set; }
    public static bool AbortOnError { get; private set; }

    /// <summary>
    /// Parses the terracord.xml configuration file
    /// </summary>
    public static void Parse()
    {
      // Set default timestamp format for Util.Log() called in exception in case terracord.xml cannot be parsed
      TimestampFormat = "MM/dd/yyyy HH:mm:ss zzz";
      // Do not terminate TShock by default if terracord.xml is unable to be parsed
      AbortOnError = false;

      try
      {
        // terracord.xml configuration file
        XDocument configFile = XDocument.Load($"tshock{Path.DirectorySeparatorChar}terracord.xml");
        // terracord.xml root element
        XElement configOptions = configFile.Element("configuration");

        BotToken = configOptions.Element("bot").Attribute("token").Value.ToString();
        ChannelId = UInt64.Parse(configOptions.Element("channel").Attribute("id").Value.ToString());
        CommandPrefix = Char.Parse(configOptions.Element("command").Attribute("prefix").Value.ToString());
        BotGame = configOptions.Element("game").Attribute("status").Value.ToString();
        TopicInterval = UInt32.Parse(configOptions.Element("topic").Attribute("interval").Value.ToString());

        // Populate broadcast RGB array values
        BroadcastColor = new byte[3]
        {
          Byte.Parse(configOptions.Element("broadcast").Attribute("red").Value.ToString()),
          Byte.Parse(configOptions.Element("broadcast").Attribute("green").Value.ToString()),
          Byte.Parse(configOptions.Element("broadcast").Attribute("blue").Value.ToString())
        };

        LogChat = Boolean.Parse(configOptions.Element("log").Attribute("chat").Value.ToString());
        DebugMode = Boolean.Parse(configOptions.Element("debug").Attribute("mode").Value.ToString());
        TimestampFormat = configOptions.Element("timestamp").Attribute("format").Value.ToString();
        AbortOnError = Boolean.Parse(configOptions.Element("exception").Attribute("abort").Value.ToString());
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
      }
      Util.Log("terracord.xml parsed.", Util.Severity.Info);
      // Display configuration values
      if(Config.DebugMode)
        Display();
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
      Util.Log($"Command Prefix: {CommandPrefix}", Util.Severity.Debug);
      Util.Log($"Bot Game: {BotGame}", Util.Severity.Debug);
      Util.Log($"Topic Interval: {TopicInterval}", Util.Severity.Debug);
      Util.Log($"Broadcast Color (RGB): {BroadcastColor[0]}, {BroadcastColor[1]}, {BroadcastColor[2]}", Util.Severity.Debug);
      Util.Log($"Log Chat: {LogChat}", Util.Severity.Debug);
      Util.Log($"Debug Mode: {DebugMode}", Util.Severity.Debug);
      Util.Log($"Timestamp Format: {TimestampFormat}", Util.Severity.Debug);
      Util.Log($"Exception Abort: {AbortOnError}", Util.Severity.Debug);
    }

    /// <summary>
    /// Generates a default terracord.xml configuration file
    /// </summary>
    public static void Generate()
    {
      Util.Log($"Attempting to generate tshock{Path.DirectorySeparatorChar}terracord.xml since the file did not exist...", Util.Severity.Info);
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
        newConfigFile.WriteLine("  <!-- Topic update interval in seconds -->");
        newConfigFile.WriteLine("  <topic interval=\"300\" />\n");
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
        Util.Log($"tshock{Path.DirectorySeparatorChar}terracord.xml created successfully.", Util.Severity.Info);
        Util.Log("Please configure your bot token and channel ID before loading the Terracord plugin.", Util.Severity.Warning);
        if(AbortOnError)
          Environment.Exit(Util.ExitFailure);
      }
      catch(Exception e)
      {
        Util.Log($"Unable to create terracord.xml: {e.Message}", Util.Severity.Error);
        if(AbortOnError)
          Environment.Exit(Util.ExitFailure);
      }
    }
  }
}
