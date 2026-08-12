// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Resources.Localization;
using Cotton.Mobile.Services;
using Cotton.Mobile.ViewModels;
using Cotton.Sdk.Auth;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Networking;
using Microsoft.Maui.Storage;
using UraniumUI;
using UraniumUI.Icons.MaterialSymbols;

namespace Cotton.Mobile
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            MauiAppBuilder builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseUraniumUI()
                .UseUraniumUIMaterial()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddMaterialSymbolsFonts();
                });

            RegisterPlatformServices(builder.Services);
            RegisterApplicationServices(builder.Services);
            RegisterSyncServices(builder.Services);
            RegisterPresentation(builder.Services);

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }

        private static void RegisterPlatformServices(IServiceCollection services)
        {
            services.AddSingleton<ISecureStorage>(SecureStorage.Default);
            services.AddSingleton<IPreferences>(Preferences.Default);
            services.AddSingleton<IBrowser>(Browser.Default);
            services.AddSingleton<IConnectivity>(Connectivity.Current);
            services.AddSingleton(_ => new HttpClient());

#if ANDROID
            services.AddSingleton<IAndroidDocumentTreeActivityResultBridge, AndroidDocumentTreeActivityResultBridge>();
            services.AddSingleton<ICottonSyncLocalRootPickerService, AndroidDocumentTreeSyncLocalRootPickerService>();
            services.AddSingleton<ICottonSyncLocalRootPermissionResolver, AndroidDocumentTreeSyncLocalRootPermissionResolver>();
            services.AddSingleton<ICottonDeviceToCloudLocalTreeReader, AndroidDocumentTreeDeviceToCloudLocalTreeReader>();
            services.AddSingleton<ICottonDeviceToCloudLocalFileContentSource, AndroidDocumentTreeDeviceToCloudLocalFileContentSource>();
            services.AddSingleton<ICottonDeviceToCloudLocalFileOperator, AndroidDocumentTreeDeviceToCloudLocalFileOperator>();
            services.AddSingleton<ICottonUserSelectedDocumentTreeCloudToDeviceSyncFileOperator, AndroidDocumentTreeCloudToDeviceSyncFileOperator>();
            services.AddSingleton<AndroidNotificationChannelService>();
            services.AddSingleton<ICottonNotificationPermissionService, AndroidNotificationPermissionService>();
            services.AddSingleton<ICottonLocalNotificationService, AndroidLocalNotificationService>();
            services.AddSingleton<ICottonNotificationBackgroundScheduler, AndroidNotificationBackgroundScheduler>();
#else
            services.AddSingleton<ICottonSyncLocalRootPickerService, DisabledCottonSyncLocalRootPickerService>();
            services.AddSingleton<ICottonSyncLocalRootPermissionResolver, StoredCottonSyncLocalRootPermissionResolver>();
            services.AddSingleton<ICottonDeviceToCloudLocalTreeReader, DisabledCottonDeviceToCloudLocalTreeReader>();
            services.AddSingleton<ICottonDeviceToCloudLocalFileContentSource, DisabledCottonDeviceToCloudLocalFileContentSource>();
            services.AddSingleton<ICottonDeviceToCloudLocalFileOperator, DisabledCottonDeviceToCloudLocalFileOperator>();
            services.AddSingleton<ICottonUserSelectedDocumentTreeCloudToDeviceSyncFileOperator, DisabledUserSelectedDocumentTreeCloudToDeviceSyncFileOperator>();
#endif
        }

        private static void RegisterApplicationServices(IServiceCollection services)
        {
            services.AddSingleton(
                new CottonMobileOptions(
                    AppResources.AppTitle,
                    new Uri("https://app.cottoncloud.dev"),
                    new Uri("https://cottoncloud.dev/privacy-policy"),
                    "cotton-play-market-support@belov.us"));
            services.AddSingleton<IApplicationForegroundService, ApplicationForegroundService>();
            services.AddSingleton<ICottonMobileApplicationMetadata, CottonMobileApplicationMetadata>();
            services.AddSingleton<IUserDialogService, UserDialogService>();
            services.AddSingleton<INetworkAccessService, NetworkAccessService>();

            services.AddSingleton<ICottonTokenStore, SecureStorageCottonTokenStore>();
            services.AddSingleton<ICottonPendingAppCodeSessionStore, SecureStorageCottonPendingAppCodeSessionStore>();
            services.AddSingleton<ICottonInstanceStore, PreferencesCottonInstanceStore>();
            services.AddSingleton<ICottonProfileCacheStore, PreferencesCottonProfileCacheStore>();
            services.AddSingleton<ICottonNotificationCursorStore, PreferencesCottonNotificationCursorStore>();
            services.AddSingleton<ICottonClientFactory, CottonClientFactory>();
            services.AddSingleton<ICottonAppCodeAuthorizationService, CottonAppCodeAuthorizationService>();
            services.AddSingleton<ICottonSessionService, CottonSessionService>();
            services.AddSingleton(new CottonNotificationDeliveryPlanner(3));
            services.AddSingleton<ICottonNotificationPollingService, CottonNotificationPollingService>();
            services.AddSingleton<ICottonNotificationRealtimeService, CottonNotificationRealtimeService>();
            services.AddSingleton<ICottonNotificationSessionService, CottonNotificationSessionService>();
        }

        private static void RegisterSyncServices(IServiceCollection services)
        {
            services.AddSingleton<ICottonSyncRootMetadataPathProvider, CottonSyncRootMetadataPathProvider>();
            services.AddSingleton<ICottonSyncRootStore, FileSystemCottonSyncRootStore>();
            services.AddSingleton<ICottonSyncRootPauseStore, FileSystemCottonSyncRootPauseStore>();
            services.AddSingleton<ICottonSyncedFileManifestPathProvider, CottonSyncedFileManifestPathProvider>();
            services.AddSingleton<ICottonSyncedFileManifestStore, FileSystemCottonSyncedFileManifestStore>();
            services.AddSingleton<ICottonUploadReceiptPathProvider, CottonUploadReceiptPathProvider>();
            services.AddSingleton<ICottonUploadReceiptStore, FileSystemCottonUploadReceiptStore>();
            services.AddSingleton<SyncRootManager>();
            services.AddSingleton<CottonSyncRootConfigurationService>();
            services.AddSingleton<CottonSyncRootReconnectService>();

            services.AddSingleton(FileDownloadCacheOptions.Default);
            services.AddSingleton<ICottonOfflineFileMetadataPathProvider, CottonOfflineFileMetadataPathProvider>();
            services.AddSingleton<ICottonOfflineFilePinStore, FileSystemCottonOfflineFilePinStore>();
            services.AddSingleton<IFileDownloadCachePruner, FileDownloadCachePruner>();
            services.AddSingleton<ICottonLocalDownloadCache, CottonLocalDownloadCache>();
            services.AddSingleton<ICottonFileDownloadService, CottonFileDownloadService>();
            services.AddSingleton<ICottonFileBrowserService, CottonFileBrowserService>();
            services.AddSingleton<ICottonFileUploadService, CottonFileUploadService>();
            services.AddSingleton<ICloudFolderPickerService, CloudFolderPickerService>();
            services.AddSingleton<ISyncRootSetupOptionsPickerService, SyncRootSetupOptionsPickerService>();
            services.AddSingleton<SyncRootSetupCoordinator>();
            services.AddSingleton<ICottonCloudToDeviceSyncFolderContentSource, CottonFileBrowserCloudToDeviceSyncFolderContentSource>();
            services.AddSingleton<ICottonDeviceToCloudRemoteFolderContentSource, CottonFileBrowserCloudToDeviceSyncFolderContentSource>();
            services.AddSingleton<CottonRecursiveRemoteContentLoader>();

            services.AddSingleton<CottonAppPrivateCloudToDeviceSyncFileOperator>();
            services.AddSingleton<ICottonCloudToDeviceSyncFileOperator, CottonCloudToDeviceSyncFileOperatorRouter>();
            services.AddSingleton<ICottonDeviceToCloudSyncFileOperator, CottonDeviceToCloudSyncFileOperator>();
            services.AddSingleton(serviceProvider =>
                new CottonCloudToDeviceSyncPlanExecutor(
                    serviceProvider.GetRequiredService<ICottonCloudToDeviceSyncFileOperator>(),
                    serviceProvider.GetRequiredService<ICottonSyncedFileManifestStore>()));
            services.AddSingleton(serviceProvider =>
                new CottonDeviceToCloudSyncPlanExecutor(
                    serviceProvider.GetRequiredService<ICottonDeviceToCloudSyncFileOperator>(),
                    serviceProvider.GetRequiredService<ICottonSyncedFileManifestStore>()));
            services.AddSingleton(serviceProvider =>
                new CottonUploadOnlySyncPlanExecutor(
                    serviceProvider.GetRequiredService<ICottonDeviceToCloudSyncFileOperator>(),
                    serviceProvider.GetRequiredService<ICottonDeviceToCloudLocalFileOperator>(),
                    serviceProvider.GetRequiredService<ICottonUploadReceiptStore>()));
            services.AddSingleton<CottonCloudToDeviceSyncCoordinator>();
            services.AddSingleton<CottonDeviceToCloudSyncCoordinator>();
            services.AddSingleton<CottonBidirectionalSyncCoordinator>();
            services.AddSingleton<SyncExecutionWorkflow>();
        }

        private static void RegisterPresentation(IServiceCollection services)
        {
            services.AddSingleton<IMainPagePresentationService, MainPagePresentationService>();
            services.AddSingleton<MainPageSessionCoordinator>();
            services.AddSingleton<MainPageUserInteractionService>();
            services.AddSingleton<SyncSettingsRootProvider>();
            services.AddSingleton<SyncSettingsLoadingHandler>();
            services.AddSingleton<SyncSettingsExecutionHandler>();
            services.AddSingleton<SyncSettingsSetupHandler>();
            services.AddSingleton<SyncSettingsManagementHandler>();
            services.AddSingleton<SyncSettingsViewModel>();
            services.AddSingleton<MainPageViewModel>();
            services.AddSingleton<MainPage>();
            services.AddSingleton<AppShell>();
            services.AddSingleton<Func<AppShell>>(serviceProvider =>
                () => serviceProvider.GetRequiredService<AppShell>());
        }
    }
}
