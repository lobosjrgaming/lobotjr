# LobotJR

An IRC bot that primarily runs the Wolfpack RPG for LobosJR on twitch. It also performs some moderation functions and a few other features that were added as needed.

## Running the Application

To run the application, download the latest version from the [releases page](https://github.com/lobosjrgaming/lobotjr/releases) and extract the application folder. Run LobotJR.Launcher.exe and provide the Client ID and Client Secret for your Twitch app. If you don't already have one, you can create it on the [Twitch Developer Portal](https://dev.twitch.tv/console). You will need to ensure your Twitch app has [http://localhost](http://localhost) as one of the OAuth Redirect URLs in order for the launcher to authenticate you.

The bot is built to run as a separate user from the streamer account. You will be prompted for a broadcaster account and a chat account. These can be the same account, but are intended to be separate. The broadcaster account is the channel the bot will connect to and monitor. Broadcaster permissions are required for the bot to access your subscriber list. The chat account is the account the bot will be logged in as, and is where all whispered commands must be sent to, and where all public responses will be sent from.

## Development Requirements

This app was created on a Windows PC using the .Net Framework 4.6.2. It requires Visual Studio 2022 and .Net Framework 4.6.2 to be installed on the development machine.

## Building Locally

Before attempting to build the app locally, make sure you have installed all development requirements and cloned this repository.

From within Visual Studio, select your configuration (usually Debug) and start the application using the button in the toolbar or the F5 key. For most application debugging, you will want to launch the LobotJR app, not the launcher. Only start debugging on the launcher app when you need to debug the launcher code.

## Running the Tests

To run the tests, use the Test Explorer from within Visual Studio. The tests are not designed to be run asynchronously, as the in-memory database is only created once when the tests are run and is then shared between all tests. This is done due to the high overhead of creating and seeding the in-memory database.

## Deployment

This application was built as a windows console application. To deploy the application, simply place the application folder somewhere on your hard drive. All application files will be generated in that folder, so make sure the application executes with permission to write to that directory. Multiple instances can be run by placing multiple copies if the application folder in different locations on your hard drive.

## Built With

- [.Net Framework 4.6.2](https://dotnet.microsoft.com/en-us/download/dotnet-framework/net462)
- [Sqlite](https://www.sqlite.org/index.html)
- [NLog](https://nlog-project.org/)
- [RestSharp](https://restsharp.dev/)
- [Autofac](https://autofac.org/)
- [EntityFramework](https://learn.microsoft.com/en-us/aspnet/entity-framework)

In addition, we use the following libraries for unit testing

- [MSTest](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-mstest-intro)
- [Moq](https://github.com/devlooped/moq)

## IDE and Extensions

We recommend using [Visual Studio 2022 Community Edition](https://visualstudio.microsoft.com/vs/)

## Contributing

Please read [CONTRIBUTING.md](https://github.com/lobosjrgaming/lobotjr/blob/master/CONTRIBUTING.md) for details on our code of conduct, and the process for submitting pull requests to us.

## Versioning

We use [SemVer](http://semver.org/) for versioning. For the versions available, see the [tags on this repository](https://github.com/lobosjrgaming/lobotjr/tags).

## Authors

- **[LobosJr](https://twitch.tv/LobosJr)** - _Owner/Lead Developer_
- **[EmpyrealHell](https://github.com/EmpyrealHell)** - _Developer_
  - To contact, please send a DM on Twitch to EmpyrealHell

## License

This project is licensed under the GPL 3.0 License - see the [LICENSE.md](https://github.com/EmpyrealHell/wolfpack-rpg-client/blob/master/LICENSE.md) file for details
