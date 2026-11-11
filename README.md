# WinIsland

A local-first, Dynamic Island-style desktop control centre for Windows 11. Designed around the Dell 15 260 with Intel Core Ultra 5 225H, and built for Windows 11 x64 PCs generally.

![WinIsland interface concept](docs/preview.png)

> This is a design preview, not a screenshot from a running build.

## Features

| Feature | What it does |
| --- | --- |
| Compact island | Top-centre pill that expands into a control panel; press `Esc` to collapse. |
| Battery | Live percentage, charging state, and power source. |
| Volume | Reads and changes the default Windows output volume, including mute. |
| Brightness | Reads and changes built-in display brightness through the Windows WMI monitor interface when the driver exposes it. |
| Spotify | Reads the active Spotify Windows media session and offers previous, play/pause, and next controls. No Spotify login or API key. |
| Screenshot | Captures all connected displays only after the user presses the button and chooses a save location. |
| Camera | Local camera preview and photo capture only after the user explicitly opens the camera. It stops when the island collapses or the app exits. |
| System tray | Reopen or exit WinIsland from the notification area. |

## Install

1. Open the repository's [Releases](https://github.com/Vxn1r/Vxn1r-WinIsland/releases) page.
2. Download `WinIsland-win-x64.zip` from the latest release.
3. Extract the complete ZIP to a normal folder.
4. Double-click `Install.bat` and choose whether WinIsland should start when you sign in.
5. Open **Start** and search for **WinIsland**.

The installer copies the app to `%LOCALAPPDATA%\Programs\WinIsland` and adds Start Menu shortcuts. It does not require administrator permission. Use **Uninstall WinIsland** in the Start Menu to remove the app and its optional startup shortcut.

## Compatibility and limitations

- Windows 11 x64. The release is self-contained and includes the .NET runtime.
- Brightness support depends on the laptop panel and graphics driver exposing the WMI brightness interface. Some external monitors do not expose it.
- Spotify controls require the Spotify desktop app to publish a Windows media session.
- Camera access depends on Windows camera privacy settings. Camera frames remain local to the app.
- Multi-display screenshots use the Windows virtual desktop bounds.
- The release is not code-signed, so Windows SmartScreen may show an unknown-publisher warning. Verify the release and source before running it.

## Build from source

Install the .NET 10 SDK and run these commands from the repository root in PowerShell:

```powershell
dotnet restore .\WinIsland.sln
dotnet build .\WinIsland.sln --configuration Release
dotnet test .\tests\WinIsland.Tests\WinIsland.Tests.csproj --configuration Release

dotnet publish .\src\WinIsland\WinIsland.csproj `
  --configuration Release `
  --runtime win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  --output .\publish
```

GitHub Actions runs restore, build, and unit tests on a Windows runner. A successful push to `main` creates the initial `v0.1.0` release. For later releases, push a new version tag such as `v0.2.0` after updating the project version in `src/WinIsland/WinIsland.csproj` and the changelog.

## Privacy and security

- No analytics, telemetry, advertisements, cloud sync, updater, or app backend.
- No Spotify OAuth flow, access token, or client secret.
- Screenshot capture is manual and opens a save dialog before capturing.
- Camera activation is manual, visibly indicated in the UI, and stops on collapse or exit.
- Screenshot and camera files are saved only to a location selected by the user.
- The application runs as the current user (`asInvoker`) and does not request administrator privileges.

Open source does not automatically mean secure. Review the code and dependency versions, and verify the release provenance before installing. Report security concerns privately where possible; see [SECURITY.md](SECURITY.md).

## License

MIT. See [LICENSE](LICENSE).
