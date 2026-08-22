// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID
using Cotton.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.Platforms.Android
{
    public class AndroidAutomaticSyncExecutor(
        ICottonSessionService sessionService,
        CottonAutomaticSyncDispatcher dispatcher,
        ICottonAutomaticSyncBackgroundScheduler backgroundScheduler,
        ILogger<AndroidAutomaticSyncExecutor> logger)
    {
        private readonly ICottonSessionService _sessionService =
            sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        private readonly CottonAutomaticSyncDispatcher _dispatcher =
            dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        private readonly ICottonAutomaticSyncBackgroundScheduler _backgroundScheduler =
            backgroundScheduler ?? throw new ArgumentNullException(nameof(backgroundScheduler));
        private readonly ILogger<AndroidAutomaticSyncExecutor> _logger =
            logger ?? throw new ArgumentNullException(nameof(logger));

        public async Task<AndroidAutomaticSyncExecutionResult> ExecuteAsync(
            CottonAutomaticSyncTrigger trigger,
            Guid? retryRootId,
            CancellationToken cancellationToken = default)
        {
            if (!Enum.IsDefined(trigger))
            {
                throw new ArgumentOutOfRangeException(nameof(trigger), "Automatic sync trigger is not supported.");
            }

            if (retryRootId == Guid.Empty)
            {
                throw new ArgumentException("Retry sync root id cannot be empty.", nameof(retryRootId));
            }

            AndroidAutomaticSyncDiagnosticLog.Started(_logger, trigger, retryRootId.HasValue);

            Uri? instanceUri = await _sessionService
                .GetRememberedSessionInstanceAsync(cancellationToken)
                .ConfigureAwait(false);
            if (instanceUri is null)
            {
                AndroidAutomaticSyncDiagnosticLog.SessionMissing(_logger);
                return AndroidAutomaticSyncExecutionResult.NoSession;
            }

            CottonAutomaticSyncRunResult result = retryRootId.HasValue
                ? await _dispatcher
                    .RunRootsAsync(instanceUri, [retryRootId.Value], cancellationToken)
                    .ConfigureAwait(false)
                : await _dispatcher
                    .RunAsync(instanceUri, trigger, cancellationToken)
                    .ConfigureAwait(false);
            AndroidAutomaticSyncDiagnosticLog.DispatchCompleted(_logger, result.FailedRootIds.Count);
            if (!result.HasFailures)
            {
                return AndroidAutomaticSyncExecutionResult.Completed;
            }

            if (retryRootId.HasValue)
            {
                return AndroidAutomaticSyncExecutionResult.RetryRequired;
            }

            await _backgroundScheduler
                .ScheduleRootRetriesAsync(result.FailedRootIds, cancellationToken)
                .ConfigureAwait(false);
            AndroidAutomaticSyncDiagnosticLog.RetriesScheduled(_logger, result.FailedRootIds.Count);
            return AndroidAutomaticSyncExecutionResult.Completed;
        }
    }
}
#endif
