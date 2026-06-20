namespace Cotton.Mobile.Services
{
    public interface ICottonRemotePushDiagnosticsService
    {
        Task<CottonRemotePushDiagnosticsSnapshot> GetSnapshotAsync(
            CancellationToken cancellationToken = default);
    }
}
