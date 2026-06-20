namespace Cotton.Mobile.Services
{
    public interface ICottonSyncLocalRootPickerService
    {
        bool IsAvailable { get; }

        Task<CottonSyncLocalRootSnapshot?> PickUserSelectedDocumentTreeAsync(
            CancellationToken cancellationToken = default);
    }
}
