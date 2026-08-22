// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.Services
{
    public static partial class CottonSyncDiagnosticLog
    {
        [LoggerMessage(EventId = 2101, Level = LogLevel.Information, Message = "Manual sync started for root {RootId}.")]
        public static partial void ManualRootStarted(ILogger logger, Guid rootId);

        [LoggerMessage(EventId = 2102, Level = LogLevel.Information, Message = "Manual sync-all started for {RootCount} roots.")]
        public static partial void ManualAllStarted(ILogger logger, int rootCount);

        [LoggerMessage(EventId = 2103, Level = LogLevel.Information, Message = "Manual sync completed for root {RootId}: {ResultCount} results, {CompletedCount} completed.")]
        public static partial void ManualRootCompleted(
            ILogger logger,
            Guid rootId,
            int resultCount,
            int completedCount);

        [LoggerMessage(EventId = 2104, Level = LogLevel.Information, Message = "Manual sync-all completed for {RootCount} roots.")]
        public static partial void ManualAllCompleted(ILogger logger, int rootCount);

        [LoggerMessage(EventId = 2105, Level = LogLevel.Warning, Message = "Manual sync failed for root {RootId}.")]
        public static partial void ManualRootFailed(ILogger logger, Guid rootId, Exception exception);

        [LoggerMessage(EventId = 2106, Level = LogLevel.Warning, Message = "Manual sync-all failed.")]
        public static partial void ManualAllFailed(ILogger logger, Exception exception);

        [LoggerMessage(EventId = 2110, Level = LogLevel.Information, Message = "Automatic sync requested by trigger {Trigger}.")]
        public static partial void AutomaticRequested(ILogger logger, CottonAutomaticSyncTrigger trigger);

        [LoggerMessage(EventId = 2111, Level = LogLevel.Information, Message = "Automatic sync requested for {RootCount} retry roots.")]
        public static partial void AutomaticRootsRequested(ILogger logger, int rootCount);

        [LoggerMessage(EventId = 2112, Level = LogLevel.Information, Message = "Automatic sync selected {SelectedRootCount} of {RootCount} roots.")]
        public static partial void AutomaticRootsSelected(
            ILogger logger,
            int selectedRootCount,
            int rootCount);

        [LoggerMessage(EventId = 2113, Level = LogLevel.Information, Message = "Automatic sync completed: {SucceededCount} succeeded, {FailedCount} failed.")]
        public static partial void AutomaticCompleted(
            ILogger logger,
            int succeededCount,
            int failedCount);

        [LoggerMessage(EventId = 2120, Level = LogLevel.Information, Message = "Sync root {RootId} started with local storage {StorageKind}.")]
        public static partial void RootStarted(
            ILogger logger,
            Guid rootId,
            CottonSyncRootStorageKind storageKind);

        [LoggerMessage(EventId = 2121, Level = LogLevel.Information, Message = "Sync root {RootId} was skipped with status {Status}.")]
        public static partial void RootSkipped(
            ILogger logger,
            Guid rootId,
            CottonDeviceToCloudSyncRootRunStatus status);

        [LoggerMessage(EventId = 2122, Level = LogLevel.Information, Message = "Sync root {RootId} local scan completed: {ItemCount} items, {ProblemCount} problems.")]
        public static partial void LocalScanCompleted(
            ILogger logger,
            Guid rootId,
            int itemCount,
            int problemCount);

        [LoggerMessage(EventId = 2123, Level = LogLevel.Information, Message = "Sync root {RootId} cloud scan completed: {ItemCount} items.")]
        public static partial void CloudScanCompleted(ILogger logger, Guid rootId, int itemCount);

        [LoggerMessage(EventId = 2124, Level = LogLevel.Information, Message = "Sync root {RootId} loaded {ReceiptCount} upload receipts.")]
        public static partial void ReceiptsLoaded(ILogger logger, Guid rootId, int receiptCount);

        [LoggerMessage(EventId = 2125, Level = LogLevel.Information, Message = "Sync root {RootId} plan: {UploadCount} uploads, {FolderCount} folders, {ConfirmationCount} confirmations, {DeleteCount} local deletes, {BlockedCount} blocked, {NoOpCount} unchanged.")]
        public static partial void PlanCreated(
            ILogger logger,
            Guid rootId,
            int uploadCount,
            int folderCount,
            int confirmationCount,
            int deleteCount,
            int blockedCount,
            int noOpCount);

        [LoggerMessage(EventId = 2130, Level = LogLevel.Information, Message = "Sync root {RootId} upload {UploadNumber} started with {SizeBytes} bytes.")]
        public static partial void UploadStarted(
            ILogger logger,
            Guid rootId,
            int uploadNumber,
            long sizeBytes);

        [LoggerMessage(EventId = 2131, Level = LogLevel.Information, Message = "Sync root {RootId} upload {UploadNumber} completed.")]
        public static partial void UploadCompleted(ILogger logger, Guid rootId, int uploadNumber);

        [LoggerMessage(EventId = 2132, Level = LogLevel.Information, Message = "Sync root {RootId} execution completed: {UploadedCount} uploaded, {ConfirmedCount} confirmed, {FolderCount} folders, {DeletedCount} local deletes, {SkippedCount} skipped, {BlockedCount} blocked.")]
        public static partial void ExecutionCompleted(
            ILogger logger,
            Guid rootId,
            int uploadedCount,
            int confirmedCount,
            int folderCount,
            int deletedCount,
            int skippedCount,
            int blockedCount);
    }
}
