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

using System.IO;
using FragLand.TerracordPlugin;
using Xunit;

namespace FragLand.TerracordPluginTests
{
  public class TerracordTest
  {
    /// <summary>
    /// Tests terracord.xml configuration file generation
    /// </summary>
    [Fact]
    public void ConfigGenerateTest()
    {
      Config.Generate();
      Assert.False(File.Exists($"tshock{Path.DirectorySeparatorChar}terracord.xml"));
    }

    /// <summary>
    /// Validates terracord.xml configuration file option values
    /// </summary>
    [Fact]
    public void ConfigParseTest()
    {
      Config.Parse();
      Assert.Equal("ABC", Config.BotToken);
      Assert.IsType<string>(Config.BotToken);
      Assert.Equal("123", Config.ChannelId.ToString());
      Assert.IsType<ulong>(Config.ChannelId);
      Assert.Equal("123", Config.OwnerId.ToString());
      Assert.IsType<ulong>(Config.OwnerId);
      Assert.Equal('!', Config.CommandPrefix);
      Assert.IsType<char>(Config.CommandPrefix);
      Assert.True(Config.RelayCommands);
      Assert.IsType<bool>(Config.RelayCommands);
      Assert.True(Config.RemoteCommands);
      Assert.IsType<bool>(Config.RemoteCommands);
      Assert.Equal("Terraria", Config.BotGame);
      Assert.IsType<string>(Config.BotGame);
      Assert.Equal("300", Config.TopicInterval.ToString());
      Assert.IsType<uint>(Config.TopicInterval);
      Assert.Equal("255", Config.BroadcastColor[0].ToString());
      Assert.IsType<byte>(Config.BroadcastColor[0]);
      Assert.Equal("215", Config.BroadcastColor[1].ToString());
      Assert.IsType<byte>(Config.BroadcastColor[1]);
      Assert.Equal("0", Config.BroadcastColor[2].ToString());
      Assert.IsType<byte>(Config.BroadcastColor[2]);
      Assert.False(Config.SilenceBroadcasts);
      Assert.IsType<bool>(Config.SilenceBroadcasts);
      Assert.False(Config.SilenceChat);
      Assert.IsType<bool>(Config.SilenceChat);
      Assert.False(Config.SilenceSaves);
      Assert.IsType<bool>(Config.SilenceSaves);
      Assert.False(Config.IgnoreChat);
      Assert.IsType<bool>(Config.IgnoreChat);
      Assert.True(Config.LogChat);
      Assert.IsType<bool>(Config.LogChat);
      Assert.Equal("0", Config.MessageLength.ToString());
      Assert.IsType<int>(Config.MessageLength);
      Assert.False(Config.DebugMode);
      Assert.IsType<bool>(Config.DebugMode);
      Assert.Equal("en-US", Config.LocaleString);
      Assert.IsType<string>(Config.LocaleString);
      Assert.Equal("<%u%@Discord>", Config.AuthorFormat.ToString());
      Assert.IsType<string>(Config.AuthorFormat);
      Assert.Equal("MM/dd/yyyy HH:mm:ss zzz", Config.TimestampFormat);
      Assert.IsType<string>(Config.TimestampFormat);
      Assert.False(Config.AbortOnError);
      Assert.IsType<bool>(Config.AbortOnError);
    }
  }
}
