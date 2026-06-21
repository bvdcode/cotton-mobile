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

        public const string RestoreAction = "Restore";

        public const string RestoreTitle = "Restore item?";

        public const string RestoredStatus = "Item restored.";

        public const string TimedOutStatus = "Restore is taking longer than expected. Refresh and try again.";

        public const string CreateMissingParentsAction = "Create folders";

        public const string ParentMissingTitle = "Original folder missing";

        public const string OverwriteAction = "Overwrite";

        public const string ConflictTitle = "Name conflict";

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

        public static string CreateConfirmMessage(string itemName)
        {
            string name = NormalizeItemName(itemName);
            return $"Restore {name} to its original folder?";
        }

        public static string CreateParentMissingMessage(string itemName)
        {
            string name = NormalizeItemName(itemName);
            return $"Create the missing original folders and restore {name}?";
        }

        public static string CreateConflictMessage(string itemName)
        {
            string name = NormalizeItemName(itemName);
            return $"An item with this name already exists. Overwrite it and restore {name}?";
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
