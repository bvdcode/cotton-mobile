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
            IReadOnlyList<FileResult> results = await _mediaPicker.PickVideosAsync(
                new MediaPickerOptions
                {
                    SelectionLimit = 1,
                });
            cancellationToken.ThrowIfCancellationRequested();
            FileResult? result = results.FirstOrDefault();
            if (result is null)
            {
                return null;
            }

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
