namespace Cotton.Mobile.Services
{
    public class CottonFolderTrashMoveResult
    {
        private CottonFolderTrashMoveResult(Guid folderId, string folderName, string statusText)
        {
            if (folderId == Guid.Empty)
            {
                throw new ArgumentException("Folder id is required.", nameof(folderId));
            }

            FolderId = folderId;
            FolderName = string.IsNullOrWhiteSpace(folderName)
                ? "folder"
                : folderName.Trim();
            StatusText = string.IsNullOrWhiteSpace(statusText)
                ? CottonFolderTrashStatusText.CreateMovedStatus(FolderName)
                : statusText.Trim();
        }

        public Guid FolderId { get; }

        public string FolderName { get; }

        public string StatusText { get; }

        public static CottonFolderTrashMoveResult Moved(CottonFileBrowserEntry folder)
        {
            ArgumentNullException.ThrowIfNull(folder);

            return new CottonFolderTrashMoveResult(
                folder.Id,
                folder.Name,
                CottonFolderTrashStatusText.CreateMovedStatus(folder.Name));
        }
    }
}
