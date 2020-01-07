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

namespace Terracord
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

    /// <summary>
    /// Writes a log message to terracord.log and the TShock console
    /// </summary>
    /// <param name="logText">the text to log</param>
    public static void Log(string logText, Severity severity)
    {
      try
      {
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
        Console.WriteLine($"Terracord: [{DateTime.Now.ToString(Config.TimestampFormat)}] {logText.ToString()}");
        Console.ResetColor();
        logFile.WriteLine($"[{DateTime.Now.ToString(Config.TimestampFormat)}] {logText.ToString()}");
        logFile.Close();
      }
      catch(Exception e)
      {
        // Log message also gets written to console, so it will be visible
        Log($"Unable to write to terracord.log: {e.Message}", Severity.Error);
        if(Config.AbortOnError)
          Environment.Exit(ExitFailure);
      }
    }
  }
}
