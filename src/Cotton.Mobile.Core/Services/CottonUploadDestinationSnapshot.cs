// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonUploadDestinationSnapshot
    {
        public CottonUploadDestinationSnapshot(Guid folderId, string folderName, string? path)
        {
            if (folderId == Guid.Empty)
            {
                throw new ArgumentException("Destination folder id is required.", nameof(folderId));
            }

            FolderId = folderId;
            FolderName = string.IsNullOrWhiteSpace(folderName) ? "Files" : folderName.Trim();
            Path = string.IsNullOrWhiteSpace(path) ? FolderName : path.Trim();
        }

        public Guid FolderId { get; }

        public string FolderName { get; }

        public string Path { get; }

        public CottonFolderHandle ToFolderHandle()
        {
            return new CottonFolderHandle(FolderId, FolderName);
        }
    }
}
