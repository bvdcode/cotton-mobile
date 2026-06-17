namespace Cotton.Mobile.Services
{
    public class CottonFileThumbnailSnapshot
    {
        private CottonFileThumbnailSnapshot(
            CottonFileThumbnailState state,
            string placeholderText,
            string? localPath)
        {
            State = state;
            PlaceholderText = string.IsNullOrWhiteSpace(placeholderText)
                ? "FILE"
                : placeholderText.Trim();
            LocalPath = string.IsNullOrWhiteSpace(localPath) ? null : localPath.Trim();
        }

        public CottonFileThumbnailState State { get; }

        public string PlaceholderText { get; }

        public string? LocalPath { get; }

        public bool IsLoading => State == CottonFileThumbnailState.Loading;

        public bool HasImage => State == CottonFileThumbnailState.Ready
            && !string.IsNullOrWhiteSpace(LocalPath);

        public bool IsPlaceholderVisible => !HasImage && !IsLoading;

        public static CottonFileThumbnailSnapshot Placeholder(string placeholderText)
        {
            return new CottonFileThumbnailSnapshot(
                CottonFileThumbnailState.Placeholder,
                placeholderText,
                null);
        }

        public static CottonFileThumbnailSnapshot Loading(string placeholderText)
        {
            return new CottonFileThumbnailSnapshot(
                CottonFileThumbnailState.Loading,
                placeholderText,
                null);
        }

        public static CottonFileThumbnailSnapshot Ready(string placeholderText, string localPath)
        {
            if (string.IsNullOrWhiteSpace(localPath))
            {
                throw new ArgumentException("Thumbnail path is required.", nameof(localPath));
            }

            return new CottonFileThumbnailSnapshot(
                CottonFileThumbnailState.Ready,
                placeholderText,
                localPath);
        }

        public static CottonFileThumbnailSnapshot Failed(string placeholderText)
        {
            return new CottonFileThumbnailSnapshot(
                CottonFileThumbnailState.Failed,
                placeholderText,
                null);
        }
    }
}
