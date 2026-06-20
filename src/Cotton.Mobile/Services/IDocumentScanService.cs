namespace Cotton.Mobile.Services
{
    public interface IDocumentScanService
    {
        bool IsAvailable { get; }

        Task<CottonFileUploadSource?> ScanDocumentAsync(CancellationToken cancellationToken = default);
    }
}
