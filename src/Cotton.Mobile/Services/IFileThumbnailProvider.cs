namespace Cotton.Mobile.Services
{
    public interface IFileThumbnailProvider
    {
        ValueTask<CottonFileThumbnailSnapshot> GetThumbnailAsync(
            Uri instanceUri,
            CottonFileBrowserEntry entry,
            CancellationToken cancellationToken = default);

        ValueTask<CottonFileThumbnailSnapshot> GetCachedThumbnailAsync(
            Uri instanceUri,
            CottonFileBrowserEntry entry,
            CancellationToken cancellationToken = default);
    }
}
