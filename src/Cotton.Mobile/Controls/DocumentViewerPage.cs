// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Controls
{
    public class DocumentViewerPage : ContentPage
    {
        private const string DefaultPageStyleResourceKey = "M3DocumentViewerPage";

        public DocumentViewerPage()
        {
            SetDynamicResource(StyleProperty, DefaultPageStyleResourceKey);
        }
    }
}
