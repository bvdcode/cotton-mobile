// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID
using Android.Gms.Common;
using Android.Gms.Tasks;
using Firebase.Messaging;
using Microsoft.Extensions.Logging;
using AndroidTask = Android.Gms.Tasks.Task;
using SystemCancellationToken = System.Threading.CancellationToken;

namespace Cotton.Mobile.Services
{
    public class AndroidFirebaseMessagingTokenProvider : ICottonRemotePushPlatformTokenProvider
    {
        private readonly ILogger<AndroidFirebaseMessagingTokenProvider> _logger;

        public AndroidFirebaseMessagingTokenProvider(
            ILogger<AndroidFirebaseMessagingTokenProvider> logger)
        {
            ArgumentNullException.ThrowIfNull(logger);

            _logger = logger;
        }

        public async Task<CottonRemotePushPlatformTokenSnapshot> GetCurrentTokenAsync(
            SystemCancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            int playServicesStatus =
                GoogleApiAvailability.Instance.IsGooglePlayServicesAvailable(Platform.AppContext);
            if (playServicesStatus != ConnectionResult.Success)
            {
                return CottonRemotePushPlatformTokenSnapshot.Unavailable(
                    CottonRemotePushProviderKind.FirebaseCloudMessaging,
                    CottonRemotePushMobilePlatform.Android,
                    "Google Play services are unavailable.");
            }

            try
            {
                string token = await GetFirebaseTokenAsync(cancellationToken).ConfigureAwait(false);
                return string.IsNullOrWhiteSpace(token)
                    ? CottonRemotePushPlatformTokenSnapshot.Unavailable(
                        CottonRemotePushProviderKind.FirebaseCloudMessaging,
                        CottonRemotePushMobilePlatform.Android,
                        "Firebase returned an empty registration token.")
                    : CottonRemotePushPlatformTokenSnapshot.Available(
                        CottonRemotePushProviderKind.FirebaseCloudMessaging,
                        CottonRemotePushMobilePlatform.Android,
                        token);
            }
            catch (Java.Lang.IllegalStateException exception)
            {
                _logger.LogInformation(
                    exception,
                    "Firebase Cloud Messaging is not configured for Cotton mobile.");
                return CottonRemotePushPlatformTokenSnapshot.NotConfigured(
                    CottonRemotePushProviderKind.FirebaseCloudMessaging,
                    CottonRemotePushMobilePlatform.Android,
                    "Firebase Cloud Messaging is not configured.");
            }
            catch (Java.Lang.Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Firebase Cloud Messaging failed to return a Cotton mobile registration token.");
                return CottonRemotePushPlatformTokenSnapshot.Unavailable(
                    CottonRemotePushProviderKind.FirebaseCloudMessaging,
                    CottonRemotePushMobilePlatform.Android,
                    "Firebase Cloud Messaging token retrieval failed.");
            }
        }

        private static Task<string> GetFirebaseTokenAsync(SystemCancellationToken cancellationToken)
        {
            var completion = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            if (cancellationToken.CanBeCanceled)
            {
                CancellationTokenRegistration registration =
                    cancellationToken.Register(() => completion.TrySetCanceled(cancellationToken));
                _ = completion.Task.ContinueWith(
                    _ => registration.Dispose(),
                    SystemCancellationToken.None,
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
            }

            AndroidTask tokenTask = FirebaseMessaging.Instance.GetToken();
            var listener = new FirebaseTokenTaskListener(completion);
            tokenTask.AddOnSuccessListener(listener);
            tokenTask.AddOnFailureListener(listener);
            tokenTask.AddOnCanceledListener(listener);
            return completion.Task;
        }

        private class FirebaseTokenTaskListener :
            Java.Lang.Object,
            IOnSuccessListener,
            IOnFailureListener,
            IOnCanceledListener
        {
            private readonly TaskCompletionSource<string> _completion;

            public FirebaseTokenTaskListener(TaskCompletionSource<string> completion)
            {
                ArgumentNullException.ThrowIfNull(completion);

                _completion = completion;
            }

            public void OnSuccess(Java.Lang.Object? result)
            {
                _completion.TrySetResult(result?.ToString() ?? string.Empty);
            }

            public void OnFailure(Java.Lang.Exception exception)
            {
                _completion.TrySetException(exception);
            }

            public void OnCanceled()
            {
                _completion.TrySetCanceled();
            }
        }
    }
}
#endif
