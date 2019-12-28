[![AppVeyor build status](https://ci.appveyor.com/api/projects/status/github/FragLand/terracord?svg=true)](https://ci.appveyor.com/project/ldilley/terracord)
[![Travis build status](https://travis-ci.com/FragLand/terracord.svg?branch=master)](https://travis-ci.com/FragLand/terracord)

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

### Building
#### Visual Studio
1. Download and install [Visual Studio](https://visualstudio.microsoft.com/) if you do not have the software. The community
edition is free and contains the essentials to build Terracord. In particular, you want the ".NET desktop development" workload. The "NuGet package manager" is also required to pull in the Discord.Net dependencies. Other individual components such as
debuggers, profilers, "Git for Windows", and the "GitHub extension for Visual Studio" may be useful.

2. Obtain a copy of the Terracord source code if you have not already. This can be performed with
`git clone https://github.com/FragLand/terracord.git`. You may alternatively
[download a zip archive of the source](https://github.com/FragLand/terracord/archive/master.zip) and extract the contents
to an arbitrary location.

3. Download the [release archive of TShock](https://github.com/Pryaxis/TShock/releases/download/v4.3.26/tshock_4.3.26.zip).

4. Create a directory named `lib` at the same path where `Terracord.sln` resides.

5. Extract `OTAPI.dll`, `TerrariaServer.exe`, and `TShockAPI.dll` from the TShock zip archive and then place these 3 files
under the `lib` directory you recently created during step 4.

6. Open `Terracord.sln` using Visual Studio.

7. NuGet should automatically download Discord.Net and its dependencies based on `Terracord.csproj`. If not, you can manually
install `Discord.Net.Core` and `Discord.Net.WebSocket` via NuGet. You may also attempt to right-click the solution in the
"Solution Explorer" of Visual Studio and then left-click "Restore NuGet Packages".

8. Use `Build->Build Solution` or `ctrl+shift+b` to build Terracord.

9. If all goes well, you should have a shiny new `Terracord.dll` at the path referenced in the build output. Enjoy!

#### Mono
:warning: As mentioned previously, building Terracord or loading `Terracord.dll` with Mono may not work considering
Discord.Net does not support this. Therefore, the following steps should be considered experimental.

1. Install Mono. Under [Debian](http://www.debian.org/), this can be achieved via:

   `apt-get install mono-complete`

2. Obtain a copy of the Terracord source code:

   `git clone https://github.com/FragLand/terracord.git`

   Or:

   `wget https://github.com/FragLand/terracord/archive/master.zip && unzip master.zip`

3. Download and extract TShock:

   `wget https://github.com/Pryaxis/TShock/releases/download/v4.3.26/tshock_4.3.26.zip && unzip tshock_4.3.26.zip`

4. Create a directory named `lib` at the same path where `Terracord.sln` resides:

   `mkdir terracord/lib`

5. Copy `OTAPI.dll`, `TerrariaServer.exe`, and `TShockAPI.dll` to `lib`:

   `cp OTAPI.dll TerrariaServer.exe ServerPlugins/TShockAPI.dll terracord/lib`

6. Install dependencies:

   `cd terracord`

   `nuget restore Terracord.sln`

7. Begin build:

   `xbuild`

8. With luck, a wild `Terracord.dll` will appear.

### Contributing and Support
Feel free to [submit an issue](https://github.com/FragLand/terracord/issues/new) if you require assistance or would like to
make a feature request. You are also welcome to join our Discord server at https://discord.frag.land/. Any contributions such
as plugin testing and pull requests are appreciated. Please see the
[fork and pull guide](https://help.github.com/en/github/collaborating-with-issues-and-pull-requests/creating-a-pull-request-from-a-fork)
for direction if you are not certain how to submit a pull request.
