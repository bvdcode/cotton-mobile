# Cotton Mobile

Native Android mobile client for Cotton Cloud, built with .NET MAUI.

Cotton Mobile connects Android devices to a Cotton Cloud instance for file browsing, uploads, sharing, offline access, transfer status, camera backup, notifications, storage controls, security settings, and diagnostics.

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
dotnet test src/Cotton.Mobile.Tests/Cotton.Mobile.Tests.csproj
dotnet build src/Cotton.Mobile/Cotton.Mobile.csproj -f net10.0-android -c Debug
```
