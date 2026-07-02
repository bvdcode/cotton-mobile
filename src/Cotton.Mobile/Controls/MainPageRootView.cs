// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Controls
{
    public class MainPageRootView : Grid
    {
        private const string DefaultGridStyleResourceKey = "M3MainPageRootGrid";

        public MainPageRootView()
        {
            RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
            RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            SetDynamicResource(StyleProperty, DefaultGridStyleResourceKey);
        }
    }
}
