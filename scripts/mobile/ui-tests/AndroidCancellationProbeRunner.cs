// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID && DEBUG
using Cotton.Mobile.Services;

namespace Cotton.Mobile.Platforms.Android
{
    internal class AndroidCancellationProbeRunner : ICottonAutomaticSyncRunner
    {
        public const string LogTag = "CottonCancellationProbe";

        public Task<CottonAutomaticSyncRunResult> RunAsync(
            CottonAuthenticatedSessionScope sessionScope,
            CottonAutomaticSyncTrigger trigger,
            CancellationToken cancellationToken = default) => WaitAsync(cancellationToken);

        public Task<CottonAutomaticSyncRunResult> RunRootsAsync(
            CottonAuthenticatedSessionScope sessionScope,
            IReadOnlyCollection<Guid> rootIds,
            CancellationToken cancellationToken = default) => WaitAsync(cancellationToken);

        private static async Task<CottonAutomaticSyncRunResult> WaitAsync(CancellationToken cancellationToken)
        {
            using CancellationTokenRegistration registration = cancellationToken.Register(() =>
                global::Android.Util.Log.Info(LogTag, $"cancellation-callback:main={MainThread.IsMainThread}"));
            _ = global::Android.Util.Log.Info(LogTag, $"operation-thread:main={MainThread.IsMainThread}");
            _ = global::Android.Util.Log.Info(LogTag, "operation-started");
            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
                return CottonAutomaticSyncRunResult.Empty;
            }
            finally
            {
                _ = global::Android.Util.Log.Info(LogTag, $"operation-stopped:cancelled={cancellationToken.IsCancellationRequested}");
            }
        }
    }
}
#endif
