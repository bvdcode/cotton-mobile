using CommunityToolkit.Maui;
using Cotton.Mobile.Services;
using Cotton.Mobile.ViewModels;
using Cotton.Sdk.Auth;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Media;
using Microsoft.Maui.Networking;
using Microsoft.Maui.Storage;

namespace Cotton.Mobile
{
	public static class MauiProgram
	{
		public static MauiApp CreateMauiApp()
		{
			var builder = MauiApp.CreateBuilder();
			builder
				.UseMauiApp<App>()
				.UseMauiCommunityToolkitMediaElement(isAndroidForegroundServiceEnabled: false)
				.ConfigureFonts(fonts =>
				{
					fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
					fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
				});

			builder.Services.AddSingleton<ISecureStorage>(SecureStorage.Default);
			builder.Services.AddSingleton<IPreferences>(Preferences.Default);
			builder.Services.AddSingleton<IBrowser>(Browser.Default);
			builder.Services.AddSingleton<IClipboard>(Clipboard.Default);
			builder.Services.AddSingleton<ILauncher>(Launcher.Default);
			builder.Services.AddSingleton<IShare>(Share.Default);
			builder.Services.AddSingleton<IConnectivity>(Connectivity.Current);
			builder.Services.AddSingleton<IFilePicker>(FilePicker.Default);
			builder.Services.AddSingleton<IMediaPicker>(MediaPicker.Default);
			builder.Services.AddSingleton(_ => new HttpClient());
			builder.Services.AddSingleton(FileThumbnailCacheOptions.Default);
			builder.Services.AddSingleton(FileDownloadCacheOptions.Default);
			builder.Services.AddSingleton(
				new CottonMobileOptions(
					"Cotton Cloud",
					new Uri("https://app.cottoncloud.dev"),
					new Uri("https://cottoncloud.dev/privacy-policy"),
					"cotton-play-market-support@belov.us"));
			builder.Services.AddSingleton<IApplicationForegroundService, ApplicationForegroundService>();
			builder.Services.AddSingleton<ICottonMobileApplicationMetadata, CottonMobileApplicationMetadata>();
			builder.Services.AddSingleton<IUserDialogService, UserDialogService>();
			builder.Services.AddSingleton<IScreenReaderService, ScreenReaderService>();
			builder.Services.AddSingleton<INetworkAccessService, NetworkAccessService>();
			builder.Services.AddSingleton<IFeedbackService, FeedbackService>();
			builder.Services.AddSingleton<IDiagnosticsPageService, DiagnosticsPageService>();
			builder.Services.AddSingleton<IStorageManagementService, StorageManagementService>();
			builder.Services.AddSingleton<IDeviceStorageSpaceService, DeviceStorageSpaceService>();
			builder.Services.AddSingleton<IStorageSettingsPageService, StorageSettingsPageService>();
			builder.Services.AddSingleton<ISecuritySettingsPageService, SecuritySettingsPageService>();
			builder.Services.AddSingleton<INotificationSettingsPageService, NotificationSettingsPageService>();
			builder.Services.AddSingleton<IActivityFeedPageService, ActivityFeedPageService>();
			builder.Services.AddSingleton<ITransfersPageService, TransfersPageService>();
			builder.Services.AddSingleton<IBackupSetupPageService, BackupSetupPageService>();
			builder.Services.AddSingleton<ICaptureInboxPageService, CaptureInboxPageService>();
			builder.Services.AddSingleton<ICaptureDestinationPickerPageService, CaptureDestinationPickerPageService>();
			builder.Services.AddSingleton<IFileDownloadCachePruner, FileDownloadCachePruner>();
			builder.Services.AddSingleton<ICottonFolderContentCache, FileSystemCottonFolderContentCache>();
			builder.Services.AddSingleton<ICottonTransferMetadataPathProvider, CottonTransferMetadataPathProvider>();
			builder.Services.AddSingleton<ICottonTransferMetadataStore, FileSystemCottonTransferMetadataStore>();
			builder.Services.AddSingleton<ICottonTransferActivitySignal, CottonTransferActivitySignal>();
			builder.Services.AddSingleton<ICottonTransferProgressSignal, CottonTransferProgressSignal>();
			builder.Services.AddSingleton<ICottonCameraBackupMetadataPathProvider, CottonCameraBackupMetadataPathProvider>();
			builder.Services.AddSingleton<ICottonCameraBackupUploadedMediaStore, FileSystemCottonCameraBackupUploadedMediaStore>();
			builder.Services.AddSingleton<ICottonOfflineFileMetadataPathProvider, CottonOfflineFileMetadataPathProvider>();
			builder.Services.AddSingleton<ICottonOfflineFilePinStore, FileSystemCottonOfflineFilePinStore>();
			builder.Services.AddSingleton<ICottonSyncRootMetadataPathProvider, CottonSyncRootMetadataPathProvider>();
			builder.Services.AddSingleton<ICottonSyncRootStore, FileSystemCottonSyncRootStore>();
			builder.Services.AddSingleton<CottonCloudToDeviceSyncRootSetupService>();
			builder.Services.AddSingleton<ICottonSyncedFileManifestPathProvider, CottonSyncedFileManifestPathProvider>();
			builder.Services.AddSingleton<ICottonSyncedFileManifestStore, FileSystemCottonSyncedFileManifestStore>();
#if ANDROID
			builder.Services.AddSingleton<IAndroidApiLevelProvider, AndroidApiLevelProvider>();
			builder.Services.AddSingleton<ICottonNotificationChannelProvisioningService, AndroidNotificationChannelProvisioningService>();
			builder.Services.AddSingleton<ICottonNotificationPermissionService, AndroidNotificationPermissionService>();
			builder.Services.AddSingleton<ICottonLocalNotificationService, AndroidLocalNotificationService>();
			builder.Services.AddSingleton<ICottonCameraBackupMediaAccessPolicy, AndroidCameraBackupMediaAccessPolicy>();
			builder.Services.AddSingleton<ICottonAndroidBackgroundTransferHost, AndroidBackgroundTransferHost>();
			builder.Services.AddSingleton<AndroidCameraBackupMediaSource>();
			builder.Services.AddSingleton<ICottonCameraBackupMediaSource>(
				services => services.GetRequiredService<AndroidCameraBackupMediaSource>());
			builder.Services.AddSingleton<ICottonCameraBackupMediaContentSource>(
				services => services.GetRequiredService<AndroidCameraBackupMediaSource>());
#else
			builder.Services.AddSingleton<IAndroidApiLevelProvider, DisabledAndroidApiLevelProvider>();
			builder.Services.AddSingleton<ICottonNotificationChannelProvisioningService, DisabledCottonNotificationChannelProvisioningService>();
			builder.Services.AddSingleton<ICottonNotificationPermissionService, DisabledCottonNotificationPermissionService>();
			builder.Services.AddSingleton<ICottonLocalNotificationService>(_ => NullCottonLocalNotificationService.Instance);
			builder.Services.AddSingleton<ICottonCameraBackupMediaAccessPolicy, DisabledCottonCameraBackupMediaAccessPolicy>();
			builder.Services.AddSingleton<ICottonAndroidBackgroundTransferHost>(_ => DisabledCottonAndroidBackgroundTransferHost.Instance);
			builder.Services.AddSingleton<DisabledCottonCameraBackupMediaSource>();
			builder.Services.AddSingleton<ICottonCameraBackupMediaSource>(
				services => services.GetRequiredService<DisabledCottonCameraBackupMediaSource>());
			builder.Services.AddSingleton<ICottonCameraBackupMediaContentSource>(
				services => services.GetRequiredService<DisabledCottonCameraBackupMediaSource>());
#endif
			builder.Services.AddSingleton<ICottonCameraBackupScanner, CottonCameraBackupScanner>();
			builder.Services.AddSingleton<ICottonCameraBackupPlanningService, CottonCameraBackupPlanningService>();
			builder.Services.AddSingleton<ICottonCameraBackupTransferEnqueueCoordinator, CottonCameraBackupTransferEnqueueCoordinator>();
			builder.Services.AddSingleton<ICottonTransferStagingPathProvider, CottonTransferStagingPathProvider>();
			builder.Services.AddSingleton<ICottonTransferStagingStore, FileSystemCottonTransferStagingStore>();
			builder.Services.AddSingleton<ICottonTransferQueueRestoreCoordinator, CottonTransferQueueRestoreCoordinator>();
			builder.Services.AddSingleton<ICottonAndroidBackgroundTransferCoordinator, CottonAndroidBackgroundTransferCoordinator>();
			builder.Services.AddSingleton<ICottonQueuedUploadClient, CottonQueuedUploadClient>();
			builder.Services.AddSingleton<ICottonQueuedUploadExecutor>(
				services => new CottonQueuedUploadExecutor(
					services.GetRequiredService<ICottonTransferMetadataStore>(),
					services.GetRequiredService<ICottonTransferStagingStore>(),
					services.GetRequiredService<ICottonQueuedUploadClient>(),
					services.GetRequiredService<ICottonCameraBackupUploadedMediaStore>(),
					services.GetRequiredService<ICottonLocalNotificationService>(),
					timeProvider: null,
					transferActivitySignal: services.GetRequiredService<ICottonTransferActivitySignal>(),
					transferProgressSignal: services.GetRequiredService<ICottonTransferProgressSignal>()));
			builder.Services.AddSingleton<ICottonAndroidBackgroundTransferJobRunner, CottonAndroidBackgroundTransferJobRunner>();
			builder.Services.AddSingleton<ICottonShareIntakePathProvider, CottonShareIntakePathProvider>();
			builder.Services.AddSingleton<ICottonShareLaunchState, CottonShareLaunchState>();
			builder.Services.AddSingleton<ICottonNotificationLaunchState, CottonNotificationLaunchState>();
			builder.Services.AddSingleton<ICottonShareIntakeStore, FileSystemCottonShareIntakeStore>();
			builder.Services.AddSingleton<ICottonShareContentStagingStore, FileSystemCottonShareContentStagingStore>();
			builder.Services.AddSingleton<ICottonShareTransferEnqueueCoordinator, CottonShareTransferEnqueueCoordinator>();
#if ANDROID
			builder.Services.AddSingleton<IAndroidShareIntentStagingService, AndroidShareIntentStagingService>();
			builder.Services.AddSingleton<IAndroidDocumentScanActivityResultBridge, AndroidDocumentScanActivityResultBridge>();
			builder.Services.AddSingleton<IDocumentScanService, AndroidDocumentScanService>();
#else
			builder.Services.AddSingleton<IDocumentScanService, DisabledDocumentScanService>();
#endif
			builder.Services.AddSingleton<ICottonFileBrowserService, CottonFileBrowserService>();
			builder.Services.AddSingleton<
				ICottonCloudToDeviceSyncFolderContentSource,
				CottonFileBrowserCloudToDeviceSyncFolderContentSource>();
			builder.Services.AddSingleton<
				ICottonCloudToDeviceSyncFileOperator,
				CottonAppPrivateCloudToDeviceSyncFileOperator>();
			builder.Services.AddSingleton(services =>
				new CottonCloudToDeviceSyncPlanExecutor(
					services.GetRequiredService<ICottonCloudToDeviceSyncFileOperator>(),
					services.GetRequiredService<ICottonSyncedFileManifestStore>()));
			builder.Services.AddSingleton<CottonCloudToDeviceSyncCoordinator>();
			builder.Services.AddSingleton<ICottonFileUploadService, CottonFileUploadService>();
			builder.Services.AddSingleton<ICottonCameraBackupSettingsStore, PreferencesCottonCameraBackupSettingsStore>();
			builder.Services.AddSingleton<IFileBrowserPreferenceStore, PreferencesFileBrowserPreferenceStore>();
			builder.Services.AddSingleton<IFileUploadPickerService, FileUploadPickerService>();
			builder.Services.AddSingleton<IPhotoUploadPickerService, PhotoUploadPickerService>();
			builder.Services.AddSingleton<IVideoUploadPickerService, VideoUploadPickerService>();
			builder.Services.AddSingleton<IUploadDestinationPickerPageService, UploadDestinationPickerPageService>();
			builder.Services.AddSingleton<IFileInteractionService, FileInteractionService>();
			builder.Services.AddSingleton<IFilePreviewService, FilePreviewService>();
			builder.Services.AddSingleton<ICloudShareLinkInteractionService, CloudShareLinkInteractionService>();
			builder.Services.AddSingleton<IFileThumbnailCache, FileThumbnailCache>();
			builder.Services.AddSingleton<IFileThumbnailProvider, FileThumbnailProvider>();
			builder.Services.AddSingleton<IMainPagePresentationService, MainPagePresentationService>();
			builder.Services.AddSingleton<ICottonProfileCacheStore, PreferencesCottonProfileCacheStore>();
			builder.Services.AddSingleton<ICottonAppLockSettingsStore, PreferencesCottonAppLockSettingsStore>();
			builder.Services.AddSingleton<ICottonAppLockRuntimeStateStore, PreferencesCottonAppLockRuntimeStateStore>();
			builder.Services.AddSingleton<
				ICottonLogoutCacheCleanupSettingsStore,
				PreferencesCottonLogoutCacheCleanupSettingsStore>();
			builder.Services.AddSingleton(CottonAppLockPolicy.Default);
			builder.Services.AddSingleton<CottonAppSwitcherPrivacyPolicy>();
			builder.Services.AddSingleton<ICottonAppLockCapabilityService, DeviceUnlockCottonAppLockCapabilityService>();
			builder.Services.AddSingleton<IAppLockGateService, AppLockGateService>();
			builder.Services.AddSingleton<ICottonAppLockCoordinator, CottonAppLockCoordinator>();
#if ANDROID
			builder.Services.AddSingleton<
				IAndroidDeviceCredentialUnlockActivityResultBridge,
				AndroidDeviceCredentialUnlockActivityResultBridge>();
			builder.Services.AddSingleton<ICottonDeviceUnlockService, AndroidDeviceUnlockService>();
			builder.Services.AddSingleton<ICottonWindowPrivacyService, AndroidCottonWindowPrivacyService>();
#else
			builder.Services.AddSingleton<ICottonDeviceUnlockService, UnavailableCottonDeviceUnlockService>();
			builder.Services.AddSingleton<ICottonWindowPrivacyService, DisabledCottonWindowPrivacyService>();
#endif
			builder.Services.AddSingleton<ICottonTokenStore, SecureStorageCottonTokenStore>();
			builder.Services.AddSingleton<ICottonPendingAppCodeSessionStore, SecureStorageCottonPendingAppCodeSessionStore>();
			builder.Services.AddSingleton<ICottonInstanceStore, PreferencesCottonInstanceStore>();
			builder.Services.AddSingleton<ICottonClientFactory, CottonClientFactory>();
			builder.Services.AddSingleton(services =>
			{
				ICottonMobileApplicationMetadata metadata = services.GetRequiredService<ICottonMobileApplicationMetadata>();
				return new CottonAuthenticatedApiClient(
					services.GetRequiredService<HttpClient>(),
					services.GetRequiredService<ICottonTokenStore>(),
					new CottonAuthenticatedApiHttpOptions(metadata.UserAgent, metadata.DeviceName));
			});
			builder.Services.AddSingleton<ICottonCloudShareLinkService, CottonCloudShareLinkService>();
			builder.Services.AddSingleton<ICottonActivityFeedService, CottonActivityFeedService>();
			builder.Services.AddSingleton<ICottonAccountSessionService, CottonAccountSessionService>();
			builder.Services.AddSingleton<ICottonRemotePushDeviceTokenService, CottonRemotePushDeviceTokenService>();
			builder.Services.AddSingleton<ICottonRemotePushPreferenceService, CottonRemotePushPreferenceService>();
#if ANDROID
			builder.Services.AddSingleton<ICottonRemotePushPlatformTokenProvider, AndroidFirebaseMessagingTokenProvider>();
#else
			builder.Services.AddSingleton<ICottonRemotePushPlatformTokenProvider>(_ =>
				new CottonStaticRemotePushPlatformTokenProvider(
					CottonRemotePushPlatformTokenSnapshot.NotConfigured(
						CottonRemotePushProviderKind.FirebaseCloudMessaging,
						CottonRemotePushMobilePlatform.Ios,
						"Remote push token provider is not configured for this platform.")));
#endif
			builder.Services.AddSingleton<CottonRemotePushRegistrationCoordinator>();
			builder.Services.AddSingleton<CottonRemotePushSessionRegistrationService>();
			builder.Services.AddSingleton<ICottonRemotePushSessionRegistrationService>(services =>
				services.GetRequiredService<CottonRemotePushSessionRegistrationService>());
			builder.Services.AddSingleton<ICottonRemotePushTokenRefreshHandler>(services =>
				services.GetRequiredService<CottonRemotePushSessionRegistrationService>());
			builder.Services.AddSingleton<ICottonRemotePushSessionRegistrationStatusProvider>(services =>
				services.GetRequiredService<CottonRemotePushSessionRegistrationService>());
			builder.Services.AddSingleton<ICottonRemotePushDiagnosticsService, CottonRemotePushDiagnosticsService>();
			builder.Services.AddSingleton<ICottonSessionService, CottonSessionService>();
			builder.Services.AddSingleton<AppShell>();
			builder.Services.AddTransient<MainPageViewModel>();
			builder.Services.AddTransient<MainPage>();
			builder.Services.AddTransient<AppLockGateViewModel>();
			builder.Services.AddTransient<AppLockGatePage>();
			builder.Services.AddTransient<StorageSettingsViewModel>();
			builder.Services.AddTransient<StoragePage>();
			builder.Services.AddTransient<SecuritySettingsViewModel>();
			builder.Services.AddTransient<SecuritySettingsPage>();
			builder.Services.AddTransient<NotificationSettingsViewModel>();
			builder.Services.AddTransient<NotificationSettingsPage>();
			builder.Services.AddTransient<ActivityFeedViewModel>();
			builder.Services.AddTransient<ActivityFeedPage>();
			builder.Services.AddTransient<BackupSetupViewModel>();
			builder.Services.AddTransient<BackupSetupPage>();
			builder.Services.AddTransient<TransfersViewModel>();
			builder.Services.AddTransient<TransfersPage>();
			builder.Services.AddTransient<CaptureInboxViewModel>();
			builder.Services.AddTransient<CaptureInboxPage>();
			builder.Services.AddTransient<CaptureDestinationPickerViewModel>();
			builder.Services.AddTransient<CaptureDestinationPickerPage>();
			builder.Services.AddTransient<MediaViewerPage>();

#if DEBUG
			builder.Logging.AddDebug();
#endif

			return builder.Build();
		}
	}
}
