// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonFileTrashMoveResult
    {
        private CottonFileTrashMoveResult(Guid fileId, string fileName, string statusText)
        {
            if (fileId == Guid.Empty)
            {
                throw new ArgumentException("File id is required.", nameof(fileId));
            }

            FileId = fileId;
            FileName = string.IsNullOrWhiteSpace(fileName)
                ? "file"
                : fileName.Trim();
            StatusText = string.IsNullOrWhiteSpace(statusText)
                ? CottonFileTrashStatusText.CreateMovedStatus(FileName)
                : statusText.Trim();
        }

        public Guid FileId { get; }

        public string FileName { get; }

        public string StatusText { get; }

        public static CottonFileTrashMoveResult Moved(CottonFileBrowserEntry file)
        {
            ArgumentNullException.ThrowIfNull(file);

            return new CottonFileTrashMoveResult(
                file.Id,
                file.Name,
                CottonFileTrashStatusText.CreateMovedStatus(file.Name));
        }
    }
}
