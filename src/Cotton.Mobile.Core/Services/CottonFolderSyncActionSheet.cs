namespace Cotton.Mobile.Services
{
    public static class CottonFolderSyncActionSheet
    {
        public const string MainAction = "Sync this folder";
        public const string Title = "Sync this folder";
        public const string DownloadToDeviceAction = "Download to this device";
        public const string ChooseDeviceFolderAction = "Choose a device folder";
        public const string UploadFromDeviceFolderAction = "Upload from a device folder";
        public const string KeepBothFoldersInSyncAction = "Keep both folders in sync";

        private static readonly string[] AppFolderActions =
        [
            DownloadToDeviceAction,
        ];

        private static readonly string[] DeviceFolderActions =
        [
            DownloadToDeviceAction,
            ChooseDeviceFolderAction,
            UploadFromDeviceFolderAction,
            KeepBothFoldersInSyncAction,
        ];

        public static IReadOnlyList<string> CreateModeActions(bool canChooseDeviceFolder)
        {
            return canChooseDeviceFolder ? DeviceFolderActions : AppFolderActions;
        }
    }
}
