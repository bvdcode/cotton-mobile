#if ANDROID
using Android;
using Android.App;
using Android.Content.PM;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;

namespace Cotton.Mobile.Services
{
    public sealed class AndroidCameraBackupMediaAccessPolicy : ICottonCameraBackupMediaAccessPolicy
    {
        private const string RequestedPreferenceKey = "cotton.cameraBackup.mediaAccess.requested";
        private const string ReadMediaVisualUserSelectedPermission =
            "android.permission.READ_MEDIA_VISUAL_USER_SELECTED";

        private readonly IPreferences _preferences;

        public AndroidCameraBackupMediaAccessPolicy(IPreferences preferences)
        {
            ArgumentNullException.ThrowIfNull(preferences);

            _preferences = preferences;
        }

        public Task<CottonCameraBackupMediaAccessState> GetAccessStateAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(GetCurrentAccessState());
        }

        public async Task<CottonCameraBackupMediaAccessState> RequestAccessAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _preferences.Set(RequestedPreferenceKey, true);

            try
            {
                _ = await Permissions.RequestAsync<CameraBackupMediaPermission>()
                    .ConfigureAwait(false);
            }
            catch (PermissionException)
            {
                return CottonCameraBackupMediaAccessState.Unavailable;
            }

            cancellationToken.ThrowIfCancellationRequested();
            return GetCurrentAccessState();
        }

        public Task OpenSettingsAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            AppInfo.Current.ShowSettingsUI();
            return Task.CompletedTask;
        }

        public Task<bool> CanReadMediaAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CottonCameraBackupMediaAccessState state = GetCurrentAccessState();
            return Task.FromResult(
                state is CottonCameraBackupMediaAccessState.Allowed
                    or CottonCameraBackupMediaAccessState.Limited);
        }

        private CottonCameraBackupMediaAccessState GetCurrentAccessState()
        {
            if (!OperatingSystem.IsAndroidVersionAtLeast(23))
            {
                return CottonCameraBackupMediaAccessState.Allowed;
            }

            if (OperatingSystem.IsAndroidVersionAtLeast(33))
            {
                bool imagesAllowed = HasPermission(Manifest.Permission.ReadMediaImages);
                bool videosAllowed = HasPermission(Manifest.Permission.ReadMediaVideo);
                bool selectedAllowed = OperatingSystem.IsAndroidVersionAtLeast(34)
                    && HasPermission(ReadMediaVisualUserSelectedPermission);

                if (imagesAllowed && videosAllowed)
                {
                    return CottonCameraBackupMediaAccessState.Allowed;
                }

                if (imagesAllowed || videosAllowed || selectedAllowed)
                {
                    return CottonCameraBackupMediaAccessState.Limited;
                }

                return WasRequested()
                    ? CottonCameraBackupMediaAccessState.Denied
                    : CottonCameraBackupMediaAccessState.NotRequested;
            }

            if (HasPermission(Manifest.Permission.ReadExternalStorage))
            {
                return CottonCameraBackupMediaAccessState.Allowed;
            }

            return WasRequested()
                ? CottonCameraBackupMediaAccessState.Denied
                : CottonCameraBackupMediaAccessState.NotRequested;
        }

        private bool WasRequested()
        {
            return _preferences.Get(RequestedPreferenceKey, false);
        }

        private static bool HasPermission(string permission)
        {
            if (!OperatingSystem.IsAndroidVersionAtLeast(23))
            {
                return true;
            }

            return Android.App.Application.Context.CheckSelfPermission(permission) == Permission.Granted;
        }

        private sealed class CameraBackupMediaPermission : Permissions.BasePlatformPermission
        {
            public override (string androidPermission, bool isRuntime)[] RequiredPermissions
            {
                get
                {
                    if (OperatingSystem.IsAndroidVersionAtLeast(34))
                    {
                        return
                        [
                            (Manifest.Permission.ReadMediaImages, true),
                            (Manifest.Permission.ReadMediaVideo, true),
                            (ReadMediaVisualUserSelectedPermission, true),
                        ];
                    }

                    if (OperatingSystem.IsAndroidVersionAtLeast(33))
                    {
                        return
                        [
                            (Manifest.Permission.ReadMediaImages, true),
                            (Manifest.Permission.ReadMediaVideo, true),
                        ];
                    }

                    return
                    [
                        (Manifest.Permission.ReadExternalStorage, true),
                    ];
                }
            }
        }
    }
}
#endif
