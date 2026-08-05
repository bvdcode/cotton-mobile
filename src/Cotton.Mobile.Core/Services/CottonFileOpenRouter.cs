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

        private static readonly Dictionary<string, string> ExtensionContentTypes =
            new(StringComparer.OrdinalIgnoreCase)
            {
                [".7z"] = "application/x-7z-compressed",
                [".bash"] = "text/plain",
                [".c"] = "text/plain",
                [".cc"] = "text/plain",
                [".conf"] = "text/plain",
                [".cpp"] = "text/plain",
                [".cs"] = "text/plain",
                [".csproj"] = "application/xml",
                [".csv"] = "text/csv",
                [".doc"] = "application/msword",
                [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                [".env"] = "text/plain",
                [".flac"] = "audio/flac",
                [".gif"] = "image/gif",
                [".go"] = "text/plain",
                [".gradle"] = "text/plain",
                [".gz"] = "application/gzip",
                [".h"] = "text/plain",
                [".hpp"] = "text/plain",
                [".heic"] = "image/heic",
                [".htm"] = "text/html",
                [".html"] = "text/html",
                [".ini"] = "text/plain",
                [".java"] = "text/plain",
                [".jpeg"] = "image/jpeg",
                [".jpg"] = "image/jpeg",
                [".js"] = "application/javascript",
                [".json"] = "application/json",
                [".kt"] = "text/plain",
                [".kts"] = "text/plain",
                [".m"] = "text/plain",
                [".m4a"] = "audio/mp4",
                [".markdown"] = "text/markdown",
                [".md"] = "text/markdown",
                [".mm"] = "text/plain",
                [".mkv"] = "video/x-matroska",
                [".mov"] = "video/quicktime",
                [".mp3"] = "audio/mpeg",
                [".mp4"] = "video/mp4",
                [".odp"] = "application/vnd.oasis.opendocument.presentation",
                [".ods"] = "application/vnd.oasis.opendocument.spreadsheet",
                [".odt"] = "application/vnd.oasis.opendocument.text",
                [".ogg"] = "audio/ogg",
                [".pdf"] = "application/pdf",
                [".php"] = "text/plain",
                [".png"] = "image/png",
                [".props"] = "application/xml",
                [".ppt"] = "application/vnd.ms-powerpoint",
                [".pptx"] = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                [".py"] = "text/x-python",
                [".rar"] = "application/vnd.rar",
                [".rb"] = "text/plain",
                [".rs"] = "text/plain",
                [".rtf"] = "application/rtf",
                [".sh"] = "application/x-sh",
                [".sln"] = "text/plain",
                [".svg"] = "image/svg+xml",
                [".swift"] = "text/plain",
                [".tar"] = "application/x-tar",
                [".targets"] = "application/xml",
                [".text"] = "text/plain",
                [".toml"] = "text/plain",
                [".ts"] = "application/typescript",
                [".txt"] = "text/plain",
                [".wav"] = "audio/wav",
                [".webm"] = "video/webm",
                [".webp"] = "image/webp",
                [".xls"] = "application/vnd.ms-excel",
                [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                [".xml"] = "application/xml",
                [".yaml"] = "application/yaml",
                [".yml"] = "application/yaml",
                [".zip"] = "application/zip",
                [".zsh"] = "text/plain",
            };

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

            string? contentType = ResolvePreferredContentType(file.Name, file.ContentType);
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
            return ResolvePreferredContentType(fileName, contentType) ?? "application/octet-stream";
        }

        public static string? ResolvePreferredContentType(string? fileName, string? contentType)
        {
            string mediaType = CottonFileKindClassifier.CreateContentTypeMediaType(contentType);
            if (!string.IsNullOrWhiteSpace(mediaType))
            {
                return mediaType;
            }

            string extension = string.IsNullOrWhiteSpace(fileName)
                ? string.Empty
                : Path.GetExtension(fileName.Trim());
            return ExtensionContentTypes.TryGetValue(extension, out string? resolvedContentType)
                ? resolvedContentType
                : null;
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
            return string.Equals(file.Kind, "PDF", StringComparison.OrdinalIgnoreCase)
                || string.Equals(contentType, "application/pdf", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsAudioPreview(CottonFileBrowserEntry file, string? contentType)
        {
            return string.Equals(file.Kind, "Audio", StringComparison.OrdinalIgnoreCase)
                || (contentType?.StartsWith("audio/", StringComparison.OrdinalIgnoreCase) ?? false);
        }

        private static bool IsVideoPreview(CottonFileBrowserEntry file, string? contentType)
        {
            return string.Equals(file.Kind, "Video", StringComparison.OrdinalIgnoreCase)
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
                "Text" => CottonSystemFileOpenKind.Text,
                "PDF" => CottonSystemFileOpenKind.Pdf,
                "Document" => CottonSystemFileOpenKind.Document,
                "Audio" => CottonSystemFileOpenKind.Audio,
                "Video" => CottonSystemFileOpenKind.Video,
                "SVG" => CottonSystemFileOpenKind.Svg,
                "Image" => CottonSystemFileOpenKind.Image,
                "File" when ArchiveFileExtensions.Contains(extension) => CottonSystemFileOpenKind.Archive,
                "File" when string.Equals(mediaType, "application/pdf", StringComparison.OrdinalIgnoreCase) =>
                    CottonSystemFileOpenKind.Pdf,
                "File" when mediaType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase) =>
                    CottonSystemFileOpenKind.Audio,
                "File" when mediaType.StartsWith("video/", StringComparison.OrdinalIgnoreCase) =>
                    CottonSystemFileOpenKind.Video,
                "File" when mediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) =>
                    CottonSystemFileOpenKind.Image,
                "File" => CottonSystemFileOpenKind.File,
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
