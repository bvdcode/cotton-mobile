// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Platforms.Android;
using Cotton.Mobile.Services;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Networking;
using Microsoft.Maui.Storage;

namespace Cotton.Mobile.DependencyInjection
{
    public static class CottonPlatformServiceCollectionExtensions
    {
        public static IServiceCollection AddCottonPlatformServices(this IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);
            services.AddSingleton<ISecureStorage>(SecureStorage.Default);
            services.AddSingleton<IPreferences>(Preferences.Default);
            services.AddSingleton<IBrowser>(Browser.Default);
            services.AddSingleton<IConnectivity>(Connectivity.Current);
            services.AddSingleton(TimeProvider.System);
            services.AddSingleton<AndroidDocumentTreeActivityResultStore>();
            services.AddSingleton<IAndroidDocumentTreeActivityResultBridge, AndroidDocumentTreeActivityResultBridge>();
            services.AddSingleton<ICottonSyncLocalRootPickerService, AndroidSyncLocalRootPickerService>();
            services.AddSingleton<ICottonSyncLocalRootPermissionResolver, AndroidSyncLocalRootPermissionResolver>();
            services.AddSingleton<AndroidDocumentTreeDeviceToCloudLocalTreeReader>();
            services.AddSingleton<AndroidMediaStoreDeviceToCloudLocalTreeReader>();
            services.AddSingleton<ICottonDeviceToCloudLocalTreeReader, AndroidDeviceToCloudLocalTreeReaderRouter>();
            services.AddSingleton<AndroidDocumentTreeDeviceToCloudLocalFileContentSource>();
            services.AddSingleton<AndroidMediaStoreDeviceToCloudLocalFileContentSource>();
            services.AddSingleton<ICottonDeviceToCloudLocalFileContentSource, AndroidDeviceToCloudLocalFileContentSourceRouter>();
            services.AddSingleton<AndroidDocumentTreeDeviceToCloudLocalFileOperator>();
            services.AddSingleton<ICottonDeviceToCloudLocalFileOperator, AndroidDeviceToCloudLocalFileOperatorRouter>();
            services.AddSingleton<ICottonNotificationPermissionService, AndroidNotificationPermissionService>();
            services.AddSingleton<ICottonLocalNotificationService, AndroidLocalNotificationService>();
            services.AddSingleton<ICottonNotificationBackgroundScheduler, AndroidNotificationBackgroundScheduler>();
            services.AddSingleton<ICottonAutomaticSyncBackgroundScheduler, AndroidAutomaticSyncBackgroundScheduler>();
            services.AddSingleton<AndroidAutomaticSyncExecutor>();
            return services;
        }
    }
}
