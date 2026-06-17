namespace Cotton.Mobile.Services
{
    public class FileThumbnailProvider : IFileThumbnailProvider
    {
        public ValueTask<CottonFileThumbnailSnapshot> GetThumbnailAsync(
            CottonFileBrowserEntry entry,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(entry);

            cancellationToken.ThrowIfCancellationRequested();
            CottonFileThumbnailSnapshot thumbnail = CottonFileThumbnailSnapshot.Placeholder(entry.BadgeText);
            return ValueTask.FromResult(thumbnail);
        }
    }
}
