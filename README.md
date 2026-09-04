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
