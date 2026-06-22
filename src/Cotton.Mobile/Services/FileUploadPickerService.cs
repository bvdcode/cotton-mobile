// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Microsoft.Maui.Storage;

namespace Cotton.Mobile.Services
{
    public class FileUploadPickerService : IFileUploadPickerService
    {
        private const string PickerTitle = "Upload file";

        private readonly IFilePicker _filePicker;

        public FileUploadPickerService(IFilePicker filePicker)
        {
            ArgumentNullException.ThrowIfNull(filePicker);

            _filePicker = filePicker;
        }

        public async Task<CottonFileUploadSource?> PickFileAsync(CancellationToken cancellationToken = default)
        {
            FileResult? result = await _filePicker.PickAsync(
                new PickOptions
                {
                    PickerTitle = PickerTitle,
                });
            cancellationToken.ThrowIfCancellationRequested();
            if (result is null)
            {
                return null;
            }

            var snapshot = new CottonFileUploadSourceSnapshot(
                result.FileName,
                result.ContentType,
                TryGetFileSize(result.FullPath),
                CreateUploadMetadata(result.FullPath, "picked-file"));
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

        private static IReadOnlyDictionary<string, string> CreateUploadMetadata(string? path, string source)
        {
            var metadata = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [CottonFileUploadMetadataKeys.Source] = source,
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
