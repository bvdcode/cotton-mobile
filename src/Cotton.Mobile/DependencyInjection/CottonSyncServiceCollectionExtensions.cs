// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.DependencyInjection
{
    public static class CottonSyncServiceCollectionExtensions
    {
        public static IServiceCollection AddCottonSyncServices(this IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);
            AddSyncPersistence(services);
            AddFileServices(services);
            AddSyncExecution(services);
            return services;
        }

        private static void AddSyncPersistence(IServiceCollection services)
        {
            services.AddSingleton<ICottonSyncRootMetadataPathProvider, CottonSyncRootMetadataPathProvider>();
            services.AddSingleton<ICottonSyncRootStore, FileSystemCottonSyncRootStore>();
            services.AddSingleton<ICottonSyncRootPauseStore, FileSystemCottonSyncRootPauseStore>();
            services.AddSingleton<ICottonAutomaticSyncStatusStore, FileSystemCottonAutomaticSyncStatusStore>();
            services.AddSingleton<ICottonContentRevisionPathProvider, CottonContentRevisionPathProvider>();
            services.AddSingleton<ICottonContentRevisionStore, FileSystemCottonContentRevisionStore>();
            services.AddSingleton<ICottonSyncedFileManifestPathProvider, CottonSyncedFileManifestPathProvider>();
            services.AddSingleton<ICottonSyncedFileManifestStore, FileSystemCottonSyncedFileManifestStore>();
            services.AddSingleton<ICottonUploadReceiptPathProvider, CottonUploadReceiptPathProvider>();
            services.AddSingleton<ICottonUploadReceiptStore, FileSystemCottonUploadReceiptStore>();
            services.AddSingleton<CottonSyncProgressHub>();
            services.AddSingleton<SyncRootManager>();
            services.AddSingleton<CottonSyncRootConfigurationService>();
            services.AddSingleton<CottonSyncRootReconnectService>();
        }

        private static void AddFileServices(IServiceCollection services)
        {
            services.AddSingleton(FileDownloadCacheOptions.Default);
            services.AddSingleton<ICottonOfflineFileMetadataPathProvider, CottonOfflineFileMetadataPathProvider>();
            services.AddSingleton<ICottonOfflineFilePinStore, FileSystemCottonOfflineFilePinStore>();
            services.AddSingleton<FileDownloadCacheProtectionProvider>();
            services.AddSingleton<FileDownloadCacheFilePruner>();
            services.AddSingleton<IFileDownloadCachePruner, FileDownloadCachePruner>();
            services.AddSingleton<CottonLocalDownloadFileStore>();
            services.AddSingleton<ICottonLocalDownloadCache, CottonLocalDownloadCache>();
            services.AddSingleton<ICottonFileDownloadService, CottonFileDownloadService>();
            services.AddSingleton<ICottonFileBrowserService, CottonFileBrowserService>();
            services.AddSingleton<ICottonFileUploadService, CottonFileUploadService>();
            services.AddSingleton<ICloudFolderPickerService, CloudFolderPickerService>();
            services.AddSingleton<ICottonMediaAlbumPickerService, CottonMediaAlbumPickerService>();
            services.AddSingleton<ISyncRootSetupOptionsPickerService, SyncRootSetupOptionsPickerService>();
            services.AddSingleton<ICottonSyncRootSetupDraftStore, PreferencesCottonSyncRootSetupDraftStore>();
            services.AddSingleton<SyncRootSetupCoordinator>();
            services.AddSingleton<ICottonDeviceToCloudRemoteFolderContentSource, CottonFileBrowserRemoteFolderContentSource>();
            services.AddSingleton<CottonRecursiveRemoteContentLoader>();
        }

        private static void AddSyncExecution(IServiceCollection services)
        {
            services.AddSingleton<ICottonDeviceToCloudSyncFileOperator, CottonDeviceToCloudSyncFileOperator>();
            services.AddSingleton(serviceProvider =>
                new CottonUploadOnlySyncPlanExecutor(
                    serviceProvider.GetRequiredService<ICottonDeviceToCloudSyncFileOperator>(),
                    serviceProvider.GetRequiredService<ICottonDeviceToCloudLocalFileOperator>(),
                    serviceProvider.GetRequiredService<ICottonUploadReceiptStore>(),
                    serviceProvider.GetRequiredService<CottonSyncProgressHub>(),
                    serviceProvider.GetRequiredService<ILogger<CottonUploadOnlySyncPlanExecutor>>()));
            services.AddSingleton<CottonSyncRootExecutionLock>();
            services.AddSingleton<ICottonDeviceToCloudSyncCoordinator, CottonDeviceToCloudSyncCoordinator>();
            services.AddSingleton<ICottonAutomaticSyncRunner, CottonAutomaticSyncRunner>();
            services.AddSingleton<CottonAutomaticSyncDispatcher>();
            services.AddSingleton<SyncExecutionWorkflow>();
        }
    }
}
