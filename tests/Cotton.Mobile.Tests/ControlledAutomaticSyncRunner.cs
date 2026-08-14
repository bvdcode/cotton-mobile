using Cotton.Mobile.Services;

namespace Cotton.Mobile.Tests
{
    internal class ControlledAutomaticSyncRunner : ICottonAutomaticSyncRunner, IDisposable
    {
        private readonly Lock _gate = new();
        private readonly SemaphoreSlim _started = new(0);
        private readonly SemaphoreSlim _release = new(0);

        public List<CottonAutomaticSyncTrigger> Triggers { get; } = [];

        public List<IReadOnlyList<Guid>> RootSelections { get; } = [];

        public Task<CottonAutomaticSyncRunResult> RunAsync(
            Uri instanceUri,
            CottonAutomaticSyncTrigger trigger,
            CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                Triggers.Add(trigger);
            }

            return RunControlledAsync(cancellationToken);
        }

        public Task<CottonAutomaticSyncRunResult> RunRootsAsync(
            Uri instanceUri,
            IReadOnlyCollection<Guid> rootIds,
            CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                RootSelections.Add([.. rootIds.Order()]);
            }

            return RunControlledAsync(cancellationToken);
        }

        public async Task WaitForNextRunAsync()
        {
            bool started = await _started.WaitAsync(TimeSpan.FromSeconds(5));
            if (!started)
            {
                throw new TimeoutException("Automatic sync run did not start.");
            }
        }

        public void ReleaseRun()
        {
            _release.Release();
        }

        public void Dispose()
        {
            _started.Dispose();
            _release.Dispose();
            GC.SuppressFinalize(this);
        }

        private async Task<CottonAutomaticSyncRunResult> RunControlledAsync(
            CancellationToken cancellationToken)
        {
            _started.Release();
            await _release.WaitAsync(cancellationToken);
            return CottonAutomaticSyncRunResult.Empty;
        }
    }
}
