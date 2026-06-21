using Microsoft.Maui.Media;

namespace Cotton.Mobile.Services
{
    public class PhotoUploadPickerService : IPhotoUploadPickerService
    {
        private readonly IMediaPicker _mediaPicker;

        public PhotoUploadPickerService(IMediaPicker mediaPicker)
        {
            ArgumentNullException.ThrowIfNull(mediaPicker);

            _mediaPicker = mediaPicker;
        }

        public async Task<CottonFileUploadSource?> PickPhotoAsync(CancellationToken cancellationToken = default)
        {
            return (await PickPhotosAsync(cancellationToken).ConfigureAwait(false)).FirstOrDefault();
        }

        public async Task<IReadOnlyList<CottonFileUploadSource>> PickPhotosAsync(
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<FileResult> results = await _mediaPicker.PickPhotosAsync(
                new MediaPickerOptions
                {
                    CompressionQuality = 100,
                    PreserveMetaData = true,
                    RotateImage = false,
                    SelectionLimit = CottonSelectedMediaUploadLimits.PhotoSelectionLimit,
                });
            cancellationToken.ThrowIfCancellationRequested();

            return results
                .Take(CottonSelectedMediaUploadLimits.PhotoSelectionLimit)
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
                [CottonFileUploadMetadataKeys.Source] = "picked-photo",
                [CottonFileUploadMetadataKeys.QualityPolicy] = "original-preferred",
                [CottonFileUploadMetadataKeys.CompressionQuality] = "100",
                [CottonFileUploadMetadataKeys.PreserveMetadata] = "true",
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
