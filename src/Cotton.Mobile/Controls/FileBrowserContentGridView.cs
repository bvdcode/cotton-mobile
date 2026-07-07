// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Controls
{
    public class FileBrowserContentGridView : Grid
    {
        private const string DefaultGridStyleResourceKey = "M3FileBrowserContentGrid";

        public FileBrowserContentGridView()
        {
            RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
            SetDynamicResource(StyleProperty, DefaultGridStyleResourceKey);
        }
    }
}
