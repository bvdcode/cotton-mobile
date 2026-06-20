namespace Cotton.Mobile.Services
{
    public static class CottonFileAddActionSheet
    {
        public const string CancelAction = "Cancel";
        public const string NewFolderAction = "New folder";
        public const string UploadFileAction = "Upload file";
        public const string UploadPhotoAction = "Upload photo";
        public const string UploadVideoAction = "Upload video";
        public const string UploadPhotoToFolderAction = "Upload photo to folder";
        public const string UploadVideoToFolderAction = "Upload video to folder";

        private static readonly string[] Actions =
        [
            NewFolderAction,
            UploadFileAction,
            UploadPhotoAction,
            UploadVideoAction,
            UploadPhotoToFolderAction,
            UploadVideoToFolderAction,
        ];

        public static IReadOnlyList<string> CreateActions()
        {
            return Actions;
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
