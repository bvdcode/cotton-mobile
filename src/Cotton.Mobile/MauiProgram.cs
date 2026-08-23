// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Handlers;
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
                .ConfigureMauiHandlers(handlers => handlers.AddHandler<Button, ButtonHandler>())
                .ConfigureFonts(fonts => fonts.AddMaterialSymbolsFonts())
                .AddCottonDiagnostics();

            builder.Services
                .AddCottonPlatformServices()
                .AddCottonApplicationServices()
                .AddCottonSyncServices()
                .AddCottonPresentation();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
