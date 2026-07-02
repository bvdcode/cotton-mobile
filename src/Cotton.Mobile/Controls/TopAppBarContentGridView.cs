// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Controls
{
    public class TopAppBarContentGridView : Grid
    {
        private const string DefaultGridStyleResourceKey = "M3TopAppBarContentGrid";

        public TopAppBarContentGridView()
        {
            double touchTarget = MaterialResources.Get<double>("TouchTarget");

            ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(touchTarget) });
            ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            SetDynamicResource(StyleProperty, DefaultGridStyleResourceKey);
        }
    }
}
