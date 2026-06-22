// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID
using Android.Views;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;

namespace Cotton.Mobile.Services
{
    public class AndroidCottonWindowPrivacyService : ICottonWindowPrivacyService
    {
        private readonly ICottonAppLockSettingsStore _settingsStore;
        private readonly ICottonAppLockCapabilityService _capabilityService;
        private readonly CottonAppSwitcherPrivacyPolicy _privacyPolicy;
        private readonly ILogger<AndroidCottonWindowPrivacyService> _logger;

        public AndroidCottonWindowPrivacyService(
            ICottonAppLockSettingsStore settingsStore,
            ICottonAppLockCapabilityService capabilityService,
            CottonAppSwitcherPrivacyPolicy privacyPolicy,
            ILogger<AndroidCottonWindowPrivacyService> logger)
        {
            ArgumentNullException.ThrowIfNull(settingsStore);
            ArgumentNullException.ThrowIfNull(capabilityService);
            ArgumentNullException.ThrowIfNull(privacyPolicy);
            ArgumentNullException.ThrowIfNull(logger);

            _settingsStore = settingsStore;
            _capabilityService = capabilityService;
            _privacyPolicy = privacyPolicy;
            _logger = logger;
        }

        public async Task ApplyAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                CottonAppLockSettings settings =
                    await _settingsStore.GetAsync(cancellationToken).ConfigureAwait(false);
                CottonAppLockCapabilitySnapshot capability =
                    await _capabilityService.GetCapabilityAsync(cancellationToken).ConfigureAwait(false);
                bool shouldHidePreviews = _privacyPolicy.ShouldHidePreviews(settings, capability);
                await MainThread.InvokeOnMainThreadAsync(() => ApplyWindowFlag(shouldHidePreviews));
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to apply Cotton mobile Android window privacy.");
            }
        }

        private static void ApplyWindowFlag(bool shouldHidePreviews)
        {
            Android.Views.Window? window = Platform.CurrentActivity?.Window;
            if (window is null)
            {
                return;
            }

            if (shouldHidePreviews)
            {
                window.SetFlags(WindowManagerFlags.Secure, WindowManagerFlags.Secure);
                return;
            }

            window.ClearFlags(WindowManagerFlags.Secure);
        }
    }
}
#endif
