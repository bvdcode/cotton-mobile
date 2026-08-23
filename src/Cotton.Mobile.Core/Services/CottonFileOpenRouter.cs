// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Resources.Localization;

namespace Cotton.Mobile.Services
{
    public static class CottonFileOpenRouter
    {
        public const long MaxTextPreviewBytes = 512 * 1024;
        public static string OpenActionLabel => CoreResources.OpenAction;
        public static string OpenWithSystemAppActionLabel => CoreResources.OpenWithSystemAppAction;
        public static string OpenUnavailableStatus => CoreResources.OpenUnavailable;
        public static string PdfOpenUnavailableStatus => CoreResources.PdfOpenUnavailable;
        public static string DocumentOpenUnavailableStatus => CoreResources.DocumentOpenUnavailable;
        public static string AudioOpenUnavailableStatus => CoreResources.AudioOpenUnavailable;
        public static string VideoOpenUnavailableStatus => CoreResources.VideoOpenUnavailable;
        public static string ArchiveOpenUnavailableStatus => CoreResources.ArchiveOpenUnavailable;
        public static string SvgOpenUnavailableStatus => CoreResources.SvgOpenUnavailable;
        public static string UnknownOpenUnavailableStatus => CoreResources.UnknownOpenUnavailable;

        private static readonly HashSet<string> ArchiveFileExtensions =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ".7z",
                ".gz",
                ".rar",
                ".tar",
                ".zip",
            };

        public static CottonFileOpenRoute CreateRoute(
            CottonFileBrowserEntry file,
            long? availableSizeBytes = null)
        {
            ArgumentNullException.ThrowIfNull(file);
            if (file.Type != CottonFileBrowserEntryType.File)
            {
                throw new ArgumentException("Only file entries can be opened with a file route.", nameof(file));
            }

            string? contentType = CottonFileContentTypeResolver.Resolve(file.Name, file.ContentType);
            if (file.IsImage)
            {
                return new CottonFileOpenRoute(
                    CottonFileOpenTarget.InAppPreview,
                    CottonFilePreviewKind.Image,
                    CottonSystemFileOpenKind.None,
                    OpenActionLabel,
                    OpenUnavailableStatus,
                    contentType);
            }

            if (CanPreviewAsText(file, availableSizeBytes))
            {
                return new CottonFileOpenRoute(
                    CottonFileOpenTarget.InAppPreview,
                    CottonFilePreviewKind.Text,
                    CottonSystemFileOpenKind.None,
                    OpenActionLabel,
                    OpenUnavailableStatus,
                    contentType);
            }

            if (IsPdfPreview(file, contentType))
            {
                return new CottonFileOpenRoute(
                    CottonFileOpenTarget.InAppPreview,
                    CottonFilePreviewKind.Pdf,
                    CottonSystemFileOpenKind.None,
                    OpenActionLabel,
                    PdfOpenUnavailableStatus,
                    contentType);
            }

            if (IsAudioPreview(file, contentType))
            {
                return new CottonFileOpenRoute(
                    CottonFileOpenTarget.InAppPreview,
                    CottonFilePreviewKind.Audio,
                    CottonSystemFileOpenKind.None,
                    OpenActionLabel,
                    AudioOpenUnavailableStatus,
                    contentType);
            }

            if (IsVideoPreview(file, contentType))
            {
                return new CottonFileOpenRoute(
                    CottonFileOpenTarget.InAppPreview,
                    CottonFilePreviewKind.Video,
                    CottonSystemFileOpenKind.None,
                    OpenActionLabel,
                    VideoOpenUnavailableStatus,
                    contentType);
            }

            CottonSystemFileOpenKind systemKind = ResolveSystemKind(file, contentType);
            return new CottonFileOpenRoute(
                CottonFileOpenTarget.SystemApp,
                CottonFilePreviewKind.None,
                systemKind,
                OpenWithSystemAppActionLabel,
                CreateUnavailableStatus(systemKind),
                contentType);
        }

        public static string ResolveRequiredContentType(string? fileName, string? contentType)
        {
            return CottonFileContentTypeResolver.ResolveRequired(fileName, contentType);
        }

