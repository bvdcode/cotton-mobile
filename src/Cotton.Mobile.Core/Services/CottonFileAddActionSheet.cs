namespace Cotton.Mobile.Services
{
    public static class CottonFileAddActionSheet
    {
        public const string CancelAction = "Cancel";
        public const string NewFolderAction = "New folder";
        public const string UploadFileAction = "Upload file";
        public const string ScanDocumentAction = "Scan document";
        public const string UploadPhotoAction = "Upload photo";
        public const string UploadVideoAction = "Upload video";
        public const string UploadPhotoToFolderAction = "Upload photo to folder";
        public const string UploadVideoToFolderAction = "Upload video to folder";

        private static readonly string[] ActionsWithDocumentScan =
        [
            NewFolderAction,
            UploadFileAction,
            ScanDocumentAction,
            UploadPhotoAction,
            UploadVideoAction,
            UploadPhotoToFolderAction,
            UploadVideoToFolderAction,
        ];

        private static readonly string[] ActionsWithoutDocumentScan =
        [
            NewFolderAction,
            UploadFileAction,
            UploadPhotoAction,
            UploadVideoAction,
            UploadPhotoToFolderAction,
            UploadVideoToFolderAction,
        ];

        public static IReadOnlyList<string> CreateActions(bool canScanDocument = true)
        {
            return canScanDocument ? ActionsWithDocumentScan : ActionsWithoutDocumentScan;
        }

        public static string CreateTitle(CottonFolderHandle folder)
        {
            ArgumentNullException.ThrowIfNull(folder);

            return string.IsNullOrWhiteSpace(folder.Name)
                ? "Add"
                : $"Add to {folder.Name.Trim()}";
        }
    }
}
