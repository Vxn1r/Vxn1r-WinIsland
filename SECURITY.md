# Security policy

WinIsland handles local desktop state and optional camera/screenshot capture. Please do not include private screenshots, camera images, access tokens, or other personal information in a public issue.

For a suspected security issue, contact the repository owner privately through GitHub's security advisory feature where available. If private reporting is unavailable, open an issue containing only a high-level description and ask for a private contact route.

## Security design notes

- The app is a normal user-mode desktop process (`asInvoker`).
- It has no network client, analytics, telemetry, remote-control server, or updater.
- Spotify integration uses the local Windows media-session API rather than Spotify credentials.
- Screenshot capture and camera startup require explicit UI actions.
- The camera shuts down when the island is collapsed or the app exits.
- Published binaries are not code-signed. Verify the release tag, workflow run, and source before running a downloaded binary.
