// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID && DEBUG
using Android.Content;
using Cotton.Mobile.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Cotton.Mobile.Platforms.Android
{
    public static class AndroidMediaAccessChecks
    {
        private const string TreePreference = "MediaAccessChecks.Tree";
        private const string LogTag = "CottonMediaAccessTests";
        private static readonly Uri InstanceUri = new("https://media-test.invalid");

        public static async Task PickFolderAsync(IServiceProvider services)
        {
            try
            {
                ICottonSyncLocalRootPickerService picker = services
                    .GetRequiredService<ICottonSyncLocalRootPickerService>();
                Guid requestId = Guid.NewGuid();
                CottonSyncLocalRootSnapshot? local = await picker.PickAsync(
                    CottonSyncRootStorageKind.UserSelectedDocumentTree, requestId);
                picker.CompletePick(requestId);
                if (local is null)
                {
                    throw new InvalidOperationException("A test folder must be selected.");
                }

                Preferences.Default.Set(TreePreference, local.RootKey);
                _ = global::Android.Util.Log.Info(LogTag, "folder:passed");
            }
            catch (Exception exception)
            {
                _ = global::Android.Util.Log.Error(LogTag, $"folder:failed:{exception}");
            }
        }

        public static async Task ReadAsync(IServiceProvider services, Intent intent)
        {
            string fileName = intent.GetStringExtra("file-name")
                ?? throw new InvalidDataException("Test file name is required.");
            string expectedHash = intent.GetStringExtra("expected-hash")
                ?? throw new InvalidDataException("Expected content hash is required.");
            string expectedMediaHash = intent.GetStringExtra("expected-media-hash")
                ?? throw new InvalidDataException("Expected media hash is required.");
            string treeUri = Preferences.Default.Get(TreePreference, string.Empty);
            CottonSyncRootSnapshot folderRoot = CreateRoot(
                new CottonSyncLocalRootSnapshot(
                    CottonSyncRootStorageKind.UserSelectedDocumentTree,
                    treeUri, "Media sample", CottonSyncRootPermissionStatus.Available),
                CottonUploadOriginalRetention.DeleteAfterConfirmedUpload,
                Guid.Parse("b0000000-0000-0000-0000-000000000001"));
            AndroidDocumentTreeDeviceToCloudLocalTreeReader folderReader = services
                .GetRequiredService<AndroidDocumentTreeDeviceToCloudLocalTreeReader>();
            CottonDeviceToCloudLocalContentSnapshot folderContent = await folderReader.ReadAsync(
                InstanceUri, folderRoot);
            string folderResult = await CheckContentAsync(services, folderRoot, folderContent, fileName, expectedHash);
            CottonDeviceToCloudLocalContentSnapshot cachedFolderContent = await folderReader.ReadAsync(
                InstanceUri, folderRoot);
            _ = await CheckContentAsync(services, folderRoot, cachedFolderContent, fileName, expectedHash);

            long bucketId = intent.GetLongExtra("bucket-id", 0);
            string mediaResult = "MediaStore: no library permission";
            if (AndroidMediaReadAccessResolver.Resolve().HasAccess)
            {
                CottonSyncRootSnapshot mediaRoot = CreateRoot(
                    new CottonSyncLocalRootSnapshot(
                        CottonSyncRootStorageKind.MediaStore,
                        "content://media/external/file", "Media sample",
                        CottonSyncRootPermissionStatus.Available,
                        AndroidMediaStoreScopeKey.Create([bucketId])),
                    CottonUploadOriginalRetention.KeepOriginals,
                    Guid.Parse("b0000000-0000-0000-0000-000000000002"));
                AndroidMediaStoreDeviceToCloudLocalTreeReader mediaReader = services
                    .GetRequiredService<AndroidMediaStoreDeviceToCloudLocalTreeReader>();
                AndroidMediaStoreScanResult mediaScan = await mediaReader.ReadWithDiagnosticsAsync(
                    InstanceUri, mediaRoot);
                mediaResult = await CheckContentAsync(services, mediaRoot, mediaScan.Content, fileName, expectedMediaHash);
                AndroidMediaStoreScanResult cachedScan = await mediaReader.ReadWithDiagnosticsAsync(
                    InstanceUri, mediaRoot);
                _ = await CheckContentAsync(services, mediaRoot, cachedScan.Content, fileName, expectedMediaHash);
                if (cachedScan.Statistics.HashedFileCount != 0 || cachedScan.Statistics.ReusedHashCount == 0)
                {
                    throw new InvalidOperationException("Unchanged media hashes were not reused.");
                }

                mediaResult += $"; hashed={mediaScan.Statistics.HashedFileCount}; reused={mediaScan.Statistics.ReusedHashCount}";
            }

            string result = $"LocationPermission={AndroidMediaContentAccess.HasLocationPermission}\n{folderResult}\n{mediaResult}\n";
            await File.WriteAllTextAsync(Path.Combine(FileSystem.AppDataDirectory, "media-access-check.txt"), result);
            _ = global::Android.Util.Log.Info(LogTag, result);
        }

        private static async Task<string> CheckContentAsync(
            IServiceProvider services,
            CottonSyncRootSnapshot root,
            CottonDeviceToCloudLocalContentSnapshot content,
            string fileName,
            string expectedHash)
        {
            CottonDeviceToCloudLocalItemSnapshot file = content.Items.Single(item => item.DisplayName == fileName);
            if (!string.Equals(file.ContentHash, expectedHash, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Unexpected scanned hash for {root.LocalRoot.StorageKind}: {file.ContentHash}.");
            }

            CottonDeviceToCloudSyncPlanItem upload = new(
                CottonDeviceToCloudSyncActionKind.UploadNewFile, CottonFileBrowserEntryType.File,
                file.DisplayName, file.RelativePath, null, null, file.LocalUpdatedAtUtc,
                file.SizeBytes, file.ContentType, file.LocalSourceId, contentHash: file.ContentHash);
            await using Stream stream = await services.GetRequiredService<ICottonDeviceToCloudLocalFileContentSource>()
                .OpenReadAsync(InstanceUri, root, upload);
            string uploadedHash = await CottonContentHash.ComputeSha256Async(stream, CancellationToken.None);
            if (!string.Equals(uploadedHash, expectedHash, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Upload stream differs from the scanned content.");
            }

            if (AndroidMediaContentAccess.HasLocationPermission && OperatingSystem.IsAndroidVersionAtLeast(31))
            {
                string? redactedHash = await services.GetRequiredService<ICottonRedactedMediaHashSource>()
                    .ComputeAsync(root, file);
                _ = global::Android.Util.Log.Info(LogTag, $"Redacted:{root.LocalRoot.StorageKind}:{fileName}:{redactedHash}");
            }

            if (root.DeletesOriginalsAfterUpload && !AndroidMediaContentAccess.HasLocationPermission)
            {
                CottonDeviceToCloudLocalFileDeleteStatus deletion = await services
                    .GetRequiredService<ICottonDeviceToCloudLocalFileOperator>()
                    .DeleteIfUnchangedAsync(InstanceUri, root, upload);
                if (deletion != CottonDeviceToCloudLocalFileDeleteStatus.Unsupported)
                {
                    throw new InvalidOperationException($"Original media was not protected: {deletion}.");
                }
            }

            CottonContentRevisionIndexSnapshot index = await services.GetRequiredService<ICottonContentRevisionStore>()
                .LoadAsync(InstanceUri, root)
                ?? throw new InvalidOperationException("Content revision index is unavailable.");
            return $"{root.LocalRoot.StorageKind}: {uploadedHash}; source={index.SourceVersion}";
        }

        private static CottonSyncRootSnapshot CreateRoot(
            CottonSyncLocalRootSnapshot local,
            CottonUploadOriginalRetention retention,
            Guid rootId)
        {
            return new CottonSyncRootSnapshot(rootId, InstanceUri, "media-tests",
                new CottonUploadDestinationSnapshot(
                    Guid.Parse("b0000000-0000-0000-0000-000000000003"), "Media sample", "/Media sample"),
                local, CottonSyncDirection.DeviceToCloud, retention);
        }
    }
}
#endif
