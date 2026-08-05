// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Resources.Localization;

namespace Cotton.Mobile.Services
{
    public static class CottonFileKindDisplayName
    {
        public static string Create(string kind)
        {
            return kind switch
            {
                "Folder" => CoreResources.FolderKind,
                "Image" => CoreResources.ImageKind,
                "PDF" => CoreResources.PdfKind,
                "Document" => CoreResources.DocumentKind,
                "Video" => CoreResources.VideoKind,
                "Audio" => CoreResources.AudioKind,
                "SVG" => CoreResources.SvgKind,
                "Text" => CoreResources.TextKind,
                "File" => CoreResources.FileKind,
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "File kind is not supported."),
            };
        }
    }
}
