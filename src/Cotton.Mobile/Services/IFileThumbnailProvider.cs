namespace Cotton.Mobile.Services
{
    public interface IFileThumbnailProvider
    {
        ValueTask<CottonFileThumbnailSnapshot> GetThumbnailAsync(
            CottonFileBrowserEntry entry,
            CancellationToken cancellationToken = default);
    }
}
