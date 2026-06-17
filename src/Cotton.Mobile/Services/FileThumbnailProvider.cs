namespace Cotton.Mobile.Services
{
    public class FileThumbnailProvider : IFileThumbnailProvider
    {
        public ValueTask<CottonFileThumbnailSnapshot> GetThumbnailAsync(
            Uri instanceUri,
            CottonFileBrowserEntry entry,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(entry);

            cancellationToken.ThrowIfCancellationRequested();
            CottonFileThumbnailSnapshot thumbnail = CreateThumbnail(instanceUri, entry);
            return ValueTask.FromResult(thumbnail);
        }

        private static CottonFileThumbnailSnapshot CreateThumbnail(
            Uri instanceUri,
            CottonFileBrowserEntry entry)
        {
            if (string.IsNullOrWhiteSpace(entry.PreviewHashEncryptedHex))
            {
                return CottonFileThumbnailSnapshot.Placeholder(entry.BadgeText);
            }

            Uri previewUri = new(
                instanceUri,
                $"{Routes.V1.Previews}/{Uri.EscapeDataString(entry.PreviewHashEncryptedHex)}.webp");
            return CottonFileThumbnailSnapshot.Ready(entry.BadgeText, previewUri.AbsoluteUri);
        }
    }
}
