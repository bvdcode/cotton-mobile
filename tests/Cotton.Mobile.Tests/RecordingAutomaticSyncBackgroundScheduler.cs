using Cotton.Mobile.Services;

namespace Cotton.Mobile.Tests
{
    internal class RecordingAutomaticSyncBackgroundScheduler : ICottonAutomaticSyncBackgroundScheduler
    {
        public int ScheduleCount { get; private set; }

        public int RescheduleMediaStoreCount { get; private set; }

        public int CancelCount { get; private set; }

        public List<Guid> RootRetryIds { get; } = [];

        public Task ScheduleAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ScheduleCount++;
            return Task.CompletedTask;
        }

        public Task RescheduleMediaStoreTriggerAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RescheduleMediaStoreCount++;
            return Task.CompletedTask;
        }

        public Task ScheduleRootRetriesAsync(
            IReadOnlyCollection<Guid> rootIds,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RootRetryIds.AddRange(rootIds);
            return Task.CompletedTask;
        }

        public Task CancelAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CancelCount++;
            return Task.CompletedTask;
        }
    }
}
