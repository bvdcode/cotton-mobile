using Microsoft.Maui.Media;

namespace Cotton.Mobile.Services
{
    public class VideoUploadPickerService : IVideoUploadPickerService
    {
        private readonly IMediaPicker _mediaPicker;

        public VideoUploadPickerService(IMediaPicker mediaPicker)
        {
            ArgumentNullException.ThrowIfNull(mediaPicker);

            _mediaPicker = mediaPicker;
        }

        public async Task<CottonFileUploadSource?> PickVideoAsync(CancellationToken cancellationToken = default)
        {
            return (await PickVideosAsync(cancellationToken).ConfigureAwait(false)).FirstOrDefault();
        }

        public async Task<IReadOnlyList<CottonFileUploadSource>> PickVideosAsync(
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<FileResult> results = await _mediaPicker.PickVideosAsync(
                new MediaPickerOptions
                {
                    SelectionLimit = CottonSelectedMediaUploadLimits.VideoSelectionLimit,
                });
            cancellationToken.ThrowIfCancellationRequested();

            return results
                .Take(CottonSelectedMediaUploadLimits.VideoSelectionLimit)
                .Select(CreateSource)
                .ToList();
        }

        private static CottonFileUploadSource CreateSource(FileResult result)
        {
            var snapshot = new CottonFileUploadSourceSnapshot(
                result.FileName,
                result.ContentType,
                TryGetFileSize(result.FullPath),
                CreateUploadMetadata(result.FullPath));
            return new CottonFileUploadSource(
                snapshot,
                async token =>
                {
                    Stream stream = await result.OpenReadAsync();
                    token.ThrowIfCancellationRequested();
                    return stream;
                });
        }

        private static long? TryGetFileSize(string? path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return null;
            }

            return new FileInfo(path).Length;
        }

        private static IReadOnlyDictionary<string, string> CreateUploadMetadata(string? path)
        {
            var metadata = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [CottonFileUploadMetadataKeys.Source] = "picked-video",
                [CottonFileUploadMetadataKeys.QualityPolicy] = "original",
                [CottonFileUploadMetadataKeys.TransferPolicy] = CottonSelectedMediaTransferPolicy.CurrentMetadataValue,
            };

            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
            {
                metadata[CottonFileUploadMetadataKeys.OriginalLastModifiedUtc] =
                    File.GetLastWriteTimeUtc(path).ToString("O", System.Globalization.CultureInfo.InvariantCulture);
            }

            return metadata;
        }
    }
}
