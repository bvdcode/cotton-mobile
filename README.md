# Cotton Mobile

Native Cotton Cloud mobile client for Android, built with .NET MAUI.

<p>
  <a href="https://play.google.com/apps/testing/dev.cottoncloud.app"><img alt="Get Cotton Cloud on Google Play" src="https://img.shields.io/badge/Google%20Play-Get%20the%20app-414141?style=for-the-badge&logo=googleplay&logoColor=white"></a>
  <a href="https://github.com/bvdcode/cotton-mobile/releases/latest/download/CottonCloud-Android.apk"><img alt="Download Cotton Cloud APK from GitHub" src="https://img.shields.io/badge/GitHub-APK%20download-181717?style=for-the-badge&logo=github&logoColor=white"></a>
</p>

Cotton Mobile is the focused phone client for Cotton Cloud: sign in to an instance, browse files, open documents with native Android flows, and keep the interface calm enough to trust every day.

The app is being built around three product rules:

- show files quickly and clearly;
- keep authentication, downloads, and sharing predictable;
- avoid adding sync-heavy features until the core browser feels excellent.

## Current Testing Scope

The Android testing build is meant for real dogfooding, not a blank store shell.

- Instance sign-in through the Cotton app-code OAuth flow.
- Secure token storage through the platform key store.
- File browser with list and tile views.
- Folder navigation, refresh, search, and sorting.
- Native Android open, share, and download actions.
- GitHub APK download for direct installs.
- Google Play internal or closed testing when tester accounts are opted in.

## Install

| Channel | Use it for | Link |
| --- | --- | --- |
| Google Play testing | Opted-in internal/closed testers | [Open testing page](https://play.google.com/apps/testing/dev.cottoncloud.app) |
| GitHub APK | Direct Android install when Play testing is inconvenient | [Download latest APK](https://github.com/bvdcode/cotton-mobile/releases/latest/download/CottonCloud-Android.apk) |

The GitHub APK is a stable release asset, so the link does not expire like a workflow artifact.

## Tester Smoke Script

Use this short flow for closed-test invites, Play dogfooding, and release-candidate checks.

1. Install Cotton Cloud from Google Play testing or the GitHub APK.
2. Sign in to the assigned Cotton Cloud instance and approve the browser request.
3. Confirm the root file browser loads with the expected account initials.
4. Switch between tile and list views.
5. Sort by name, type, and size.
6. Search for a known file, clear search, then refresh the folder.
7. Open a folder, confirm its contents or empty state, then go back up.
8. Open a small text or image file.
9. Download or share one file, then confirm it is marked `On device` when available.
10. Send feedback from the account menu and describe anything confusing or broken; the draft includes app version, device, Android, screen, cache, and browser context automatically. If no email app is available, Cotton copies the same details so they can be pasted into email or chat.
11. If support asks for a snapshot, open `Diagnostics` from the account menu, tap `Copy`, and paste the copied text into the same feedback thread.

Optional offline check: after opening a file once, turn on airplane mode and confirm only files marked `On device` still open.

## Local Android Development

```bash
scripts/mobile/build-android-debug.sh
COTTON_ADB_SERIAL=emulator-5554 scripts/mobile/install-android-debug.sh
```

Use the emulator for fast visual checks, then batch real-device smoke tests on the Samsung A53 over ADB when the slice is worth testing on hardware.
Debug builds install side by side with the Play/GitHub release package.

## Repository Checks

```bash
scripts/ci/validate-yaml.rb
```

The YAML validator uses Ruby's standard parser and checks workflow/config files before pushing CI changes.

## Release Flow

Signed Android builds are produced by the `Mobile Android` workflow.

- Use `build_signed_release=true` to produce signed APK/AAB artifacts.
- Use `publish_github_release=true` to update the stable GitHub APK download.
- Use `upload_to_google_play=true` only for intentional Play testing uploads.
- `publish_github_release` and `upload_to_google_play` require `build_signed_release=true`; the workflow fails early if that combination is wrong.
- Signed release display versions come from GitVersion `MajorMinorPatch`; run Play uploads from an intentional SemVer tag such as `v1.0.0`.
- Android `versionCode` is computed from the GitHub Actions run number and attempt, is guarded against reuse, and is not user-facing.
- Signed release builds fail if the computed display version and Android version code were not applied.

The release flow keeps direct APK testing, Play dogfooding, and store submission separate on purpose.

## Product Direction

The next milestones are intentionally boring in the best way: make the file browser feel fast, make previews useful, make downloads and offline state obvious, then add sync and notifications only when the architecture can carry them cleanly.
