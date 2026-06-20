namespace Cotton.Mobile.Services
{
    public class DisabledDocumentScanService : IDocumentScanService
    {
        public bool IsAvailable => false;

        public Task<CottonFileUploadSource?> ScanDocumentAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<CottonFileUploadSource?>(null);
        }
    }
}
