# Cotton Mobile

Native Android mobile client for Cotton Cloud, built with .NET MAUI.

Cotton Mobile backs up new files from selected Android folders and photo/video albums to a Cotton Cloud instance. It supports automatic uploads, upload progress, pause and resume, and optional removal of local originals after the cloud copy is confirmed.

Folder sources are checked periodically and when the app opens. Media sources also trigger checks when Android reports new photos or videos. Background timing depends on Android scheduling, network availability, and battery restrictions; immediate upload is not guaranteed. Use Run all in the app to request a check now.

The current app provides upload-only backup. It does not replace changed cloud files, download files, or provide two-way synchronization.

## Project Links

| Resource | Link |
| --- | --- |
| Cotton Cloud | [cottoncloud.dev](https://cottoncloud.dev) |
| Source repository | [bvdcode/cotton-mobile](https://github.com/bvdcode/cotton-mobile) |
| Google Play testing | [Open testing page](https://play.google.com/apps/testing/dev.cottoncloud.app) |
| Latest APK | [Download from GitHub Releases](https://github.com/bvdcode/cotton-mobile/releases/latest/download/CottonCloud-Android.apk) |

## Android App

- Package ID: `dev.cottoncloud.app`
- Target framework: `.NET MAUI / net10.0-android`

## Repository Structure

- `src/Cotton.Mobile` — Android application.
- `src/Cotton.Mobile.Core` — platform-independent application logic.
- `src/Cotton.Mobile.Tests` — unit and contract tests.
- `scripts/ci` — tests for release automation.
- `scripts/mobile` — Android build, runtime verification, and release commands.
- `store/google-play` — Google Play listing metadata and graphics.

## Development

```shell
dotnet restore Cotton.Mobile.slnx
dotnet test --project src/Cotton.Mobile.Tests/Cotton.Mobile.Tests.csproj
dotnet build src/Cotton.Mobile/Cotton.Mobile.csproj -f net10.0-android -c Debug
```

### Upload UI verification

The emulator checks exercise offline commands, pause availability during uploads, source selection, scrolling, and the default setting for keeping originals. They use display fixtures without a server account. Authenticated uploads and server-copy integrity require a separate integration test.

```shell
dotnet build src/Cotton.Mobile/Cotton.Mobile.csproj -f net10.0-android -c Debug -p:CottonUiTests=true -p:OutputPath=bin/UploadUiTests/
adb -s emulator-5554 install -r src/Cotton.Mobile/bin/UploadUiTests/dev.cottoncloud.app.debug-Signed.apk
python scripts/mobile/test-android-upload-ui.py --serial emulator-5554 --output .qa/upload-ui --full
```

Use a fresh Android emulator. The runner temporarily changes its network, display, font scale, and theme settings. `--full` covers phone, narrow phone, tablet, landscape, and enlarged text in both themes. `--dashboard-only` limits a repeat check to upload feedback and controls. UI test scenarios are excluded from normal Debug and Release builds.

### Authenticated upload verification

The upload check exercises the installed Android application against a test account. It uploads 17 generated files including a 64 MiB file, stops the process during that transfer, verifies automatic recovery, downloads and checks every cloud copy, and runs again to confirm that file IDs remain unchanged. It also checks that Android originals remain intact. Each run uses a unique subfolder and removes that subfolder from Android and the cloud when finished.

First, sign in on an isolated emulator and configure a **Selected folder** source under `/sdcard/Documents/<local root>`, connected to a dedicated cloud folder. Keep removal of originals disabled. Save the test account credentials in a local JSON file containing `username` and `password`.

Create a configuration file with the matching source and cloud folder:

```json
{
  "server": "https://<server profile>",
  "accountFile": "<account credentials file>",
  "serial": "emulator-5554",
  "package": "dev.cottoncloud.app",
  "androidSourceDirectory": "/sdcard/Documents/UploadChecks",
  "cloudFolderId": "<cloud folder id>",
  "outputDirectory": ".qa/upload-checks"
}
```

```shell
dotnet run --project scripts/mobile/upload-check/UploadCheck.csproj --configuration Release -- <configuration.json>
```

The command requires `adb` on PATH and an already configured, unpaused source. It accepts emulator serials only. Local results include the source files, diagnostic logs, and `result.json`. CI builds this command; authenticated execution requires the configured emulator and test account.
