// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using System.Globalization;

namespace Cotton.Mobile.Services
{
    public class CottonSyncFileUploadSourceFactory(ICottonDeviceToCloudLocalFileContentSource localContentSource)
    {
        private const string MetadataSourceValue = "device-to-cloud-sync";

        public CottonFileUploadSource Create(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonDeviceToCloudSyncPlanItem item)
        {
            ArgumentNullException.ThrowIfNull(item);
            Dictionary<string, string> metadata = new(StringComparer.Ordinal)
            {
                [CottonFileUploadMetadataKeys.Source] = MetadataSourceValue
            };
            if (item.LocalUpdatedAtUtc.HasValue)
            {
                metadata[CottonFileUploadMetadataKeys.OriginalLastModified] =
                    item.LocalUpdatedAtUtc.Value.ToString("O", CultureInfo.InvariantCulture);
            }

            if (item.UploadOperationId.HasValue)
            {
                metadata[CottonFileUploadMetadataKeys.UploadOperationId] = item.UploadOperationId.Value.ToString("N");
            }

            return new CottonFileUploadSource(
                new CottonFileUploadSourceSnapshot(
                    item.DisplayName, item.ContentType, item.SizeBytes, metadata, item.ContentHash),
                token => localContentSource.OpenReadAsync(instanceUri, root, item, token));
        }
    }
}
