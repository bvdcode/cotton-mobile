namespace Cotton.Mobile.Services
{
    public class CottonSyncJournalItemSnapshot
    {
        private CottonSyncJournalItemSnapshot(
            Guid id,
            Guid syncRootId,
            string syncRootStableKey,
            long sequence,
            CottonSyncJournalOperation operation,
            CottonFileBrowserEntryType targetType,
            Guid targetId,
            string displayName,
            CottonSyncJournalOrigin origin,
            CottonSyncJournalStatus status,
            int attemptCount,
            string? failureMessage,
            string? expectedETag,
            string? normalizedName,
            Guid? targetParentId,
            CottonSyncDeleteMode? deleteMode,
            DateTime createdAtUtc,
            DateTime updatedAtUtc)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException("Sync journal item id is required.", nameof(id));
            }

            if (syncRootId == Guid.Empty)
            {
                throw new ArgumentException("Sync root id is required.", nameof(syncRootId));
            }

            if (string.IsNullOrWhiteSpace(syncRootStableKey))
            {
                throw new ArgumentException("Sync root stable key is required.", nameof(syncRootStableKey));
            }

            if (sequence <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sequence), "Sync journal sequence must be positive.");
            }

            if (!Enum.IsDefined(operation))
            {
                throw new ArgumentOutOfRangeException(nameof(operation), "Sync journal operation is not supported.");
            }

            if (!Enum.IsDefined(targetType))
            {
                throw new ArgumentOutOfRangeException(nameof(targetType), "Sync journal target type is not supported.");
            }

            if (targetId == Guid.Empty)
            {
                throw new ArgumentException("Sync journal target id is required.", nameof(targetId));
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("Sync journal display name is required.", nameof(displayName));
            }

            if (!Enum.IsDefined(origin))
            {
                throw new ArgumentOutOfRangeException(nameof(origin), "Sync journal origin is not supported.");
            }

            if (!Enum.IsDefined(status))
            {
                throw new ArgumentOutOfRangeException(nameof(status), "Sync journal status is not supported.");
            }

            if (attemptCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(attemptCount), "Sync journal attempt count cannot be negative.");
            }

            if (status == CottonSyncJournalStatus.Failed && string.IsNullOrWhiteSpace(failureMessage))
            {
                throw new ArgumentException("Failed sync journal items require a failure message.", nameof(failureMessage));
            }

            ValidateOperationPayload(operation, normalizedName, targetParentId, deleteMode);

            Id = id;
            SyncRootId = syncRootId;
            SyncRootStableKey = syncRootStableKey.Trim();
            Sequence = sequence;
            Operation = operation;
            TargetType = targetType;
            TargetId = targetId;
            DisplayName = displayName.Trim();
            Origin = origin;
            Status = status;
            AttemptCount = attemptCount;
            FailureMessage = string.IsNullOrWhiteSpace(failureMessage) ? null : failureMessage.Trim();
            ExpectedETag = string.IsNullOrWhiteSpace(expectedETag) ? null : expectedETag.Trim();
            NormalizedName = string.IsNullOrWhiteSpace(normalizedName) ? null : normalizedName.Trim();
            TargetParentId = targetParentId == Guid.Empty ? null : targetParentId;
            DeleteMode = deleteMode;
            CreatedAtUtc = NormalizeUtc(createdAtUtc);
            UpdatedAtUtc = NormalizeUtc(updatedAtUtc);
        }

        public Guid Id { get; }

        public Guid SyncRootId { get; }

        public string SyncRootStableKey { get; }

        public long Sequence { get; }

        public CottonSyncJournalOperation Operation { get; }

        public CottonFileBrowserEntryType TargetType { get; }

        public Guid TargetId { get; }

        public string DisplayName { get; }

        public CottonSyncJournalOrigin Origin { get; }

        public CottonSyncJournalStatus Status { get; }

        public int AttemptCount { get; }

        public string? FailureMessage { get; }

        public string? ExpectedETag { get; }

        public string? NormalizedName { get; }

        public Guid? TargetParentId { get; }

        public CottonSyncDeleteMode? DeleteMode { get; }

        public DateTime CreatedAtUtc { get; }

        public DateTime UpdatedAtUtc { get; }

        public bool IsTerminal => Status is CottonSyncJournalStatus.Completed or CottonSyncJournalStatus.Cancelled;

        public bool CanRetry => Status == CottonSyncJournalStatus.Failed;

        public bool CanCancel => !IsTerminal;

        public bool IsConflict => Status == CottonSyncJournalStatus.Conflict;

        public static CottonSyncJournalItemSnapshot CreateRenameMove(
            Guid id,
            CottonSyncRootSnapshot root,
            CottonFileBrowserEntry entry,
            CottonSyncRenameMoveSemanticsSnapshot semantics,
            CottonSyncJournalOrigin origin,
            long sequence,
            DateTime createdAtUtc)
        {
            ArgumentNullException.ThrowIfNull(root);
            ArgumentNullException.ThrowIfNull(entry);
            ArgumentNullException.ThrowIfNull(semantics);
            EnsureReadyRoot(root);
            EnsureConflictSafeMutation(semantics.HasConflictPrecondition);
            EnsureTargetTypeMatches(entry, semantics.TargetType);

            CottonSyncJournalOperation operation = semantics.Operation switch
            {
                CottonSyncRenameMoveOperation.Rename => CottonSyncJournalOperation.Rename,
                CottonSyncRenameMoveOperation.Move => CottonSyncJournalOperation.Move,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(semantics),
                    "Rename/move operation is not supported."),
            };

            return new CottonSyncJournalItemSnapshot(
                id,
                root.Id,
                root.StableKey,
                sequence,
                operation,
                entry.Type,
                entry.Id,
                entry.Name,
                origin,
                CottonSyncJournalStatus.Pending,
                attemptCount: 0,
                failureMessage: null,
                semantics.ExpectedETag,
                semantics.NormalizedName,
                semantics.TargetParentId,
                deleteMode: null,
                createdAtUtc,
                createdAtUtc);
        }

        public static CottonSyncJournalItemSnapshot CreateDelete(
            Guid id,
            CottonSyncRootSnapshot root,
            CottonFileBrowserEntry entry,
            CottonSyncDeleteSemanticsSnapshot semantics,
            CottonSyncJournalOrigin origin,
            long sequence,
            DateTime createdAtUtc)
        {
            ArgumentNullException.ThrowIfNull(root);
            ArgumentNullException.ThrowIfNull(entry);
            ArgumentNullException.ThrowIfNull(semantics);
            EnsureReadyRoot(root);
            EnsureConflictSafeMutation(semantics.HasConflictPrecondition);
            EnsureTargetTypeMatches(entry, semantics.TargetType);

            return new CottonSyncJournalItemSnapshot(
                id,
                root.Id,
                root.StableKey,
                sequence,
                CottonSyncJournalOperation.Delete,
                entry.Type,
                entry.Id,
                entry.Name,
                origin,
                CottonSyncJournalStatus.Pending,
                attemptCount: 0,
                failureMessage: null,
                semantics.ExpectedETag,
                normalizedName: null,
                targetParentId: null,
                semantics.Mode,
                createdAtUtc,
                createdAtUtc);
        }

        public static CottonSyncJournalItemSnapshot Restore(
            Guid id,
            Guid syncRootId,
            string syncRootStableKey,
            long sequence,
            CottonSyncJournalOperation operation,
            CottonFileBrowserEntryType targetType,
            Guid targetId,
            string displayName,
            CottonSyncJournalOrigin origin,
            CottonSyncJournalStatus status,
            int attemptCount,
            string? failureMessage,
            string? expectedETag,
            string? normalizedName,
            Guid? targetParentId,
            CottonSyncDeleteMode? deleteMode,
            DateTime createdAtUtc,
            DateTime updatedAtUtc)
        {
            return new CottonSyncJournalItemSnapshot(
                id,
                syncRootId,
                syncRootStableKey,
                sequence,
                operation,
                targetType,
                targetId,
                displayName,
                origin,
                status,
                attemptCount,
                failureMessage,
                expectedETag,
                normalizedName,
                targetParentId,
                deleteMode,
                createdAtUtc,
                updatedAtUtc);
        }

        public CottonSyncJournalItemSnapshot Start(DateTime updatedAtUtc)
        {
            EnsureStatus([CottonSyncJournalStatus.Pending], nameof(Start));
            return With(CottonSyncJournalStatus.Running, AttemptCount + 1, failureMessage: null, updatedAtUtc);
        }

        public CottonSyncJournalItemSnapshot Complete(DateTime updatedAtUtc)
        {
            EnsureStatus([CottonSyncJournalStatus.Running], nameof(Complete));
            return With(CottonSyncJournalStatus.Completed, AttemptCount, failureMessage: null, updatedAtUtc);
        }

        public CottonSyncJournalItemSnapshot Fail(string failureMessage, DateTime updatedAtUtc)
        {
            EnsureStatus([CottonSyncJournalStatus.Running], nameof(Fail));
            return With(CottonSyncJournalStatus.Failed, AttemptCount, failureMessage, updatedAtUtc);
        }

        public CottonSyncJournalItemSnapshot MarkConflict(string conflictMessage, DateTime updatedAtUtc)
        {
            EnsureStatus([CottonSyncJournalStatus.Pending, CottonSyncJournalStatus.Running], nameof(MarkConflict));
            return With(CottonSyncJournalStatus.Conflict, AttemptCount, conflictMessage, updatedAtUtc);
        }

        public CottonSyncJournalItemSnapshot Retry(DateTime updatedAtUtc)
        {
            EnsureStatus([CottonSyncJournalStatus.Failed], nameof(Retry));
            return With(CottonSyncJournalStatus.Pending, AttemptCount, failureMessage: null, updatedAtUtc);
        }

        public CottonSyncJournalItemSnapshot Cancel(DateTime updatedAtUtc)
        {
            if (!CanCancel)
            {
                throw new InvalidOperationException($"Cannot cancel a {Status} sync journal item.");
            }

            return With(CottonSyncJournalStatus.Cancelled, AttemptCount, failureMessage: null, updatedAtUtc);
        }

        public CottonSyncJournalItemSnapshot RestoreAfterRestart(DateTime updatedAtUtc)
        {
            return Status == CottonSyncJournalStatus.Running
                ? With(CottonSyncJournalStatus.Pending, AttemptCount, failureMessage: null, updatedAtUtc)
                : this;
        }

        private CottonSyncJournalItemSnapshot With(
            CottonSyncJournalStatus status,
            int attemptCount,
            string? failureMessage,
            DateTime updatedAtUtc)
        {
            return new CottonSyncJournalItemSnapshot(
                Id,
                SyncRootId,
                SyncRootStableKey,
                Sequence,
                Operation,
                TargetType,
                TargetId,
                DisplayName,
                Origin,
                status,
                attemptCount,
                failureMessage,
                ExpectedETag,
                NormalizedName,
                TargetParentId,
                DeleteMode,
                CreatedAtUtc,
                updatedAtUtc);
        }

        private void EnsureStatus(CottonSyncJournalStatus[] allowedStatuses, string operationName)
        {
            if (!allowedStatuses.Contains(Status))
            {
                throw new InvalidOperationException($"Cannot {operationName} a {Status} sync journal item.");
            }
        }

        private static void ValidateOperationPayload(
            CottonSyncJournalOperation operation,
            string? normalizedName,
            Guid? targetParentId,
            CottonSyncDeleteMode? deleteMode)
        {
            if (operation == CottonSyncJournalOperation.Rename && string.IsNullOrWhiteSpace(normalizedName))
            {
                throw new ArgumentException("Rename journal items require a normalized name.", nameof(normalizedName));
            }

            if (operation == CottonSyncJournalOperation.Move && (!targetParentId.HasValue || targetParentId == Guid.Empty))
            {
                throw new ArgumentException("Move journal items require a target parent id.", nameof(targetParentId));
            }

            if (operation == CottonSyncJournalOperation.Delete)
            {
                if (!deleteMode.HasValue)
                {
                    throw new ArgumentException("Delete journal items require a delete mode.", nameof(deleteMode));
                }

                if (!Enum.IsDefined(deleteMode.Value))
                {
                    throw new ArgumentOutOfRangeException(nameof(deleteMode), "Delete mode is not supported.");
                }
            }
        }

        private static void EnsureReadyRoot(CottonSyncRootSnapshot root)
        {
            if (!root.CanRunSync)
            {
                throw new InvalidOperationException("Sync journal items require a ready sync root.");
            }
        }

        private static void EnsureConflictSafeMutation(bool hasConflictPrecondition)
        {
            if (!hasConflictPrecondition)
            {
                throw new InvalidOperationException("Sync journal items require a conflict-safe mutation precondition.");
            }
        }

        private static void EnsureTargetTypeMatches(
            CottonFileBrowserEntry entry,
            CottonFileBrowserEntryType targetType)
        {
            if (entry.Type != targetType)
            {
                throw new ArgumentException("Sync journal target type does not match the file browser entry.", nameof(entry));
            }
        }

        private static DateTime NormalizeUtc(DateTime value)
        {
            return value.Kind == DateTimeKind.Utc
                ? value
                : DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }
    }
}
