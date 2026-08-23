// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public static class CottonFileContentTypeResolver
    {
        private static readonly Dictionary<string, string> ContentTypesByExtension =
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
                [".heic"] = "image/heic",
                [".hpp"] = "text/plain",
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
                [".mkv"] = "video/x-matroska",
                [".mm"] = "text/plain",
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
                [".ppt"] = "application/vnd.ms-powerpoint",
                [".pptx"] = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                [".props"] = "application/xml",
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

        public static string ResolveRequired(string? fileName, string? contentType)
        {
            return Resolve(fileName, contentType) ?? "application/octet-stream";
        }

        public static string? Resolve(string? fileName, string? contentType)
        {
            string mediaType = CottonFileKindClassifier.CreateContentTypeMediaType(contentType);
            if (!string.IsNullOrWhiteSpace(mediaType))
            {
                return mediaType;
            }

            string extension = string.IsNullOrWhiteSpace(fileName)
                ? string.Empty
                : Path.GetExtension(fileName.Trim());
            return ContentTypesByExtension.GetValueOrDefault(extension);
        }
    }
}
