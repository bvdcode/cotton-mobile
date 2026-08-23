// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.ViewModels;

namespace Cotton.Mobile.DependencyInjection
{
    public static class CottonPresentationServiceCollectionExtensions
    {
        public static IServiceCollection AddCottonPresentation(this IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);
            services.AddSingleton<IMainPagePresentationService, MainPagePresentationService>();
            services.AddSingleton<MainPageSessionCoordinator>();
            services.AddSingleton<MainPageUserInteractionService>();
            services.AddSingleton<SyncSettingsRootProvider>();
            services.AddSingleton<SyncSettingsStatusObserver>();
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
            return services;
        }
    }
}
