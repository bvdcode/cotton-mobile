// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Resources.Localization;
using Cotton.Mobile.Services;
using Cotton.Sdk.Auth;
using EasyExtensions.Mediator;

namespace Cotton.Mobile.DependencyInjection
{
    public static class CottonApplicationServiceCollectionExtensions
    {
        public static IServiceCollection AddCottonApplicationServices(this IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);
            services.AddMediator(configuration =>
                configuration.RegisterServicesFromAssemblyContaining<RunSyncRootRequest>());
            services.AddSingleton(
                new CottonMobileOptions(
                    AppResources.AppTitle,
                    new Uri("https://github.com/bvdcode/cotton-mobile"),
                    new Uri("https://cottoncloud.dev/privacy-policy"),
                    "cotton-play-market-support@belov.us"));
            services.AddSingleton<IApplicationForegroundService, ApplicationForegroundService>();
            services.AddSingleton<ICottonMobileApplicationMetadata, CottonMobileApplicationMetadata>();
            services.AddSingleton<IUserDialogService, UserDialogService>();
            services.AddSingleton<INetworkAccessService, NetworkAccessService>();
            services.AddSingleton<ICottonTokenStore, SecureStorageCottonTokenStore>();
            services.AddSingleton<ICottonPendingAppCodeSessionStore, SecureStorageCottonPendingAppCodeSessionStore>();
            services.AddSingleton<ICottonInstanceStore, PreferencesCottonInstanceStore>();
            services.AddSingleton<ICottonInstanceProbe, CottonServerInfoProbe>();
            services.AddSingleton<ICottonInstanceResolver, CottonInstanceResolver>();
            services.AddSingleton<ICottonProfileCacheStore, PreferencesCottonProfileCacheStore>();
            services.AddSingleton<ICottonNotificationCursorStore, PreferencesCottonNotificationCursorStore>();
            services.AddSingleton<ICottonNotificationBatchProvider, CottonSdkNotificationBatchProvider>();
            services.AddSingleton<ICottonClientFactory, CottonClientFactory>();
            services.AddSingleton<ICottonAppCodeAuthorizationService, CottonAppCodeAuthorizationService>();
            services.AddSingleton<ICottonSessionService, CottonSessionService>();
            services.AddSingleton<CottonNotificationDeliveryPlanner>();
            services.AddSingleton<ICottonNotificationPollingService, CottonNotificationPollingService>();
            services.AddSingleton<ICottonNotificationRealtimeService, CottonNotificationRealtimeService>();
            services.AddSingleton<ICottonNotificationSessionService, CottonNotificationSessionService>();
            services.AddSingleton<ICottonAutomaticSyncSessionService, CottonAutomaticSyncSessionService>();
            return services;
        }
    }
}
