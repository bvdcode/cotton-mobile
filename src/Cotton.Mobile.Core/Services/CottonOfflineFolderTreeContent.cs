namespace Cotton.Mobile.Services
{
    public class CottonOfflineFolderTreeContent
    {
        public CottonOfflineFolderTreeContent(
            CottonFolderContent content,
            IReadOnlyList<CottonOfflineFolderTreeContent> childFolders)
        {
            ArgumentNullException.ThrowIfNull(content);
            ArgumentNullException.ThrowIfNull(childFolders);

            HashSet<Guid> folderEntryIds = content
                .Entries
                .Where(entry => entry.Type == CottonFileBrowserEntryType.Folder)
                .Select(entry => entry.Id)
                .ToHashSet();
            if (childFolders.Select(child => child.Content.FolderId).Distinct().Count() != childFolders.Count)
            {
                throw new ArgumentException("Child folder content ids must be unique.", nameof(childFolders));
            }

            if (childFolders.Any(child => !folderEntryIds.Contains(child.Content.FolderId)))
            {
                throw new ArgumentException("Child folder content must match a direct folder entry.", nameof(childFolders));
            }

            Content = content;
            ChildFolders = childFolders;
        }

        public CottonFolderContent Content { get; }

        public IReadOnlyList<CottonOfflineFolderTreeContent> ChildFolders { get; }

        public IReadOnlyList<CottonFileBrowserEntry> GetFilesDepthFirst()
        {
            var files = new List<CottonFileBrowserEntry>();
            AddFilesDepthFirst(files);
            return files;
        }

        public IReadOnlyList<CottonOfflineFolderTreeFileSnapshot> GetFilesWithDisplayPathsDepthFirst()
        {
            var files = new List<CottonOfflineFolderTreeFileSnapshot>();
            AddFilesWithDisplayPathsDepthFirst(files, parentPath: null);
            return files;
        }

        public static CottonOfflineFolderTreeContent Create(
            CottonFolderContent content,
            params CottonOfflineFolderTreeContent[] childFolders)
        {
            return new CottonOfflineFolderTreeContent(content, childFolders);
        }

        private void AddFilesDepthFirst(List<CottonFileBrowserEntry> files)
        {
            files.AddRange(Content.Entries.Where(entry => entry.Type == CottonFileBrowserEntryType.File));
            foreach (CottonOfflineFolderTreeContent childFolder in ChildFolders)
            {
                childFolder.AddFilesDepthFirst(files);
            }
        }

        private void AddFilesWithDisplayPathsDepthFirst(
            List<CottonOfflineFolderTreeFileSnapshot> files,
            string? parentPath)
        {
            files.AddRange(Content
                .Entries
                .Where(entry => entry.Type == CottonFileBrowserEntryType.File)
                .Select(file => new CottonOfflineFolderTreeFileSnapshot(
                    file,
                    CreateDisplayPath(parentPath, file.Name))));

            foreach (CottonOfflineFolderTreeContent childFolder in ChildFolders)
            {
                CottonFileBrowserEntry folderEntry = Content
                    .Entries
                    .First(entry => entry.Type == CottonFileBrowserEntryType.Folder
                        && entry.Id == childFolder.Content.FolderId);
                childFolder.AddFilesWithDisplayPathsDepthFirst(
                    files,
                    CreateDisplayPath(parentPath, folderEntry.Name));
            }
        }

        private static string CreateDisplayPath(string? parentPath, string name)
        {
            string trimmedName = name.Trim();
            return string.IsNullOrWhiteSpace(parentPath)
                ? trimmedName
                : $"{parentPath.Trim()}/{trimmedName}";
        }
    }
}
