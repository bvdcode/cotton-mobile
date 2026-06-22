// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class UnavailablePdfPreviewRenderer : IPdfPreviewRenderer
    {
        public Task<PdfPreviewDocumentSnapshot> RenderAsync(
            string filePath,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("PDF preview is not available on this platform.");
        }
    }
}
