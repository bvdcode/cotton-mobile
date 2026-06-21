namespace Cotton.Mobile.Services
{
    public static class CottonTrashRestoreStatusText
    {
        public const string CancelledStatus = "Restore cancelled.";

        public const string ConflictStatus = "Restore conflict.";

        public const string FailedStatus = "Could not restore item.";

        public const string NotRestorableStatus = "Item cannot be restored.";

        public const string OfflineUnavailableStatus = "Offline. Restore needs internet.";

        public const string ParentMissingStatus = "Original folder is missing.";

        public const string RestoredStatus = "Item restored.";

        public static string CreateOutcomeStatus(CottonSyncRestoreOutcomeSnapshot outcome)
        {
            ArgumentNullException.ThrowIfNull(outcome);

            return outcome.Status switch
            {
                CottonSyncRestoreOutcomeStatus.Restored => RestoredStatus,
                CottonSyncRestoreOutcomeStatus.ParentMissingNeedsChoice => ParentMissingStatus,
                CottonSyncRestoreOutcomeStatus.ConflictNeedsChoice => ConflictStatus,
                CottonSyncRestoreOutcomeStatus.NotRestorable => NotRestorableStatus,
                _ => throw new ArgumentOutOfRangeException(nameof(outcome), "Restore outcome status is not supported."),
            };
        }

        public static string CreateRestoringStatus(string itemName)
        {
            string name = NormalizeItemName(itemName);
            return $"Restoring {name}...";
        }

        public static string CreateRestoredStatus(string itemName)
        {
            string name = NormalizeItemName(itemName);
            return $"{name} restored.";
        }

        private static string NormalizeItemName(string itemName)
        {
            return string.IsNullOrWhiteSpace(itemName) ? "Item" : itemName.Trim();
        }
    }
}
