// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Controls
{
    public class DarkViewerPage : ContentPage
    {
        private const string DefaultPageStyleResourceKey = "M3DarkViewerPage";

        public DarkViewerPage()
        {
            SetDynamicResource(StyleProperty, DefaultPageStyleResourceKey);
        }
    }
}