        public static string? ResolvePreferredContentType(string? fileName, string? contentType)
        {
            return CottonFileContentTypeResolver.Resolve(fileName, contentType);
        }

        private static bool CanPreviewText(CottonFileBrowserEntry file, long? availableSizeBytes)
        {
            long? sizeBytes = availableSizeBytes ?? file.SizeBytes;
            return !sizeBytes.HasValue || sizeBytes.Value is >= 0 and <= MaxTextPreviewBytes;
        }

        private static bool CanPreviewAsText(CottonFileBrowserEntry file, long? availableSizeBytes)
        {
            return (file.IsText || file.IsSvg) && CanPreviewText(file, availableSizeBytes);
        }

        private static bool IsPdfPreview(CottonFileBrowserEntry file, string? contentType)
        {
            return file.Kind == CottonFileKind.Pdf
                || string.Equals(contentType, "application/pdf", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsAudioPreview(CottonFileBrowserEntry file, string? contentType)
        {
            return file.Kind == CottonFileKind.Audio
                || (contentType?.StartsWith("audio/", StringComparison.OrdinalIgnoreCase) ?? false);
        }

        private static bool IsVideoPreview(CottonFileBrowserEntry file, string? contentType)
        {
            return file.Kind == CottonFileKind.Video
                || (contentType?.StartsWith("video/", StringComparison.OrdinalIgnoreCase) ?? false);
        }

        private static CottonSystemFileOpenKind ResolveSystemKind(
            CottonFileBrowserEntry file,
            string? contentType)
        {
            string extension = Path.GetExtension(file.Name);
            string mediaType = contentType ?? string.Empty;
            return file.Kind switch
            {
                CottonFileKind.Text => CottonSystemFileOpenKind.Text,
                CottonFileKind.Pdf => CottonSystemFileOpenKind.Pdf,
                CottonFileKind.Document => CottonSystemFileOpenKind.Document,
                CottonFileKind.Audio => CottonSystemFileOpenKind.Audio,
                CottonFileKind.Video => CottonSystemFileOpenKind.Video,
                CottonFileKind.Svg => CottonSystemFileOpenKind.Svg,
                CottonFileKind.Image => CottonSystemFileOpenKind.Image,
                CottonFileKind.File when ArchiveFileExtensions.Contains(extension) => CottonSystemFileOpenKind.Archive,
                CottonFileKind.File when string.Equals(mediaType, "application/pdf", StringComparison.OrdinalIgnoreCase) =>
                    CottonSystemFileOpenKind.Pdf,
                CottonFileKind.File when mediaType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase) =>
                    CottonSystemFileOpenKind.Audio,
                CottonFileKind.File when mediaType.StartsWith("video/", StringComparison.OrdinalIgnoreCase) =>
                    CottonSystemFileOpenKind.Video,
                CottonFileKind.File when mediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) =>
                    CottonSystemFileOpenKind.Image,
                CottonFileKind.File => CottonSystemFileOpenKind.File,
                CottonFileKind.Folder => throw new ArgumentException(
                    "Folder entries cannot be opened with a file route.",
                    nameof(file)),
                _ => throw new ArgumentOutOfRangeException(nameof(file), file.Kind, "File kind is not supported."),
            };
        }

        private static string CreateUnavailableStatus(CottonSystemFileOpenKind kind)
        {
            return kind switch
            {
                CottonSystemFileOpenKind.Pdf => PdfOpenUnavailableStatus,
                CottonSystemFileOpenKind.Document => DocumentOpenUnavailableStatus,
                CottonSystemFileOpenKind.Audio => AudioOpenUnavailableStatus,
                CottonSystemFileOpenKind.Video => VideoOpenUnavailableStatus,
                CottonSystemFileOpenKind.Archive => ArchiveOpenUnavailableStatus,
                CottonSystemFileOpenKind.Svg => SvgOpenUnavailableStatus,
                CottonSystemFileOpenKind.None => OpenUnavailableStatus,
                CottonSystemFileOpenKind.Text => OpenUnavailableStatus,
                CottonSystemFileOpenKind.Image => OpenUnavailableStatus,
                CottonSystemFileOpenKind.File => UnknownOpenUnavailableStatus,
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "System file open kind is not supported."),
            };
        }
    }
}
