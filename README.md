This bot is based on [Discord.Net](https://github.com/discord-net/Discord.Net). It's aim is to give a global multiplayer lobby experience. It makes use of the [Master Server](https://github.com/OpenRA/OpenRAMasterServer) and [Resource Center](https://github.com/OpenRA/OpenRA-Resources) APIs. This is inspired by the [OraBot](https://github.com/OpenRA/Orabot/) project.

## Requirements
The project was made using [Visual Studio Code](https://code.visualstudio.com/) with its C# extension.

You will also need to install [.NET Core 8.0 SDK](https://dotnet.microsoft.com/download) build the project.

### Create the application
1. Go to [Applications](https://discord.com/developers/applications) page on Discord Developer portal.
2. Press the **New Application** button.
3. **New Application** page will open. Enter the bot's name in the **name** field.
4. When you're done, press the **create** button.
5. When the app is created, jump to the **bot** section and press the **add bot** button.
6. Once this is done, you will need to copy the **bot's token**. Under **app bot user**, there's a **token** field, press copy **the resulting value**.

### Building the project and configuring the bot
1. Go to `HardVacuumBot` and run `dotnet build` to compile it.
2. Create `App.config` from the template with the token.
3. `Server` and `LobbyChannel` require the IDs from Discord.

### Adding the bot to the server
1. Go back to the app page, and copy the bot's **client ID**.
2. Go to `https://discordapp.com/oauth2/authorize?client_id=the_app_id_here&scope=bot&permissions=0`.
3. On the page, select **the server** (1), and press **authorize** (2).
4. Verify that you are not a robot and you're done! You can now run the bot!
