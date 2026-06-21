namespace Cotton.Mobile.Services
{
    public interface ICottonSelectedMediaTransferEnqueueCoordinator
    {
        Task<CottonSelectedMediaTransferEnqueueResult> EnqueueAsync(
            Uri instanceUri,
            CottonFolderHandle destinationFolder,
            string? destinationPath,
            IReadOnlyList<CottonFileUploadSource> sources,
            CancellationToken cancellationToken = default);
    }
}
