# Cotton Mobile

Native Cotton Cloud mobile client for Android, built with .NET MAUI.

<p>
  <a href="https://play.google.com/apps/testing/dev.cottoncloud.app"><img alt="Get Cotton Cloud on Google Play" src="https://img.shields.io/badge/Google%20Play-Get%20the%20app-414141?style=for-the-badge&logo=googleplay&logoColor=white"></a>
  <a href="https://github.com/bvdcode/cotton-mobile/releases/latest/download/CottonCloud-Android.apk"><img alt="Download Cotton Cloud APK from GitHub" src="https://img.shields.io/badge/GitHub-APK%20download-181717?style=for-the-badge&logo=github&logoColor=white"></a>
</p>

Cotton Mobile is the Android client for Cotton Cloud. It signs in to a Cotton Cloud instance, browses remote files, uploads and shares content, keeps selected items available offline, and exposes transfer, notification, storage, security, and diagnostics state for testing.

## Current Testing Scope

Current Android testing builds cover:

- Instance sign-in through the Cotton app-code OAuth flow.
- Secure token storage through the platform key store.
- File browser with list and tile views.
- Folder navigation, refresh, search, and sorting.
- Native Android open, share, download, upload, and document-scan actions.
- Share-to-Cotton capture inbox for Android share targets.
- Durable transfer queue with restart recovery evidence.
- Camera backup setup and media-scan queueing.
- Offline files and folders with explicit `On device` state.
- Notification permission, local transfer notifications, and server-push controls.
- Storage cleanup, diagnostics, security settings, activity, recent files, Trash, and Sync settings surfaces.
- GitHub APK download for direct installs.
- Google Play internal or closed testing when tester accounts are opted in.

## Install

| Channel | Use it for | Link |
| --- | --- | --- |
| Google Play testing | Opted-in internal/closed testers | [Open testing page](https://play.google.com/apps/testing/dev.cottoncloud.app) |
| GitHub APK | Direct Android install when Play testing is inconvenient | [Download latest APK](https://github.com/bvdcode/cotton-mobile/releases/latest/download/CottonCloud-Android.apk) |

The GitHub APK is a stable release asset, so the link does not expire like a workflow artifact.

## Tester Smoke Script

Use this flow for closed-test invites, Play dogfooding, and release-candidate checks.

1. Install Cotton Cloud from Google Play testing or the GitHub APK.
2. Sign in to the assigned Cotton Cloud instance and approve the browser request.
3. Confirm the root file browser loads with the expected account initials.
4. Switch between tile and list views.
5. Sort by name, type, and size.
6. Search for a known file, clear search, then refresh the folder.
7. Open a folder, confirm its contents or empty state, then go back up.
8. Open a small text or image file.
9. Download or share one file, then confirm it is marked `On device` when available.
10. Upload a small file through `Add files` -> `Upload...` -> `Upload file`.
11. Share a small text or file item from Android into Cotton, then choose a destination from Capture Inbox.
12. Queue one selected photo or video upload and confirm it appears in Transfers.
13. Enable Camera Backup for the assigned destination and run `Queue now` with a recent test photo.
14. Keep one file and one folder offline, turn on airplane mode, and confirm only `On device` content opens.
15. Open Notifications and confirm Android permission state plus server-push preferences or the unavailable/retry state.
16. Open Storage and confirm account storage, pending uploads, offline files, and cleanup actions are understandable.
17. Open Activity, Recent files, Trash, Security, Sync, and Diagnostics from the account menu and confirm each page loads without clipped controls.
18. Send feedback from the account menu and describe anything confusing or broken; the draft includes app version, device, Android, screen, cache, and browser context automatically. If no email app is available, Cotton copies the same details so they can be pasted into email or chat.
19. If support asks for a snapshot, open `Diagnostics` from the account menu, tap `Copy`, and paste the copied text into the same feedback thread.

Optional document-scan check: use `Add files` -> `Scan document`, complete the Android document scanner flow with a real document or gallery import, and confirm the scanned PDF appears in Files.

## Local Android Development

```bash
scripts/mobile/build-android-debug.sh
COTTON_ADB_SERIAL=emulator-5554 scripts/mobile/install-android-debug.sh
```

Use the emulator for fast visual checks, then batch real-device smoke tests over ADB when the slice is worth testing on hardware.
Debug builds install side by side with the Play/GitHub release package.

## Release Flow

The `Mobile Android` workflow runs on every push to `main` and `develop`.

- Every push runs Google Play listing validation, Android release-version checks, release-notes checks, mobile unit tests, a debug APK build, and a signed release build.
- Every push to `main` or `develop` creates the next SemVer tag from `GitVersion.yml` and existing `vX.Y.Z` tags, creates a GitHub Release, and uploads `CottonCloud-Android.apk`.
- `main` releases are marked latest and are used by the stable GitHub APK download link.
- `develop` releases are marked as prereleases and keep a downloadable APK for branch testing.
- Every push to `main` also uploads the signed AAB to Google Play internal testing after the GitHub Release is created.
- Manual workflow dispatch remains available for controlled signed rebuilds, Play uploads from an existing SemVer tag, and store listing updates.
- Android `ApplicationDisplayVersion` is SemVer. Android `versionCode` is computed from the GitHub Actions run number and attempt, is guarded against reuse, and is not user-facing.
- Google Play upload requires valid signing secrets, Firebase config, service-account access, and any required Play Console app-content declarations for requested Android permissions.

## Product Direction

The next milestones are upload reliability, camera backup, offline content, sync, notifications, storage controls, security controls, sharing, and activity history.
