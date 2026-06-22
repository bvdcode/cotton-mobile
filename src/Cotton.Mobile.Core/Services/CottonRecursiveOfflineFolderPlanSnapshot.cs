// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonRecursiveOfflineFolderPlanSnapshot
    {
        public CottonRecursiveOfflineFolderPlanSnapshot(
            Guid folderId,
            string folderName,
            int fileCount,
            int folderCount,
            long knownSizeBytes,
            int unknownSizeFileCount,
            int missingFolderContentCount)
        {
            if (folderId == Guid.Empty)
            {
                throw new ArgumentException("Folder id is required.", nameof(folderId));
            }

            if (string.IsNullOrWhiteSpace(folderName))
            {
                throw new ArgumentException("Folder name is required.", nameof(folderName));
            }

            if (fileCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(fileCount), "File count cannot be negative.");
            }

            if (folderCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(folderCount), "Folder count cannot be negative.");
            }

            if (knownSizeBytes < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(knownSizeBytes), "Size cannot be negative.");
            }

            if (unknownSizeFileCount < 0 || unknownSizeFileCount > fileCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(unknownSizeFileCount),
                    "Unknown-size file count must fit within the file count.");
            }

            if (missingFolderContentCount < 0 || missingFolderContentCount > folderCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(missingFolderContentCount),
                    "Missing folder content count must fit within the folder count.");
            }

            FolderId = folderId;
            FolderName = folderName.Trim();
            FileCount = fileCount;
            FolderCount = folderCount;
            KnownSizeBytes = knownSizeBytes;
            UnknownSizeFileCount = unknownSizeFileCount;
            MissingFolderContentCount = missingFolderContentCount;
            Status = ResolveStatus(fileCount, unknownSizeFileCount, missingFolderContentCount);
        }

        public Guid FolderId { get; }

        public string FolderName { get; }

        public int FileCount { get; }

        public int FolderCount { get; }

        public long KnownSizeBytes { get; }

        public int UnknownSizeFileCount { get; }

        public int MissingFolderContentCount { get; }

        public CottonRecursiveOfflineFolderPlanStatus Status { get; }

        public bool HasExactSize => UnknownSizeFileCount == 0;

        public bool CanQueueRecursiveFiles => Status == CottonRecursiveOfflineFolderPlanStatus.Ready;

        public static CottonRecursiveOfflineFolderPlanSnapshot Create(CottonOfflineFolderTreeContent tree)
        {
            ArgumentNullException.ThrowIfNull(tree);

            int fileCount = 0;
            int folderCount = 0;
            long knownSizeBytes = 0;
            int unknownSizeFileCount = 0;
            int missingFolderContentCount = 0;

            AddTree(tree);
            return new CottonRecursiveOfflineFolderPlanSnapshot(
                tree.Content.FolderId,
                tree.Content.FolderName,
                fileCount,
                folderCount,
                knownSizeBytes,
                unknownSizeFileCount,
                missingFolderContentCount);

            void AddTree(CottonOfflineFolderTreeContent currentTree)
            {
                foreach (CottonFileBrowserEntry entry in currentTree.Content.Entries)
                {
                    if (entry.Type == CottonFileBrowserEntryType.File)
                    {
                        fileCount++;
                        if (entry.SizeBytes.HasValue)
                        {
                            knownSizeBytes += entry.SizeBytes.Value;
                        }
                        else
                        {
                            unknownSizeFileCount++;
                        }

                        continue;
                    }

                    if (entry.Type != CottonFileBrowserEntryType.Folder)
                    {
                        continue;
                    }

                    folderCount++;
                    CottonOfflineFolderTreeContent? childFolder = currentTree
                        .ChildFolders
                        .SingleOrDefault(child => child.Content.FolderId == entry.Id);
                    if (childFolder is null)
                    {
                        missingFolderContentCount++;
                        continue;
                    }

                    AddTree(childFolder);
                }
            }
        }

        private static CottonRecursiveOfflineFolderPlanStatus ResolveStatus(
            int fileCount,
            int unknownSizeFileCount,
            int missingFolderContentCount)
        {
            if (fileCount == 0 && missingFolderContentCount == 0)
            {
                return CottonRecursiveOfflineFolderPlanStatus.Empty;
            }

            if (missingFolderContentCount > 0)
            {
                return CottonRecursiveOfflineFolderPlanStatus.NeedsFolderScan;
            }

            return unknownSizeFileCount > 0
                ? CottonRecursiveOfflineFolderPlanStatus.HasUnknownSize
                : CottonRecursiveOfflineFolderPlanStatus.Ready;
        }
    }
}
