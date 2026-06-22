// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID
using Android.App;
using Android.Content;
using Android.OS;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;

namespace Cotton.Mobile.Services
{
    public class AndroidDeviceUnlockService : ICottonDeviceUnlockService
    {
        private const string SecureLockMissingDetailText =
            "Set a screen lock on this device before using App lock.";
        private const string KeyguardUnavailableDetailText =
            "Device unlock is not available on this Android device.";
        private const string PromptTitle = "Unlock Cotton";
        private const string PromptDescription = "Confirm your device unlock to continue.";

        private readonly IAndroidDeviceCredentialUnlockActivityResultBridge _activityResultBridge;
        private readonly ILogger<AndroidDeviceUnlockService> _logger;

        public AndroidDeviceUnlockService(
            IAndroidDeviceCredentialUnlockActivityResultBridge activityResultBridge,
            ILogger<AndroidDeviceUnlockService> logger)
        {
            ArgumentNullException.ThrowIfNull(activityResultBridge);
            ArgumentNullException.ThrowIfNull(logger);

            _activityResultBridge = activityResultBridge;
            _logger = logger;
        }

        public Task<CottonDeviceUnlockAvailabilitySnapshot> GetAvailabilityAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(GetAvailability());
        }

        public async Task<CottonDeviceUnlockResult> RequestUnlockAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            CottonDeviceUnlockAvailabilitySnapshot availability = GetAvailability();
            if (!availability.CanVerify)
            {
                return CottonDeviceUnlockResult.Unavailable(availability.DetailText);
            }

            Activity? activity = Platform.CurrentActivity;
            if (activity is null)
            {
                return CottonDeviceUnlockResult.Failed("Could not open the device unlock prompt.");
            }

            KeyguardManager? keyguardManager = GetKeyguardManager();
            if (keyguardManager is null)
            {
                return CottonDeviceUnlockResult.Unavailable(KeyguardUnavailableDetailText);
            }

#pragma warning disable CA1416, CA1422
            Intent? intent = keyguardManager.CreateConfirmDeviceCredentialIntent(
                PromptTitle,
                PromptDescription);
#pragma warning restore CA1416, CA1422
            if (intent is null)
            {
                return CottonDeviceUnlockResult.Unavailable(KeyguardUnavailableDetailText);
            }

            try
            {
                return await _activityResultBridge
                    .RequestUnlockAsync(activity, intent, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (System.OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to request Android device unlock.");
                return CottonDeviceUnlockResult.Failed("Could not verify device unlock.");
            }
        }

        private static CottonDeviceUnlockAvailabilitySnapshot GetAvailability()
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.M)
            {
                return CottonDeviceUnlockAvailabilitySnapshot.Unavailable(KeyguardUnavailableDetailText);
            }

            KeyguardManager? keyguardManager = GetKeyguardManager();
            if (keyguardManager is null)
            {
                return CottonDeviceUnlockAvailabilitySnapshot.Unavailable(KeyguardUnavailableDetailText);
            }

#pragma warning disable CA1416
            return keyguardManager.IsDeviceSecure
                ? CottonDeviceUnlockAvailabilitySnapshot.Available
                : CottonDeviceUnlockAvailabilitySnapshot.Unavailable(SecureLockMissingDetailText);
#pragma warning restore CA1416
        }

        private static KeyguardManager? GetKeyguardManager()
        {
            Context? context = Platform.CurrentActivity ?? Android.App.Application.Context;
            return context?.GetSystemService(Context.KeyguardService) as KeyguardManager;
        }
    }
}
#endif
