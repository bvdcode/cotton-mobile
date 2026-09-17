// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.Services
{
    public class CottonMediaOriginalRestoreService(
        ICottonDeviceToCloudLocalTreeReader localReader,
        CottonRecursiveRemoteContentLoader remoteLoader,
        ICottonRedactedMediaHashSource redactedHashSource,
        ICottonMediaOriginalRestoreStore restoreStore,
        CottonSyncRootExecutionLock executionLock,
        ILogger<CottonMediaOriginalRestoreService> logger)
    {
        public bool IsSupported => redactedHashSource.IsSupported;

        public bool CanReadOriginals => redactedHashSource.CanReadOriginals;

        public async Task<bool> IsReviewNeededAsync(CottonSyncRootSnapshot root, CancellationToken cancellationToken = default)
        {
            CottonMediaOriginalRestoreState state = await restoreStore.LoadAsync(root, cancellationToken).ConfigureAwait(false);
            return !state.ReviewCompleted;
        }

        public async Task<bool> CompleteReviewAsync(
            CottonMediaOriginalRestorePreview preview,
            bool restore,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(preview);
            CottonMediaOriginalRestoreState state = await restoreStore.UpdateAsync(preview.Root, previous =>
            {
                List<CottonMediaOriginalRestoreApproval> approvals = [.. previous.Approvals];
                if (restore)
                {
                    EnsureAvailable(preview.Root);
                    foreach (CottonMediaOriginalRestoreItem item in preview.Items)
                    {
                        if (approvals.Any(approval => approval.LocalFile.LocalSourceId == item.LocalFile.LocalSourceId
                            && approval.LocalFile.ContentHash == item.LocalFile.ContentHash
                            && approval.FileId == item.RemoteFile.Id && approval.ExpectedETag == item.RemoteFile.ETag))
                        {
                            continue;
                        }

                        approvals.RemoveAll(approval => approval.LocalFile.LocalSourceId == item.LocalFile.LocalSourceId);
                        approvals.Add(new CottonMediaOriginalRestoreApproval(item.LocalFile,
                            item.RemoteFile.Id, item.RemoteFile.ETag!, item.RemoteFile.ContentHash!, Guid.NewGuid()));
                    }
                }

                return new CottonMediaOriginalRestoreState(preview.Root.StableKey, true, approvals);
            }, cancellationToken).ConfigureAwait(false);
            return state.Approvals.Count > 0;
        }

        public Task<CottonMediaOriginalRestorePreview> ScanAsync(
            CottonSyncRootSnapshot root,
            IProgress<string>? progress = null,
            CancellationToken cancellationToken = default)
        {
            EnsureAvailable(root);
            return executionLock.ExecuteAsync(root, token => ScanCoreAsync(root, progress, token), cancellationToken);
        }

        private async Task<CottonMediaOriginalRestorePreview> ScanCoreAsync(
            CottonSyncRootSnapshot root,
            IProgress<string>? progress,
            CancellationToken cancellationToken)
        {
            CottonDeviceToCloudLocalContentSnapshot local = await localReader
                .ReadAsync(root.InstanceUri, root, cancellationToken).ConfigureAwait(false);
            CottonDeviceToCloudRemoteContentSnapshot remote = await remoteLoader
                .LoadAsync(root.InstanceUri, root, cancellationToken).ConfigureAwait(false);
            Dictionary<string, CottonDeviceToCloudRemoteItemSnapshot> remoteByPath = remote.Items
                .ToDictionary(item => item.RelativePath, StringComparer.OrdinalIgnoreCase);
            List<CottonMediaOriginalRestoreItem> verified = [];
            int unchanged = 0;
            int unverified = 0;
            foreach (CottonDeviceToCloudLocalItemSnapshot file in local.Items
                .Where(IsMediaFile).OrderBy(item => item.RelativePath, StringComparer.OrdinalIgnoreCase))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!remoteByPath.TryGetValue(file.RelativePath, out CottonDeviceToCloudRemoteItemSnapshot? cloud))
                {
                    continue;
                }

                if (cloud.Entry.Type == CottonFileBrowserEntryType.File
                    && string.Equals(file.ContentHash, cloud.Entry.ContentHash, StringComparison.Ordinal)
                    && file.SizeBytes == cloud.Entry.SizeBytes)
                {
                    unchanged++;
                    continue;
                }

                progress?.Report(file.RelativePath);
                string? redactedHash = null;
                if (cloud.Entry.Type == CottonFileBrowserEntryType.File
                    && file.SizeBytes.HasValue && cloud.Entry.SizeBytes == file.SizeBytes
                    && !string.IsNullOrWhiteSpace(cloud.Entry.ETag)
                    && cloud.Entry.ContentHash is not null
                    && file.LocalSourceId is not null)
                {
                    redactedHash = await redactedHashSource.ComputeAsync(root, file, cancellationToken)
                        .ConfigureAwait(false);
                }

                if (redactedHash is null || !string.Equals(redactedHash, cloud.Entry.ContentHash, StringComparison.Ordinal))
                {
                    unverified++;
                    continue;
                }

                verified.Add(new CottonMediaOriginalRestoreItem(file, cloud.Entry));
            }

            EnsureAvailable(root);
            CottonMediaRestoreLog.ScanCompleted(logger, root.Id, verified.Count, unchanged, unverified);
            return new CottonMediaOriginalRestorePreview(root, verified, unchanged, unverified);
        }

        private static bool IsMediaFile(CottonDeviceToCloudLocalItemSnapshot file)
        {
            return file.ItemType == CottonFileBrowserEntryType.File && file.ContentType is not null
                && (file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
                    || file.ContentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase));
        }

        private void EnsureAvailable(CottonSyncRootSnapshot root)
        {
            ArgumentNullException.ThrowIfNull(root);
            if (!IsSupported || !CanReadOriginals || !CottonDeviceToCloudSyncRootCapability.CanRun(root))
            {
                throw new InvalidOperationException("Original media access and a runnable upload folder are required.");
            }
        }
    }
}
