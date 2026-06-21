using Cotton.Files;

namespace Cotton.Mobile.Services
{
    public class CottonTrashRestoreResult
    {
        public CottonTrashRestoreResult(
            Guid itemId,
            CottonFileBrowserEntryType itemType,
            CottonTrashRestoreRetryMode retryMode,
            CottonSyncRestoreOutcomeSnapshot outcome)
        {
            if (itemId == Guid.Empty)
            {
                throw new ArgumentException("Item id is required.", nameof(itemId));
            }

            ValidateItemType(itemType);
            if (!Enum.IsDefined(retryMode))
            {
                throw new ArgumentOutOfRangeException(nameof(retryMode), "Restore retry mode is not supported.");
            }

            ArgumentNullException.ThrowIfNull(outcome);

            ItemId = itemId;
            ItemType = itemType;
            RetryMode = retryMode;
            Outcome = outcome;
        }

        public Guid ItemId { get; }

        public CottonFileBrowserEntryType ItemType { get; }

        public CottonTrashRestoreRetryMode RetryMode { get; }

        public CottonSyncRestoreOutcomeSnapshot Outcome { get; }

        public CottonSyncRestoreOutcomeStatus Status => Outcome.Status;

        public bool CanRetryWithCreateMissingParents => Outcome.CanRetryWithCreateMissingParents;

        public bool CanRetryWithOverwrite => Outcome.CanRetryWithOverwrite;

        public bool IsRestored => Status == CottonSyncRestoreOutcomeStatus.Restored;

        public bool IsTerminal => Outcome.IsTerminal;

        public string StatusText => CottonTrashRestoreStatusText.CreateOutcomeStatus(Outcome);

        public static CottonTrashRestoreResult Create(
            Guid itemId,
            CottonFileBrowserEntryType itemType,
            CottonTrashRestoreRetryMode retryMode,
            RestoreOutcomeDto outcome)
        {
            return new CottonTrashRestoreResult(
                itemId,
                itemType,
                retryMode,
                CottonSyncRestorePolicy.CreateOutcome(outcome));
        }

        private static void ValidateItemType(CottonFileBrowserEntryType itemType)
        {
            if (itemType is not CottonFileBrowserEntryType.File and not CottonFileBrowserEntryType.Folder)
            {
                throw new ArgumentOutOfRangeException(nameof(itemType), "Restore item type is not supported.");
            }
        }
    }
}
