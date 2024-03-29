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
      Assert.True(File.Exists($"tshock{Path.DirectorySeparatorChar}Terracord{Path.DirectorySeparatorChar}terracord.xml"));
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
      Assert.Equal("!", Config.CommandPrefix);
      Assert.IsType<string>(Config.CommandPrefix);
      Assert.True(Config.RelayCommands);
      Assert.IsType<bool>(Config.RelayCommands);
      Assert.True(Config.RemoteCommands);
      Assert.IsType<bool>(Config.RemoteCommands);
      Assert.Equal("Administrators Moderators", Config.AuthorizedRoles);
      Assert.IsType<string>(Config.AuthorizedRoles);
      Assert.Equal("$server_name: $world_name: $player_count/$player_slots", Config.BotGame);
      Assert.IsType<string>(Config.BotGame);
      Assert.Equal("300", Config.TopicInterval.ToString());
      Assert.IsType<uint>(Config.TopicInterval);
      Assert.Equal("Relay offline", Config.OfflineTopic);
      Assert.IsType<string>(Config.OfflineTopic);
      Assert.Equal("$server_name: $world_name | $player_count/$player_slots players online | Server online for $uptime | Last update: $current_time", Config.OnlineTopic);
      Assert.IsType<string>(Config.OnlineTopic);
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
      Assert.False(Config.AnnounceReconnect);
      Assert.IsType<bool>(Config.AnnounceReconnect);
      Assert.Equal("**:white_check_mark: Relay online.**", Config.AvailableText);
      Assert.IsType<string>(Config.AvailableText);
      Assert.Equal("**:octagonal_sign: Relay offline.**", Config.UnavailableText);
      Assert.IsType<string>(Config.UnavailableText);
      Assert.Equal("**:green_circle: $player_name has joined the server.**", Config.JoinText);
      Assert.IsType<string>(Config.JoinText);
      Assert.Equal("**:red_circle: $player_name has left the server.**", Config.LeaveText);
      Assert.IsType<string>(Config.LeaveText);
      Assert.Equal("**:mega: Broadcast:** $message", Config.BroadcastText);
      Assert.IsType<string>(Config.BroadcastText);
      Assert.Equal("**[$group_name]<$player_name>** $message", Config.PlayerText);
      Assert.IsType<string>(Config.PlayerText);
      Assert.Equal("<$user_name@Discord> $message", Config.ChatText);
      Assert.IsType<string>(Config.ChatText);
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
      Assert.Equal("MM/dd/yyyy HH:mm:ss zzz", Config.TimestampFormat);
      Assert.IsType<string>(Config.TimestampFormat);
      Assert.False(Config.AbortOnError);
      Assert.IsType<bool>(Config.AbortOnError);
      Assert.False(Config.ConvertEmoticons);
      Assert.IsType<bool>(Config.ConvertEmoticons);
    }
  }
}
