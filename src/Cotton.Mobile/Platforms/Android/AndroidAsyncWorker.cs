// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID
using Android.Content;
using Android.Util;
using AndroidX.Concurrent.Futures;
using AndroidX.Core.Content;
using AndroidX.Work;
using Cotton.Mobile.Services;
using Google.Common.Util.Concurrent;
using Java.Util.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.Platforms.Android
{
    public abstract class AndroidAsyncWorker(
        Context context,
        WorkerParameters workerParameters) :
        ListenableWorker(context, workerParameters),
        CallbackToFutureAdapter.IResolver
    {
        private const string LogTag = "CottonWorker";
        private const string FailureMessage = "Android background worker failed.";

        private readonly CancellationTokenSource _stoppingSource = new();

        public override IListenableFuture StartWork()
        {
            return CallbackToFutureAdapter.GetFuture(this)
                ?? throw new InvalidOperationException("Android worker future is unavailable.");
        }

        public Java.Lang.Object AttachCompleter(CallbackToFutureAdapter.Completer? p0)
        {
            CallbackToFutureAdapter.Completer completer = p0
                ?? throw new ArgumentNullException(nameof(p0));

            IExecutor executor = ContextCompat.GetMainExecutor(global::Android.App.Application.Context)
                ?? throw new InvalidOperationException("Android main executor is unavailable.");
            completer.AddCancellationListener(
                new Java.Lang.Runnable(_stoppingSource.Cancel),
                executor);
            _ = CompleteAsync(completer);
            return new Java.Lang.String(GetType().Name);
        }

        public override void OnStopped()
        {
            _stoppingSource.Cancel();
            base.OnStopped();
        }

        protected abstract Task<ListenableWorker.Result> ExecuteAsync(
            CancellationToken cancellationToken);

        protected static ListenableWorker.Result Success()
        {
            return ListenableWorker.Result.InvokeSuccess()
                ?? throw new InvalidOperationException("Android WorkManager success result is unavailable.");
        }

        protected static ListenableWorker.Result Retry()
        {
            return ListenableWorker.Result.InvokeRetry()
                ?? throw new InvalidOperationException("Android WorkManager retry result is unavailable.");
        }

        private async Task CompleteAsync(CallbackToFutureAdapter.Completer completer)
        {
            ListenableWorker.Result result;
            try
            {
                result = await ExecuteAsync(_stoppingSource.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (_stoppingSource.IsCancellationRequested)
            {
                result = Retry();
            }
            catch (Exception exception)
            {
                LogFailure(exception);
                result = Retry();
            }

            _ = completer.Set(result);
        }

        private void LogFailure(Exception exception)
        {
            IServiceProvider? services = IPlatformApplication.Current?.Services;
            ILoggerFactory? loggerFactory = services?.GetService<ILoggerFactory>();
            if (loggerFactory is not null)
            {
                ILogger logger = loggerFactory.CreateLogger(GetType().FullName ?? GetType().Name);
                CottonLog.Warning(logger, FailureMessage, exception);
                return;
            }

            _ = Log.Warn(LogTag, exception.ToString());
        }
    }
}
#endif
