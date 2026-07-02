// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Controls
{
    [ContentProperty(nameof(Content))]
    public class MaterialRefreshView : RefreshView
    {
        private const string DefaultRefreshStyleResourceKey = "M3MaterialRefreshView";

        public MaterialRefreshView()
        {
            SetDynamicResource(StyleProperty, DefaultRefreshStyleResourceKey);
        }
    }
}
