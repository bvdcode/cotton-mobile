// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Services;
using Cotton.Mobile.ViewModels;

namespace Cotton.Mobile
{
    public class SyncDashboardTemplateSelector : DataTemplateSelector
    {
        public DataTemplate MediaLocationTemplate { get; set; } = null!;

        public DataTemplate SyncRootTemplate { get; set; } = null!;

        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            return item switch
            {
                MediaLocationAccessViewModel => MediaLocationTemplate,
                CottonSyncRootListItem => SyncRootTemplate,
                _ => throw new ArgumentException("Unsupported sync dashboard item.", nameof(item)),
            };
        }
    }
}
