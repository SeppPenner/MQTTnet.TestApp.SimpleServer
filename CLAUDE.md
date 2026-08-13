# Project rules for Claude

## What this is

MQTTnet.TestApp.SimpleServer is a Windows Forms test bench for the MQTTnet library. One window
hosts three things at the same time: an MQTT server, a publisher client and a subscriber client,
all talking to `localhost` on the port from the port text box. That is the whole point of the
application, it lets MQTTnet be tried out against itself without installing a broker. The window
title is `MQTT Testing`, the form is still called `Form1`. The assembly was originally created by
[@grammyleung](https://github.com/grammyleung).

The repository is an application, it is **not** published as a NuGet package and it has no
installer: no `GeneratePackageOnBuild`, no push script, no Inno Setup folder. A release ends with
a pushed tag, and a run means starting the executable from `bin`.

One solution `src/MQTTnet.TestApp.SimpleServer.sln` with exactly two projects:

- `src/MQTTnet.TestApp.SimpleServer/MQTTnet.TestApp.SimpleServer.csproj`, `OutputType` `WinExe`,
  `UseWindowsForms`, the application.
- `src/MQTTnet.TestApp.SimpleServer.Tests/MQTTnet.TestApp.SimpleServer.Tests.csproj`, MSTest, added
  in version 1.0.8.0.

Layout inside `src/MQTTnet.TestApp.SimpleServer`:

- `Program.cs`: `Main` with `[STAThread]`, sets the high DPI mode and the visual styles, then
  `Application.Run(new Form1())`. Nothing else.
- `Services/MqttService.cs` plus `Services/IMqttService.cs`: the three MQTT lifecycles. Start and
  stop for the server, the publisher and the subscriber, one publish method, three events
  (`MessageReceived`, `PublisherConnected`, `PublisherDisconnected`) and three properties that say
  what is started. It knows nothing about Windows Forms, which is what makes it testable. New MQTT
  logic belongs here, not in the form.
- `Form1.cs`: the form around that service. Button click handlers that call one service method each
  and show a message box when it throws, the message formatting for the output box, the port text
  box validation and the timer that enables and disables the buttons.
- `Form1.Designer.cs`: the generated designer code, `Form1.resx`: the generated resource file with
  nothing but the three `resheader` entries.
- `GlobalUsings.cs`: all usings of the project, including the alias `Timer`.

Layout inside `src/MQTTnet.TestApp.SimpleServer.Tests`:

- `MqttServiceTests.cs`: the state of a new service, the server that starts twice and stops, a port
  out of range, a subscriber without a server, publishing without a publisher and without a topic,
  the publish that arrives at the subscriber, the retained message that reaches a late subscriber,
  the stopped subscriber that receives nothing, the connect and disconnect events of the publisher
  and the dispose that takes everything down.
- `TestDataProvider.cs`: the topic and the payload both of which the application starts with, the
  two timeouts and `GetFreePort`, which every test uses to get a port of its own.
- `GlobalUsings.cs`: all usings of the test project.

Repository root: `README.md` (the only user documentation, badges and a link to the changelog),
`Changelog.md`, `License.txt` (MIT), `.gitignore` and `.gitattributes`. The `.editorconfig` sits one
level down in `src`. There is no `Updating.md`, no `HowToUse.md`, no screenshots and no `.github`
folder.

## Build

```powershell
dotnet build src/MQTTnet.TestApp.SimpleServer.sln
```

```powershell
dotnet test src/MQTTnet.TestApp.SimpleServer.sln
```

- Single target framework `net10.0-windows` in both projects, no multi-targeting. Windows only, it
  is Windows Forms. The test project carries `UseWindowsForms` as well, because it references the
  application project.
- All build properties live directly in the two `.csproj` files and are duplicated there. There is
  **no** `Directory.Build.props` in this repository.
- `TreatWarningsAsErrors` is enabled, so every warning breaks the build, NuGet warnings (`NU****`)
  from restore included. A clean build reports zero warnings, keep it that way.
- `NU1803` (HTTP source usage during restore) is the one warning suppressed via `NoWarn`. Fix
  warnings instead of extending that list. `NuGetAudit` and `NuGetAuditMode=all` are on, so a
  vulnerable transitive package fails the build too.
- Versions come from GitVersion.MsBuild out of the git tags, for example `1.0.8-1` for the first
  commit after tag `1.0.7`. Never edit a version property or an assembly version by hand.
- Restore needs nuget.org. If a private feed is configured globally on the machine and cannot be
  reached, restore fails with `NU1900` and, because warnings are errors, the build stops. Then build
  with an explicit source:
  `dotnet build src/MQTTnet.TestApp.SimpleServer.sln --source https://api.nuget.org/v3/index.json`.
  The same holds for `dotnet list ... package --outdated`, which additionally needs `--no-restore`
  to skip the failing restore.
- Tests are MSTest, in the single test project `src/MQTTnet.TestApp.SimpleServer.Tests`, which
  follows the same package set as the sibling repositories: `Microsoft.NET.Test.Sdk`,
  `MSTest.TestAdapter`, `MSTest.TestFramework`, `coverlet.collector` and `GitVersion.MsBuild`.
  `dotnet test` runs 11 tests in a few seconds. They need no fixture outside the repository, but
  they do open TCP ports on the loopback interface, because they run a real server with a real
  publisher and a real subscriber. Every test takes a free port of its own, so a test run does not
  collide with an already running instance of the application. Never claim a test run happened
  without running it.
- Beyond the tests, a behaviour change of the form itself is verified by starting the application:
  enter a port, `Start` the server, `Start` the publisher and the subscriber, `Random`, `Publish`,
  and the message has to appear in the big text box at the bottom. The publisher shows a
  `ConnectHandler` message box on connect, which in the UI Automation tree is a window below the
  main window, not a top level window of the process.

## Code conventions

Follow the surrounding code, it is consistent throughout every hand written file:

- File header comment block with `<copyright file="..." company="Hämmer Electronics">` and a
  `<summary>`, then the file-scoped namespace.
- XML doc comments on every type and every member, private members included, no exceptions. Event
  handlers document their `sender` and `e` parameters even though the text says nothing new.
- `Nullable`, `ImplicitUsings` and `LangVersion latest` are enabled. The three MQTT fields of
  `MqttService` are declared nullable and their null state is the state of the application:
  `mqttServer is null` means the server is stopped, that is what `IsServerStarted` reports, and the
  timer of the form derives the button states from exactly that. A start method that fails throws
  the field away instead of keeping a half started object in it.
- New `using` directives go into `GlobalUsings.cs`, inside the existing `#pragma warning disable
  IDE0065` block, never at the top of a file. The editorconfig requires usings inside the namespace
  (`csharp_using_directive_placement=inside_namespace:warning`), which global usings cannot satisfy,
  that is what the pragma is for. Do not add other pragmas. The comment text in that block is
  German because Visual Studio generated it, leave it alone.
- Fields, properties, methods and events are always accessed with `this.` qualification
  (`dotnet_style_qualification_for_*` at severity `warning`).
- `src/.editorconfig` also enforces braces everywhere, no multiple blank lines, four spaces, CRLF,
  UTF-8, file scoped namespaces, `System` usings sorted first and `IDE0005` as warning. Analyzer
  warnings are fixed, not silenced.
- `Form1.Designer.cs` breaks all of that: tabs instead of spaces, a block namespace, no file header.
  It is generated code, leave its shape alone and only touch it where the designer would, which in
  practice means the control declarations, `InitializeComponent` and the event wiring.

## Known quirks

Do not silently "clean up" these, they are existing behaviour:

- **The default endpoint of the server has to be switched on.** `MqttServerOptions` comes with
  `DefaultEndpointOptions.IsEnabled` set to `false`, and a server that is started without it starts
  happily and listens nowhere, so every client gets its connection refused. That is why
  `StartServerAsync` builds the options with `MqttServerOptionsBuilder.WithDefaultEndpoint()`
  instead of setting the port on a bare `new MqttServerOptions()`. Version 1.0.7 did the latter and
  therefore had no working server at all. Do not go back to setting properties on a bare options
  object.
- **Everything runs against `localhost`.** The host is hardcoded in the client options, only the
  port comes from the UI, and the server binds the same port, so the app only ever talks to its own
  server.
- **Every published message is retained.** `PublishAsync` builds with
  `WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)` and `WithRetainFlag()`, and the
  server runs with persistent sessions. A subscriber that starts later therefore immediately
  receives the last message of the topic. That is a feature of the test bench, not a bug in the
  subscriber, and `ASubscriberThatStartsLateReceivesTheRetainedMessage` pins it down.
- **The output box says `AtMostOnce` although the publisher sends `AtLeastOnce`.** The topic filter
  of the subscriber is built without a quality of service level, so it asks for the default, which
  is at most once. A message is delivered with the lower level of the two, and that is the level the
  output line shows. Nothing is broken, the two ends simply ask for different things.
- **The credentials are decoration.** The publisher connects `WithCredentials("username",
  "password")`, the subscriber sends nothing at all, and the server has no validator, so every
  connection is accepted either way. That asymmetry is the reason why `BuildClientOptions` takes a
  `withCredentials` flag.
- **The TLS options do nothing.** `MqttClientTlsOptions` is built with `UseTls = false` plus
  `IgnoreCertificateChainErrors`, `IgnoreCertificateRevocationErrors` and
  `AllowUntrustedCertificates`. Those three only matter once TLS is on, which it never is here.
- **`MqttProtocolVersion.V311` is hardcoded** in both clients, there is no UI for it.
- **The port text box validates by reverting.** `TextBoxPort_TextChanged` keeps the last value that
  parsed as a port number between 1 and 65535 in the `port` field and writes it back as soon as the
  text stops parsing, then puts the caret at the end. So the box can never hold anything else, and
  the field is the reason why. `MqttService` checks the range a second time, because the form is not
  the only possible caller.
- **A polling timer owns the button states.** A `System.Timers.Timer` with a one second interval
  runs `TimerElapsed`, which `BeginInvoke`s the enabled states of all seven buttons and of the port
  box from the three properties of the service. No handler enables or disables a button itself, so a
  state change becomes visible with up to one second of delay. The timer is stopped and disposed in
  the `FormClosed` handler, together with the service, otherwise it would reach a form that is
  already gone.
- **`Timer` is an alias.** `GlobalUsings.cs` ends with `global using Timer = System.Timers.Timer;`,
  because `System.Windows.Forms.Timer` would otherwise win through `ImplicitUsings`. Removing the
  alias silently changes which timer is used.
- **Generated control names.** The labels are `label1` to `label5`, the form is `Form1`. Only the
  controls that the code touches got real names (`TextBoxPort`, `ButtonServerStart`,
  `TextBoxSubscriber` and so on). Renaming the rest is churn in generated code.
- **Messages are prepended, not appended.** `TextBoxSubscriber.Text` gets the new line in front of
  the old content, so the newest message is the top line and the box never scrolls on its own.
- **Errors are message boxes.** There is no logging and no status bar. Every button handler is an
  `async void` method that catches everything and shows `MessageBox.Show(ex.Message, "Error Occurs",
  ...)`, which it has to, because an exception that escapes an `async void` handler ends up in the
  unhandled exception dialog. The publisher connect and disconnect events show a box as well, with
  the caption `ConnectHandler`. Events of the service arrive on a background thread, so the form
  marshals them through `RunOnUserInterfaceThread`.
- **`RuntimeIdentifiers win-x64` without a publish.** The property is set in the `.csproj`, but
  nothing in the repository publishes with a RID and the app is framework dependent. A machine that
  runs it needs the Windows Desktop runtime installed.
- **The commit hash appears twice in the `ProductVersion`.** GitVersion writes an
  `InformationalVersion` that already ends in `+Branch.master.Sha.<hash>`, and the SDK appends the
  source revision on top of it, so the result reads `...Sha.<hash>.<hash>`. It has been that way
  since the move to .NET 8 and it shows up nowhere in the UI, the window title is a fixed string.
  `IncludeSourceRevisionInInformationalVersion` would switch the second half off.
- **AppVeyor badge without CI in the repository.** `README.md` links an AppVeyor build that is
  configured outside of this repository. There is no pipeline file here.
- **`src/MQTTnet.TestApp.SimpleServer.sln.DotSettings`** is tracked and holds nothing but a
  ReSharper user dictionary (`Haemmer`, `H_00E4mmer`, `mqtt`, `Tnet`). Leave it alone.
- **`.gitattributes` is the unmodified Visual Studio template**, every rule below `* text=auto` is
  commented out. With `core.autocrlf=true` on this machine the repository stores LF and the working
  tree gets CRLF. Files written by a script therefore have to be written with CRLF, otherwise the
  working tree disagrees with the `.editorconfig`.

## Releasing

1. Make the change.
2. Add an entry at the top of `Changelog.md` in the existing format:
   `* **Version 1.0.8.0 (2026-08-13)** : Short description.`
3. Commit that.
4. Tag the commit with the plain version number, no `v` prefix (`1.0.7`, `1.0.6`, ...). The existing
   tags are lightweight tags, create new ones the same way.
5. Push the commits and the tag.

The version in the `Changelog.md` has four parts (`1.0.8.0`), the tag has three (`1.0.8`).
GitVersion turns the tag into the assembly version, so an untagged commit produces something like
`1.0.8-1+Branch.master.Sha...`. There is no installer to build and no package to push, so the
release ends with the push.

## Git

- **Never amend a commit.** No `git commit --amend`, not for a typo in the message, not to add a
  forgotten file, not even when the commit is still local. Write a follow-up commit instead. The
  release versions come from tags on exact commits, an amended commit leaves its tag pointing at a
  commit that no longer exists in the branch.

## Writing style

- Commit messages are written **in English only**: short, precise subject line, explanatory body
  when needed.
- Code comments and comments in project files such as `.csproj` are **always English**, regardless
  of the language used in the conversation.
- **No em dashes or en dashes** (`—`, `–`), neither in prose, commit messages, code comments nor
  documentation. Use a regular hyphen, comma, colon, parentheses or a separate sentence.
- German texts (documentation, chat replies) always use real umlauts and ß, never ASCII
  transliterations such as `ae`, `oe`, `ue` or `ss`. Identifiers, file names and configuration keys
  stay unchanged where umlauts are technically undesirable.
