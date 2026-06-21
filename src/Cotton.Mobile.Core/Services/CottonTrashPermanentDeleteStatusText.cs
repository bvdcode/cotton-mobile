namespace Cotton.Mobile.Services
{
    public static class CottonTrashPermanentDeleteStatusText
    {
        public const string CancelledStatus = "Delete forever cancelled.";

        public const string ConfirmAction = "Delete forever";

        public const string ConfirmTitle = "Delete forever?";

        public const string FailedStatus = "Could not permanently delete item.";

        public const string NeedsRefreshStatus = "Refresh trash before permanently deleting this file.";

        public const string OfflineUnavailableStatus = "Offline. Delete forever needs internet.";

        public static string CreateConfirmMessage(string itemName, CottonFileBrowserEntryType itemType)
        {
            string name = NormalizeItemName(itemName);
            return itemType == CottonFileBrowserEntryType.Folder
                ? $"Permanently delete {name} and its contents? This cannot be undone."
                : $"Permanently delete {name}? This cannot be undone.";
        }

        public static string CreateDeletingStatus(string itemName)
        {
            string name = NormalizeItemName(itemName);
            return $"Deleting {name} forever...";
        }

        public static string CreateDeletedStatus(string itemName)
        {
            string name = NormalizeItemName(itemName);
            return $"{name} permanently deleted.";
        }

        public static string CreateDeletedNeedsRefreshStatus(string itemName)
        {
            string name = NormalizeItemName(itemName);
            return $"{name} permanently deleted. Refresh to update trash.";
        }

        private static string NormalizeItemName(string itemName)
        {
            return string.IsNullOrWhiteSpace(itemName) ? "Item" : itemName.Trim();
        }
    }
}
