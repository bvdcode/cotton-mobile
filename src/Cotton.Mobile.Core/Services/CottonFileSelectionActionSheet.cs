namespace Cotton.Mobile.Services
{
    public static class CottonFileSelectionActionSheet
    {
        public static IReadOnlyList<CottonFileBulkActionSnapshot> CreateActions(
            CottonFileSelectionSnapshot selection)
        {
            ArgumentNullException.ThrowIfNull(selection);

            return selection.Actions
                .Where(action => action.IsEnabled)
                .Where(action => IsSupportedAction(selection, action))
                .ToArray();
        }

        private static bool IsSupportedAction(
            CottonFileSelectionSnapshot selection,
            CottonFileBulkActionSnapshot action)
        {
            if (action.Kind is CottonFileBulkActionKind.CopyLinks or CottonFileBulkActionKind.ShareLinks)
            {
                return true;
            }

            if (action.Kind == CottonFileBulkActionKind.DownloadFiles)
            {
                return selection.FileCount > 0 && !selection.HasFolders;
            }

            if (action.Kind == CottonFileBulkActionKind.KeepOffline)
            {
                return (selection.FileCount > 0 && !selection.HasFolders)
                    || selection.Count == 1 && selection.FolderCount == 1;
            }

            if (action.Kind == CottonFileBulkActionKind.RemoveOffline)
            {
                return selection.FileCount > 0 && !selection.HasFolders;
            }

            if (action.Kind == CottonFileBulkActionKind.ShareLocalFiles)
            {
                return selection.FileCount > 0 && !selection.HasFolders;
            }

            return false;
        }
    }
}
