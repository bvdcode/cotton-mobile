// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Resources.Localization;

namespace Cotton.Mobile.Services
{
    public static class CottonFileKindDisplayName
    {
        public static string Create(CottonFileKind kind)
        {
            return kind switch
            {
                CottonFileKind.Folder => CoreResources.FolderKind,
                CottonFileKind.Image => CoreResources.ImageKind,
                CottonFileKind.Pdf => CoreResources.PdfKind,
                CottonFileKind.Document => CoreResources.DocumentKind,
                CottonFileKind.Video => CoreResources.VideoKind,
                CottonFileKind.Audio => CoreResources.AudioKind,
                CottonFileKind.Svg => CoreResources.SvgKind,
                CottonFileKind.Text => CoreResources.TextKind,
                CottonFileKind.File => CoreResources.FileKind,
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "File kind is not supported."),
            };
        }
    }
}
