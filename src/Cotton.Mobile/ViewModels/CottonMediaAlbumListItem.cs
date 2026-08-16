// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Resources.Localization;
using Cotton.Mobile.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Cotton.Mobile.ViewModels
{
    public class CottonMediaAlbumListItem(CottonMediaAlbumSnapshot album) : ObservableObject
    {
        private readonly CottonMediaAlbumSnapshot _album =
            album ?? throw new ArgumentNullException(nameof(album));
        private bool _isSelected;

        public CottonMediaAlbumSnapshot Album => _album;

        public string DisplayName => Album.DisplayName;

        public string ItemCountText => SyncRootSetupResources.CreateMediaAlbumItemCount(Album.ItemCount);

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
    }
}
