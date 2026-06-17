# Cotton Mobile

Native Cotton Cloud mobile client for Android, built with .NET MAUI.

<p>
  <a href="https://play.google.com/apps/testing/dev.cottoncloud.app"><img alt="Get Cotton Cloud on Google Play" src="https://img.shields.io/badge/Google%20Play-Get%20the%20app-414141?style=for-the-badge&logo=googleplay&logoColor=white"></a>
  <a href="https://github.com/bvdcode/cotton-mobile/releases/latest/download/CottonCloud-Android.apk"><img alt="Download Cotton Cloud APK from GitHub" src="https://img.shields.io/badge/GitHub-APK%20download-181717?style=for-the-badge&logo=github&logoColor=white"></a>
</p>

Cotton Mobile is the focused phone client for Cotton Cloud: sign in to an instance, browse files, preview common formats, and use native Android open/share/download flows without turning the app into a noisy sync dashboard too early.

## Install

| Channel | Use it for | Link |
| --- | --- | --- |
| Google Play testing | Opted-in internal/closed testers | [Open testing page](https://play.google.com/apps/testing/dev.cottoncloud.app) |
| GitHub APK | Direct Android install when Play testing is inconvenient | [Download latest APK](https://github.com/bvdcode/cotton-mobile/releases/latest/download/CottonCloud-Android.apk) |

The APK link points at the latest GitHub Release asset, not a temporary workflow artifact.

## Local Android Loop

```bash
scripts/mobile/build-android-debug.sh
COTTON_ADB_SERIAL=emulator-5554 scripts/mobile/install-android-debug.sh
```

Use the emulator for fast visual checks, then batch real-device smoke tests on the Samsung A53 over ADB when the slice is worth testing on hardware.

## Release Flow

Signed Android builds are produced by the `Mobile Android` workflow.

- Use `build_signed_release=true` to produce signed APK/AAB artifacts.
- Use `publish_github_release=true` to update the stable GitHub APK download.
- Use `upload_to_google_play=true` only for intentional Play testing uploads.

The release flow keeps direct APK testing, Play dogfooding, and store submission separate on purpose.
