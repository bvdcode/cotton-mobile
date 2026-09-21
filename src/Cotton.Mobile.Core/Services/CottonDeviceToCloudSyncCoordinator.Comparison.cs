// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public partial class CottonDeviceToCloudSyncCoordinator
    {
        private async Task<CottonDeviceToCloudSyncRootRunResult?> TryReuseComparisonAsync(
            CottonSyncRootSnapshot root,
            CottonDeviceToCloudLocalContentSnapshot local,
            CancellationToken cancellationToken)
        {
            if (root.DeletesOriginalsAfterUpload || local.Problems.Count > 0)
            {
                return null;
            }

            CottonSyncReviewState state = await _reviewStore.LoadAsync(root, cancellationToken).ConfigureAwait(false);
            if (state.BaselineFingerprint is null || state.Approvals.Count > 0
                || state.BaselineFingerprint != CottonSyncLocalFingerprint.Create(root, local)
                || await _originalRestore.HasPendingAsync(root, cancellationToken).ConfigureAwait(false))
            {
                return null;
            }

            CottonDeviceToCloudSyncPlanSnapshot plan = new(root.Id, root.CloudFolder.FolderId,
                root.CloudFolder.FolderName, [.. state.Conflicts.Select(item => item.ToPlanItem())]);
            CottonDeviceToCloudSyncExecutionResult result = new(0, 0, 0, 0, state.UnchangedCount, state.Conflicts.Count);
            CottonSyncDiagnosticLog.ComparisonReused(_logger, root.Id, state.UnchangedCount, state.Conflicts.Count);
            return CottonDeviceToCloudSyncRootRunResult.Completed(root, plan, result);
        }

        private Task SaveComparisonAsync(
            CottonSyncRootSnapshot root,
            CottonDeviceToCloudLocalContentSnapshot local,
            CottonDeviceToCloudSyncPlanSnapshot plan,
            CottonDeviceToCloudSyncExecutionResult result,
            CancellationToken cancellationToken)
        {
            if (root.DeletesOriginalsAfterUpload || local.Problems.Count > 0)
            {
                return Task.CompletedTask;
            }

            return _reviewStore.UpdateAsync(root, state =>
            {
                bool canReuse = state.Approvals.Count == 0 && state.Conflicts.Count == plan.BlockedCount
                    && !plan.Items.Any(item => item.IsBlocked && item.Action != CottonDeviceToCloudSyncActionKind.RemotePathConflict)
                    && !plan.HasExecutableChanges && !result.HasAppliedChanges;
                CottonSyncVerifiedFile[] verified = [.. plan.Items.Where(item =>
                        item.TargetType == CottonFileBrowserEntryType.File
                        && item.LocalSourceId is not null && item.ContentHash is not null
                        && (item.IsNoOp || item.RequiresUpload || item.ConfirmsPendingUpload))
                    .Select(item => new CottonSyncVerifiedFile(item.LocalSourceId!,
                        item.RelativePath, item.SizeBytes, item.ContentHash!))];
                return new CottonSyncReviewState(root.StableKey,
                    canReuse ? CottonSyncLocalFingerprint.Create(root, local) : null,
                    result.SkippedCount, state.Conflicts, state.Approvals, verified);
            }, cancellationToken);
        }

        private async Task<HashSet<string>> GetVerifiedSourcesAsync(
            CottonSyncRootSnapshot root,
            CottonDeviceToCloudLocalContentSnapshot local,
            IReadOnlyList<CottonUploadReceiptSnapshot> receipts,
            CancellationToken cancellationToken)
        {
            if (root.DeletesOriginalsAfterUpload || receipts.Any(item => item.IsPending)
                || await _originalRestore.HasPendingAsync(root, cancellationToken).ConfigureAwait(false))
            {
                return [];
            }

            CottonSyncReviewState state = await _reviewStore.LoadAsync(root, cancellationToken).ConfigureAwait(false);
            Dictionary<string, CottonSyncVerifiedFile> previous = state.VerifiedFiles
                .ToDictionary(item => item.LocalSourceId, StringComparer.Ordinal);
            HashSet<string> pending = receipts.Where(item => item.IsPending)
                .Select(item => item.LocalSourceId).ToHashSet(StringComparer.Ordinal);
            pending.UnionWith(state.Approvals.Select(item => item.Conflict.LocalFile.LocalSourceId!));
            return local.Items.Where(item => item.LocalSourceId is not null
                    && !pending.Contains(item.LocalSourceId)
                    && previous.TryGetValue(item.LocalSourceId, out CottonSyncVerifiedFile? verified)
                    && verified.Matches(item))
                .Select(item => item.LocalSourceId!).ToHashSet(StringComparer.Ordinal);
        }
    }
}
