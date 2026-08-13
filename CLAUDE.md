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

One solution `src/MQTTnet.TestApp.SimpleServer.sln` with exactly one project:

- `src/MQTTnet.TestApp.SimpleServer/MQTTnet.TestApp.SimpleServer.csproj`, `OutputType` `WinExe`,
  `UseWindowsForms`, the whole application.

Layout inside `src/MQTTnet.TestApp.SimpleServer`:

- `Program.cs`: `Main` with `[STAThread]`, sets the high DPI mode and the visual styles, then
  `Application.Run(new Form1())`. Nothing else.
- `Form1.cs`: everything else. The three MQTT lifecycles (server, publisher, subscriber), the
  button click handlers, the message formatting for the output box and the timer that enables and
  disables the buttons.
- `Form1.Designer.cs`: the generated designer code, `Form1.resx`: the generated resource file with
  nothing but the three `resheader` entries.
- `GlobalUsings.cs`: all usings of the project, including the alias `Timer`.

Repository root: `README.md` (the only user documentation, badges and a link to the changelog),
`Changelog.md`, `License.txt` (MIT), `.gitignore` and `.gitattributes`. The `.editorconfig` sits one
level down in `src`. There is no `Updating.md`, no `HowToUse.md`, no screenshots and no `.github`
folder.

## Build

```powershell
dotnet build src/MQTTnet.TestApp.SimpleServer.sln
```

- Single target framework `net9.0-windows`, no multi-targeting. Windows only, it is Windows Forms.
- All build properties live directly in the one `.csproj`. There is **no** `Directory.Build.props`
  in this repository.
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
- There are no tests. A behaviour change is verified by starting the application: enter a port,
  `Start` the server, `Start` the publisher and the subscriber, `Random`, `Publish`, and the message
  has to appear in the big text box at the bottom.

## Code conventions

Follow the surrounding code, it is consistent throughout every hand written file:

- File header comment block with `<copyright file="..." company="Hämmer Electronics">` and a
  `<summary>`, then the file-scoped namespace.
- XML doc comments on every type and every member, private members included, no exceptions. Event
  handlers document their `sender` and `e` parameters even though the text says nothing new.
- `Nullable`, `ImplicitUsings` and `LangVersion latest` are enabled. The MQTT fields are declared
  nullable and their null state is the state of the application: `mqttServer is null` means the
  server is stopped, and the timer derives the button states from exactly that.
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

- **Everything runs against `localhost`.** The host is hardcoded in both client option builders,
  only the port comes from the UI. The server binds
  `MqttServerOptions.DefaultEndpointOptions.Port` to the same value, so the app only ever talks to
  its own server.
- **Every published message is retained.** `ButtonPublish_Click` builds with
  `WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)` and `WithRetainFlag()`, and the
  server runs with `EnablePersistentSessions = true`. A subscriber that starts later therefore
  immediately receives the last message of the topic. That is a feature of the test bench, not a
  bug in the subscriber.
- **The credentials are decoration.** The publisher connects `WithCredentials("username",
  "password")`, the subscriber sends nothing at all, and the server has no validator, so every
  connection is accepted either way.
- **The TLS options do nothing.** `MqttClientTlsOptions` is built with `UseTls = false` plus
  `IgnoreCertificateChainErrors`, `IgnoreCertificateRevocationErrors` and
  `AllowUntrustedCertificates`. Those three only matter once TLS is on, which it never is here.
- **`MqttProtocolVersion.V311` is hardcoded** in both clients, there is no UI for it.
- **The port text box validates by reverting.** `TextBoxPort_TextChanged` keeps the last value that
  parsed as an `int` in the `port` field and writes it back as soon as the text stops parsing, then
  puts the caret at the end. So the box can never hold a non-numeric value, and the field is the
  reason why.
- **A polling timer owns the button states.** A `System.Timers.Timer` with a one second interval
  runs `TimerElapsed`, which `BeginInvoke`s the enabled states of all six buttons and of the port
  box from the null state of the three MQTT fields. No handler enables or disables a button itself,
  so a state change becomes visible with up to one second of delay.
- **`Timer` is an alias.** `GlobalUsings.cs` ends with `global using Timer = System.Timers.Timer;`,
  because `System.Windows.Forms.Timer` would otherwise win through `ImplicitUsings`. Removing the
  alias silently changes which timer is used.
- **Generated control names.** The labels are `label1` to `label5`, the form is `Form1`. Only the
  controls that the code touches got real names (`TextBoxPort`, `ButtonServerStart`,
  `TextBoxSubscriber` and so on). Renaming the rest is churn in generated code.
- **Messages are prepended, not appended.** `TextBoxSubscriber.Text` gets the new line in front of
  the old content, so the newest message is the top line and the box never scrolls on its own.
- **Errors are message boxes.** There is no logging and no status bar. The publish path and the
  server start path catch their exceptions and show `MessageBox.Show(ex.Message, "Error Occurs",
  ...)`, the publisher connect and disconnect events show a box as well. The other handlers do not
  catch, so an exception there ends up in the unhandled exception dialog.
- **`RuntimeIdentifiers win-x64` without a publish.** The property is set in the `.csproj`, but
  nothing in the repository publishes with a RID and the app is framework dependent. A machine that
  runs it needs the Windows Desktop runtime installed.
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
