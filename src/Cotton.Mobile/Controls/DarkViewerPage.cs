// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Cotton.Mobile.Controls
{
    public class DarkViewerPage : ContentPage
    {
        private const string DefaultPageStyleResourceKey = "M3DarkViewerPage";
        private IViewerSystemChromeService? _systemChromeService;

        public DarkViewerPage()
        {
            SetDynamicResource(StyleProperty, DefaultPageStyleResourceKey);
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            ResolveSystemChromeService()?.SetDarkViewerActive(true);
        }

        protected override void OnDisappearing()
        {
            ResolveSystemChromeService()?.SetDarkViewerActive(false);

            base.OnDisappearing();
        }

        private IViewerSystemChromeService? ResolveSystemChromeService()
        {
            _systemChromeService ??= IPlatformApplication.Current?.Services
                .GetService<IViewerSystemChromeService>();
            return _systemChromeService;
        }
    }
}
