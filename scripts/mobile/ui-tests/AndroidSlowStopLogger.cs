// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID && DEBUG
using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.Platforms.Android
{
    internal class AndroidSlowStopLogger(int eventIdToDelay = 2205) : ILoggerProvider, ILogger
    {
        private int _pending = 1;

        public ILogger CreateLogger(string categoryName) => this;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel == LogLevel.Warning;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (eventId.Id != eventIdToDelay || Interlocked.Exchange(ref _pending, 0) == 0)
            {
                return;
            }

            _ = global::Android.Util.Log.Info(AndroidCancellationProbeRunner.LogTag,
                $"stop-log-blocked:main={MainThread.IsMainThread}");
            using ManualResetEventSlim blockedWriter = new(false);
            _ = blockedWriter.Wait(TimeSpan.FromSeconds(3));
            _ = global::Android.Util.Log.Info(AndroidCancellationProbeRunner.LogTag, "stop-log-released");
        }

        public void Dispose()
        {
            Interlocked.Exchange(ref _pending, 0);
        }
    }
}
#endif
