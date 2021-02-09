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

using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace FragLand.TerracordPlugin
{
  class Util
  {
    // Exit values
    //private const int ExitSuccess = 0; // unused for now
    public const int ExitFailure = -1;

    // Mutex for log file writes
    private static readonly Mutex LogMutex = new Mutex();

    // Log severity
    public enum Severity
    {
      Debug = 0,
      Info = 1,
      Warning = 2,
      Error = 3    // includes critical messages and exceptions
    }

    // Holds common emoji to text emoticon mappings
    public static Dictionary<string, string> EmojiDict = new Dictionary<string, string>();

    /// <summary>
    /// Writes a log message to terracord.log and the TShock console
    /// </summary>
    /// <param name="logText">the text to log</param>
    public static void Log(string logText, Severity severity)
    {
      try
      {
        // The switch expression below is supported in C# >=8.0 (.NET Core >=3.0 and .NET Standard >=2.1)
        /*Console.ForegroundColor = severity switch
        {
          Severity.Debug => ConsoleColor.DarkGray,
          Severity.Info => ConsoleColor.White,
          Severity.Warning => ConsoleColor.Yellow,
          Severity.Error => ConsoleColor.Red,
          _ => ConsoleColor.White
        };*/
        switch(severity)
        {
          case Severity.Debug:
            Console.ForegroundColor = ConsoleColor.DarkGray;
            break;
          case Severity.Info:
            Console.ForegroundColor = ConsoleColor.White;
            break;
          case Severity.Warning:
            Console.ForegroundColor = ConsoleColor.Yellow;
            break;
          case Severity.Error:
            Console.ForegroundColor = ConsoleColor.Red;
            break;
          default:
            Console.ForegroundColor = ConsoleColor.White;
            break;
        }

        // Write to console first in case file is unavailable
        string logEntry = $"[{DateTime.Now.ToString(Config.TimestampFormat, Config.Locale)}] [{severity}] {logText.ToString(Config.Locale)}";
        Console.WriteLine($"Terracord: {logEntry}");
        Console.ResetColor();
        LogMutex.WaitOne();
        StreamWriter logFile = new StreamWriter($"{Config.TerracordPath}terracord.log", true);
        logFile.WriteLine(logEntry);
        logFile.Close();
        LogMutex.ReleaseMutex();
      }
      catch(Exception e)
      {
        // Log message also gets written to console, so it will be visible
        HandleFatalError($"Unable to write to terracord.log: {e.Message}");
        throw;
      }
    }

    /// <summary>
    /// Handles fatal errors
    /// </summary>
    public static void HandleFatalError(string errorMessage)
    {
      Log(errorMessage, Severity.Error);
      if(Config.AbortOnError)
        Environment.Exit(ExitFailure);
    }

    /// <summary>
    /// Checks Discord message for attachments and includes attachment properties in returned string
    /// </summary>
    /// <param name="message">message received by Discord bot</param>
    /// <param name="messageContent">modified message content</param>
    public static string CheckMessageAttachments(SocketMessage message, string messageContent)
    {
      foreach(var attachment in message.Attachments)
      {
        messageContent = $"Attachment: (File: {attachment.Filename}) (URL: {attachment.Url}) (Message: {messageContent})";
        break; // there should only be a single attachment per message
      }
      return messageContent;
    }

    /// <summary>
    /// Converts channel, role, and user mentions to friendly names before being broadcasted to TShock players
    /// </summary>
    /// <param name="message">message received by Discord bot</param>
    /// <returns>modified message text</returns>
    public static string ConvertMentions(SocketMessage message)
    {
      StringBuilder modifiedMessageText = new StringBuilder(message.Content);

      if(message.MentionedChannels.Count > 0)
      {
        foreach(var channel in message.MentionedChannels)
          modifiedMessageText = modifiedMessageText.Replace($"<#{channel.Id}>", $"#{channel.Name}");
      }

      if(message.MentionedRoles.Count > 0)
      {
        foreach(var role in message.MentionedRoles)
          modifiedMessageText = modifiedMessageText.Replace($"<@&{role.Id}>", $"@{role.Name}");
      }

      if(message.MentionedUsers.Count > 0)
      {
        foreach(var user in message.MentionedUsers)
          modifiedMessageText = modifiedMessageText.Replace($"<@!{user.Id}>", $"@{user.Username}");
      }

      return modifiedMessageText.ToString();
    }

    /// <summary>
    /// Converts custom Discord emotes before being sent to Terraria players
    /// </summary>
    /// <param name="message"></param>
    /// <returns>modified message</returns>
    public static string ConvertEmotes(string message)
    {
      string modifiedMessage = message;
      string emoPattern = "<(:[a-zA-Z0-9]+:)[0-9]*>";

      // Check for emotes and simplify them from <:example:> or <:custom_example:1234567890> to :example: or :custom_example:
      if(Regex.IsMatch(modifiedMessage, emoPattern))
        modifiedMessage = Regex.Replace(Regex.Escape(modifiedMessage), emoPattern, "$1");

      // Check for and replace some standard emojis in the form :smile: with :)
      //foreach(KeyValuePair<string, string> entry in Util.EmojiDict)
      //{
      //  if(modifiedMessage.Contains(entry.Value))
      //    modifiedMessage = modifiedMessage.Replace(entry.Value, entry.Key);
      //}

      return modifiedMessage;
    }

    /// <summary>
    /// Attempts to convert channel mentions sent from Terraria players to Discord
    /// </summary>
    /// <param name="message">message to modify</param>
    /// <param name="discordClient">DiscordSocketClient object</param>
    /// <returns>modified message</returns>
    public static string ConvertChannelMentions(string message, DiscordSocketClient discordClient)
    {
      string modifiedMessage = message;
      string channelPattern = @"#.+";

      var guilds = discordClient.Guilds;
      if(Regex.IsMatch(modifiedMessage, channelPattern))
      {
        foreach(var guild in guilds)
        {
          foreach(var channel in guild.TextChannels)
            modifiedMessage = Regex.Replace(Regex.Escape(modifiedMessage), $"#{channel.Name}", channel.Mention, RegexOptions.IgnoreCase);
        }
      }

      return modifiedMessage;
    }

    /// <summary>
    /// Attempts to convert role/user mentions sent from Terraria players to Discord
    /// </summary>
    /// <param name="message">message to modify</param>
    /// <param name="discordClient">DiscordSocketClient object</param>
    /// <returns>modified message</returns>
    public static string ConvertRoleUserMentions(string message, DiscordSocketClient discordClient)
    {
      string modifiedMessage = message;
      string roleUserPattern = @"@.+";

      var guilds = discordClient.Guilds;
      if(Regex.IsMatch(modifiedMessage, roleUserPattern))
      {
        foreach(var guild in guilds)
        {
          foreach(var role in guild.Roles)
            modifiedMessage = Regex.Replace(Regex.Escape(modifiedMessage), $"@{role.Name}", role.Mention, RegexOptions.IgnoreCase);
          // ToDo: Deal with duplicate usernames (users with the same username will have different #NNNN discriminators)
          foreach(var user in guild.Users)
          {
            modifiedMessage = Regex.Replace(Regex.Escape(modifiedMessage), $"@{user.Username}", user.Mention, RegexOptions.IgnoreCase);
            // Also replace nicknames with mentions -- this is bugged and replaces non-existent tags.
            //modifiedMessage = Regex.Replace(Regex.Escape(modifiedMessage), $"@{user.Nickname}", user.Mention, RegexOptions.IgnoreCase);
          }
        }
      }

      return modifiedMessage;
    }

    /// <summary>
    /// Filter broadcast message based on contents (TShock 4.4 broadcast fix)
    /// </summary>
    /// <param name="message">message to check</param>
    /// <returns>true if message should be filtered or false otherwise</returns>
    public static bool FilterBroadcast(string message)
    {
      string discordMessage = Config.ChatText.Replace("%u%", ".+");
      discordMessage = discordMessage.Replace("%m%", ".*");
      if(Regex.IsMatch(message, $"^{discordMessage}$"))  // Discord message
        return true;
      if(Regex.IsMatch(message, "^.+: .*$"))             // Terraria chat message
        return true;
      if(Config.SilenceSaves)
      {
        if(message.Equals("Saving world...", StringComparison.OrdinalIgnoreCase) || message.Equals("World saved.", StringComparison.OrdinalIgnoreCase))
          return true;
      }
      if(Regex.IsMatch(message, "^.+ has (joined|left).$")) // join/leave events
        return true;
      return false;
    }

    /// <summary>
    /// Check for newlines/carriage returns and replace them to prevent multi-line broadcasts without a Discord author prefixed (TShock 4.4 broadcast fix)
    /// </summary>
    /// <param name="message">message to check</param>
    /// <returns>modified message</returns>
    public static string FixMultiline(string message)
    {
      string modifiedMessage = Regex.Replace(message, @"\n|\r", " ");
      return modifiedMessage;
    }

    /// <summary>
    /// Check if user is authorized to execute remote TShock commands based on Discord role membership
    /// </summary>
    /// <param name="user">user to check</param>
    /// <returns>true if user is authorized or false otherwise</returns>
    public static bool AuthorizedUser(SocketUser user)
    {
      if(String.IsNullOrEmpty(Config.AuthorizedRoles))
        return false;
      foreach(var role in ((SocketGuildUser)(user)).Roles)
      {
        foreach(string allowedRole in Config.AuthorizedRoles.Split(' '))
        {
          if(role.Name.Equals(allowedRole, StringComparison.OrdinalIgnoreCase))
            return true;
        }
      }
      return false;
    }

    /// <summary>
    /// Populates the dictionary by mapping Discord emojis to text emoticons
    /// </summary>
    /*public static void PopulateEmojiDict()
    {
      EmojiDict.Add("\uD83d\uDE04", ":)");
      EmojiDict.Add("\uD83D\uDE03", ":)");
      EmojiDict.Add("\uD83D\uDE42", ":)");
      EmojiDict.Add("\u2639\uFE0F", ":(");
      EmojiDict.Add("\uD83D\uDE26", ":(");
      EmojiDict.Add("\uD83D\uDE41", ":(");
      EmojiDict.Add("\uD83D\uDE06", "XD");
      EmojiDict.Add("\uD83D\uDE1B", ":P");
      EmojiDict.Add("\uD83D\uDE1D", "XP");
      EmojiDict.Add("\uD83D\uDE1C", ";P");
      EmojiDict.Add("\uD83D\uDE09", ";)");
      EmojiDict.Add("\uD83D\uDE2E", ":o");
      EmojiDict.Add("\uD83D\uDE10", ":|");
      EmojiDict.Add("\uD83D\uDE01", ":D");
      EmojiDict.Add("\uD83D\uDE00", ":D");
      EmojiDict.Add("\uD83D\uDE2C", "8D");
      EmojiDict.Add("\uD83D\uDE20", ">:(");
      EmojiDict.Add("\uD83D\uDE21", ">8(");
      EmojiDict.Add("\uD83D\uDE22", ":~(");
      EmojiDict.Add("\uD83D\uDE17", ":*");
      EmojiDict.Add("\u2764\uFE0F", "<3");
      EmojiDict.Add("\uD83D\uDC94", "</3");
    }*/
  }
}
