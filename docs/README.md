Terracord
=========
Terracord is a [Discord](http://discordapp.com/) <-> [Terraria](http://terraria.org/) bridge plugin for
[TShock](http://tshock.co/). The plugin enables the bi-directional flow of messages between a Discord text
channel and a TShock server. This project is inspired by [DiscordSRV](https://github.com/DiscordSRV/DiscordSRV)
which is a Discord <-> [Minecraft](http://www.minecraft.net/) chat relay plugin for [PaperMC](http://papermc.io/)
and [Spigot](http://www.spigotmc.org/).

Terracord is written in [C#](https://docs.microsoft.com/en-us/dotnet/csharp/) and is licensed under the terms of
the [GPLv3](http://www.gnu.org/licenses/gpl-3.0.en.html). This project makes use of
[Discord.Net](https://github.com/discord-net/Discord.Net) and the [Terraria API](https://github.com/Pryaxis/TerrariaAPI-Server).

### Installation
1. Simply copy `Terracord.dll` and its dependencies into your TShock `ServerPlugins` directory. The dependencies should
be contained in any release zip archive and includes the following files: `Discord.Net.Core.dll`, `Discord.Net.Rest.dll`,
`Discord.Net.WebSocket.dll`, `Newtonsoft.Json.dll`, `System.Collections.Immutable.dll`, and `System.Interactive.Async.dll`.

2. Edit `terracord.json` to set your bot token, bot prefix (`!` is the default prefix), and Discord channel ID. The
channel ID can be obtained by enabling developer mode in your Discord application and then copying the ID of the relevant
text channel you want to relay Terraria messages to. A channel ID appears as a long string of numbers. The process is described
[here](https://support.discordapp.com/hc/en-us/articles/206346498-Where-can-I-find-my-User-Server-Message-ID-). In order
to obtain a bot token, you will need to create a Discord bot application. The process is described
[here](https://github.com/reactiflux/discord-irc/wiki/Creating-a-discord-bot-&-getting-a-token).

3. Restart your TShock server to load the plugin.

:warning: Unfortunately, Terracord may not work with [Mono](https://www.mono-project.com/). This is due to Discord.Net
not supporting Mono.

### Support

Feel free to [submit an issue](https://github.com/FragLand/terracord/issues/new) if you require assistance. You are also
welcome to join our Discord server at https://discord.frag.land/. Any contributions are appreciated.
