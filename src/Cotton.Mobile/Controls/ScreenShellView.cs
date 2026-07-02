// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Controls
{
    [ContentProperty(nameof(Items))]
    public class ScreenShellView : ContentView
    {
        private readonly Grid _grid;

        public ScreenShellView()
        {
            _grid = new Grid();
            _grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            _grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });

            Content = _grid;
        }

        public IList<IView> Items => _grid.Children;
    }
}
