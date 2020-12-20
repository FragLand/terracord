[![AppVeyor build status](https://img.shields.io/appveyor/ci/ldilley/terracord?label=AppVeyor%20build%20status)](https://ci.appveyor.com/project/ldilley/terracord)
[![Travis CI build status](https://img.shields.io/travis/com/FragLand/terracord?label=Travis%20CI%20build%20status)](https://travis-ci.com/FragLand/terracord)
[![Code Climate maintainability](https://img.shields.io/codeclimate/maintainability-percentage/FragLand/terracord?label=Code%20Climate%20maintainability)](https://codeclimate.com/github/FragLand/terracord/maintainability)
[![CodeFactor grade](https://img.shields.io/codefactor/grade/github/FragLand/terracord?label=CodeFactor%20quality)](https://www.codefactor.io/repository/github/fragland/terracord)
[![Discord](https://img.shields.io/discord/540333638479380487?label=Discord)](https://discord.frag.land/)

Terracord
=========
Terracord is a [Discord](https://discord.com/) ↔ [Terraria](https://terraria.org/) bridge plugin for
[TShock](https://tshock.co/). The plugin enables the bi-directional flow of messages between a Discord text
channel and a TShock server. This project is inspired by [DiscordSRV](https://github.com/DiscordSRV/DiscordSRV)
which is a Discord ↔ [Minecraft](http://www.minecraft.net/) chat relay plugin for [PaperMC](https://papermc.io/)
and [Spigot](https://www.spigotmc.org/).

Terracord is written in [C#](https://docs.microsoft.com/en-us/dotnet/csharp/) and is licensed under the terms of
the [GPLv3](https://www.gnu.org/licenses/gpl-3.0.en.html). This project makes use of
[Discord.Net](https://github.com/discord-net/Discord.Net) and the [Terraria API](https://github.com/Pryaxis/TerrariaAPI-Server).

<details>
<summary>Installation and Setup</summary>

### Discord Bot
1. Follow the instructions [here](https://github.com/reactiflux/discord-irc/wiki/Creating-a-discord-bot-&-getting-a-token)
to create a bot and invite it to your server. Make note of the bot token as you'll need it later.

2. Give the bot the following server-wide permissions:

   <details>
   <summary>Permissions</summary>
   
   | Permission | Required | Effect | Scope |
   | -- | -- | -- | -- |
   | Read Text Channels & See Voice Channels/Read Messages | ✔ Yes | Allows the bot to view channels and messages within a text channel | Server/Channel |
   | Send Messages | ✔ Yes | Allows the bot to send messages in a text channel | Server/Channel |
   | Read Message History | ✔ Yes | Allows the bot to see previous messages in a text channel | Server/Channel |
   | Change Nickname | ❌ No | Allows the bot to change its own nickname when the configuration is reloaded | Server |
   | Embed Links | ❌ No | Allows the bot to embed links within a text channel | Server/Channel |
   | Manage Channel(s) | ❌ No | Allows the bot to dynamically update the channel topic with info about the Terraria server. | Server/Channel |
   
   - **Server** scope means the permission is added to the bot's role in <kbd><kbd>Server Settings</kbd>⇒<kbd>Roles</kbd></kbd>.  
   - **Channel** scope means the permission is added to the bot (or its role) directly in the desired text channel using
   <kbd><kbd>Edit Channel</kbd>⇒<kbd>Permissions</kbd></kbd>.  
   - **Server/Channel** scope means the permission can either be a **Server** or **Channel** permission.  
   </details>

3. Copy the ID of the desired text channel following the instructions
[here](https://support.discordapp.com/hc/en-us/articles/206346498-Where-can-I-find-my-User-Server-Message-ID-) and also make
note of it.

### TShock Plugin
1. Copy `Terracord.dll` and its dependencies into your TShock `ServerPlugins` directory. The dependencies are the following:
`Discord.Net.Core.dll`, `Discord.Net.Rest.dll`, `Discord.Net.WebSocket.dll`, `Newtonsoft.Json.dll`,
`System.Collections.Immutable.dll`, and `System.Interactive.Async.dll`. These files should be contained in any release archive.

   Ensure that the version of `Newtonsoft.Json.dll` copied to the `ServerPlugins` directory is ≥ 11.0.2. This is a required
   dependency of Discord.Net. The instance of this DLL included with TShock 4.4.0 is older (10.0.3) and using it results in
   the inability to establish a connection to the Discord service.

2. Edit `terracord.xml` to set your bot token and Discord channel ID. The [Discord Bot](#Discord-Bot) section demonstrates how to
obtain this pair of items. `terracord.xml` should be saved to the `tshock > Terracord` directory. Other settings in this configuration
file may also be changed to your liking.

3. Restart your TShock server to load the plugin. For review or troubleshooting purposes, `terracord.log` can be found in
the `tshock > Terracord` directory.

:warning: Unfortunately, Terracord may not work with [Mono](https://www.mono-project.com/). This is due to Discord.Net
not supporting Mono.
</details>

<details>
<summary>Discord Commands</summary>

| Command | Description |
| -- | -- |
| `help` | Display commands list |
| `playerlist` | Display online players |
| `serverinfo` | Display server details |
| `uptime` | Displays plugin uptime |

If a command is not in the above list and the issuing Discord user has one of the admin roles or is the bot owner (both configured
in `terracord.xml`), the command will be forwarded onto the Terraria server. The server and any relevant plugins will handle the
command at this point and provide output if applicable.
</details>

<details>
<summary>Building</summary>

#### Visual Studio
1. Download and install [Visual Studio](https://visualstudio.microsoft.com/) if you do not have the software. The community
edition is free and contains the essentials to build Terracord. In particular, you want the ".NET desktop development" workload.
The "NuGet package manager" is also required to pull in the Discord.Net dependencies. Other individual components such as
debuggers, profilers, "Git for Windows", and the "GitHub extension for Visual Studio" may be useful.

2. Obtain a copy of the Terracord source code if you have not already. This can be performed with
`git clone https://github.com/FragLand/terracord.git`. You may alternatively
[download a zip archive of the source](https://github.com/FragLand/terracord/archive/master.zip) and extract the contents
to an arbitrary location.

3. Download the latest [TShock release](https://github.com/Pryaxis/TShock/releases).

4. Create a directory named `lib` at the same path where `Terracord.sln` resides.

5. Extract `OTAPI.dll`, `TerrariaServer.exe`, and `TShockAPI.dll` from the TShock zip archive and then place these 3 files
under the `lib` directory you recently created during step 4.

6. Open `Terracord.sln` using Visual Studio.

7. NuGet should automatically download Discord.Net and its dependencies based on `Terracord.csproj`. If not, you can manually
install `Discord.Net.Core` and `Discord.Net.WebSocket` via NuGet. You may also attempt to right-click the solution in the
"Solution Explorer" of Visual Studio and then left-click "Restore NuGet Packages".

8. Use <kbd><kbd>Build</kbd>⇒<kbd>Build Solution</kbd></kbd> or <kbd><kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>b</kbd></kbd> to
build Terracord.

9. If all goes well, you should have a shiny new `Terracord.dll` at the path referenced in the build output. Enjoy!

#### .NET Core
1. Install [.NET Core SDK](https://dotnet.microsoft.com/download/dotnet-core/3.1). .NET Core SDK 3.1.100 is known to
successfully build Terracord. You can also [configure various Linux package managers](https://docs.microsoft.com/en-us/dotnet/core/install/linux-package-manager-debian10)
to install .NET core. This has the added benefit of being able to easily update the software.

2. Obtain a copy of the Terracord source code:

   `git clone https://github.com/FragLand/terracord.git`

   Or:

   `wget https://github.com/FragLand/terracord/archive/master.zip && unzip master.zip`

3. Download and extract the latest [TShock release](https://github.com/Pryaxis/TShock/releases):

   `wget https://github.com/Pryaxis/TShock/releases/download/vx.x.x/tshock_x.x.x.zip && unzip tshock_x.x.x.zip`

4. Create a directory named `lib` at the same path where `Terracord.sln` resides:

   `mkdir terracord/lib`

5. Copy `OTAPI.dll`, `TerrariaServer.exe`, and `TShockAPI.dll` to `lib`:

   `cp OTAPI.dll TerrariaServer.exe ServerPlugins/TShockAPI.dll terracord/lib`

6. Install dependencies:

   `cd terracord`

   `dotnet restore`

7. Begin build:

   `dotnet build -c <Debug|Release>`

8. You should now have a `Terracord.dll`.

#### Mono
:warning: As mentioned previously, loading `Terracord.dll` with Mono may not work considering Discord.Net does not
support this. Therefore, the following steps should be considered experimental.

1. Install Mono and NuGet. Under [Debian](http://www.debian.org/), this can be achieved via:

   `apt-get install mono-complete nuget`

2. Obtain a copy of the Terracord source code:

   `git clone https://github.com/FragLand/terracord.git`

   Or:

   `wget https://github.com/FragLand/terracord/archive/master.zip && unzip master.zip`

3. Download and extract the latest [TShock release](https://github.com/Pryaxis/TShock/releases):

   `wget https://github.com/Pryaxis/TShock/releases/download/vx.x.x/tshock_x.x.x.zip && unzip tshock_x.x.x.zip`

4. Create a directory named `lib` at the same path where `Terracord.sln` resides:

   `mkdir terracord/lib`

5. Copy `OTAPI.dll`, `TerrariaServer.exe`, and `TShockAPI.dll` to `lib`:

   `cp OTAPI.dll TerrariaServer.exe ServerPlugins/TShockAPI.dll terracord/lib`

6. Install dependencies:

   `cd terracord`

   `nuget restore Terracord.sln`

7. Begin build:

   `xbuild /p:Configuration=<Debug|Release> Terracord.sln`
   
   Or:
   
   `msbuild /p:Configuration=<Debug|Release> Terracord.sln`

8. With luck, a wild `Terracord.dll` will appear.
</details>

### Related Projects
[@Dids](https://github.com/Dids) has contributed a Docker image of TShock bundled with Terracord at: https://github.com/Didstopia/terraria-server

### Contributing and Support
Feel free to [submit an issue](https://github.com/FragLand/terracord/issues/new) if you require assistance or would like to
make a feature request. You are also welcome to join our Discord server at https://discord.frag.land/. Any contributions such
as plugin testing and pull requests are appreciated. Please see the
[fork and pull guide](https://help.github.com/en/github/collaborating-with-issues-and-pull-requests/creating-a-pull-request-from-a-fork)
for direction if you are not certain how to submit a pull request.
