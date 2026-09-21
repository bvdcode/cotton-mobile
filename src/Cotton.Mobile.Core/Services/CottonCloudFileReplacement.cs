// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.Services
{
    public class CottonCloudFileReplacement(
        ICottonFileUploadService uploadService,
        ICottonDeviceToCloudRemoteFolderContentSource remoteSource,
        CottonSyncFileUploadSourceFactory sourceFactory,
        ICottonRestoredUploadReceiptStore receiptStore,
        TimeProvider timeProvider,
        ILogger<CottonCloudFileReplacement> logger)
    {
        public async Task<CottonFileBrowserEntry> ExecuteAsync(
            CottonSyncRootSnapshot root,
            CottonDeviceToCloudLocalItemSnapshot local,
            CottonFileBrowserEntry remote,
            CottonFolderHandle parent,
            string expectedETag,
            Guid operationId,
            IProgress<long> progress,
            CancellationToken cancellationToken)
        {
            CottonFileBrowserEntry result = remote;
            if (!Matches(local, remote.Id, result))
            {
                CottonDeviceToCloudSyncPlanItem upload = new(
                    CottonDeviceToCloudSyncActionKind.UploadNewFile,
                    CottonFileBrowserEntryType.File, local.DisplayName, local.RelativePath,
                    remote.Id, expectedETag, local.LocalUpdatedAtUtc, local.SizeBytes,
                    local.ContentType, local.LocalSourceId, operationId, local.ContentHash);
                CottonFileUploadSource source = sourceFactory.Create(root.InstanceUri, root, upload);
                try
                {
                    result = await uploadService.UpdateContentAsync(root.InstanceUri, remote.Id, parent,
                        expectedETag, source, progress, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception exception) when (exception is not OperationCanceledException
                    && !cancellationToken.IsCancellationRequested)
                {
                    CottonSyncDiagnosticLog.ReplacementFailed(logger, exception);
                    CottonFolderContent refreshed = await remoteSource.LoadAsync(
                        root.InstanceUri, parent, cancellationToken).ConfigureAwait(false);
                    CottonFileBrowserEntry? confirmed = refreshed.Entries.SingleOrDefault(
                        item => Matches(local, remote.Id, item));
                    if (confirmed is null)
                    {
                        throw;
                    }

                    CottonSyncDiagnosticLog.ConflictResolutionResponseRecovered(logger, root.Id, confirmed.Id);
                    result = confirmed;
                }
            }

            if (!Matches(local, remote.Id, result))
            {
                throw new InvalidDataException("Cloud replacement returned a different file revision.");
            }

            CottonUploadReceiptSnapshot receipt = new(
                local.LocalSourceId!, local.RelativePath, local.LocalUpdatedAtUtc, local.SizeBytes,
                local.ContentType, operationId, CottonUploadReceiptStatus.Uploaded,
                timeProvider.GetUtcNow().UtcDateTime, result.Id, result.ETag, local.ContentHash);
            await receiptStore.SaveRestoredAsync(root, receipt, cancellationToken).ConfigureAwait(false);
            return result;
        }

        private static bool Matches(CottonDeviceToCloudLocalItemSnapshot local, Guid fileId, CottonFileBrowserEntry remote)
        {
            return remote.Id == fileId && remote.Type == CottonFileBrowserEntryType.File
                && remote.Name == local.DisplayName && remote.SizeBytes == local.SizeBytes
                && local.ContentHash is not null && remote.ContentHash == local.ContentHash
                && !string.IsNullOrWhiteSpace(remote.ETag);
        }
    }
}
