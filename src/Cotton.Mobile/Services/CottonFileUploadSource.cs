namespace Cotton.Mobile.Services
{
    public class CottonFileUploadSource
    {
        private readonly Func<CancellationToken, Task<Stream>> _openReadAsync;

        public CottonFileUploadSource(
            CottonFileUploadSourceSnapshot snapshot,
            Func<CancellationToken, Task<Stream>> openReadAsync)
        {
            ArgumentNullException.ThrowIfNull(snapshot);
            ArgumentNullException.ThrowIfNull(openReadAsync);

            Snapshot = snapshot;
            _openReadAsync = openReadAsync;
        }

        public CottonFileUploadSourceSnapshot Snapshot { get; }

        public CottonFileUploadSource WithSnapshot(CottonFileUploadSourceSnapshot snapshot)
        {
            return new CottonFileUploadSource(snapshot, _openReadAsync);
        }

        public Task<Stream> OpenReadAsync(CancellationToken cancellationToken)
        {
            return _openReadAsync(cancellationToken);
        }
    }
}
