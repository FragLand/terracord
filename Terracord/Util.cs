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
using System.Collections.Generic;
using System.IO;

namespace FragLand.TerracordPlugin
{
  class Util
  {
    // Exit values
    //private const int ExitSuccess = 0; // unused for now
    public const int ExitFailure = -1;

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
        StreamWriter logFile = new StreamWriter($"tshock{Path.DirectorySeparatorChar}terracord.log", true);
        // Write to console first in case file is unavailable
        string logEntry = $"[{DateTime.Now.ToString(Config.TimestampFormat, Config.Locale)}] [{severity.ToString()}] {logText.ToString(Config.Locale)}";
        Console.WriteLine($"Terracord: {logEntry}");
        Console.ResetColor();
        logFile.WriteLine(logEntry);
        logFile.Close();
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
    /// Calculates plugin uptime
    /// </summary>
    /// <returns>uptime as a string</returns>
    public static string Uptime()
    {
      TimeSpan elapsed = DateTime.Now.Subtract(Terracord.startTime);
      return $"{elapsed.Days} day(s), {elapsed.Hours} hour(s), {elapsed.Minutes} minute(s), and {elapsed.Seconds} second(s)";
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
