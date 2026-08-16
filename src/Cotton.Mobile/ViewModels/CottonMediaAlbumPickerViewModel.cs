// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Cotton.Mobile.ViewModels
{
    public class CottonMediaAlbumPickerViewModel : ObservableObject
    {
        private readonly Action<IReadOnlyList<CottonMediaAlbumSnapshot>?> _complete;
        private bool _didComplete;

        public CottonMediaAlbumPickerViewModel(
            IReadOnlyList<CottonMediaAlbumSnapshot> albums,
            Action<IReadOnlyList<CottonMediaAlbumSnapshot>?> complete)
        {
            ArgumentNullException.ThrowIfNull(albums);
            ArgumentNullException.ThrowIfNull(complete);

            _complete = complete;
            Albums = [.. albums.Select(album => new CottonMediaAlbumListItem(album))];
            foreach (CottonMediaAlbumListItem item in Albums)
            {
                item.PropertyChanged += OnAlbumPropertyChanged;
            }

            ContinueCommand = new Command(Continue, CanContinue);
            CancelCommand = new Command(Cancel);
        }

        public IReadOnlyList<CottonMediaAlbumListItem> Albums { get; }

        public Command ContinueCommand { get; }

        public Command CancelCommand { get; }

        public bool IsEmptyVisible => Albums.Count == 0;

        public bool IsListVisible => !IsEmptyVisible;

        public bool IsInteractionLocked => _didComplete;

        public void Cancel()
        {
            CompleteOnce(albums: null);
        }

        private bool CanContinue()
        {
            return !_didComplete && Albums.Any(item => item.IsSelected);
        }

        private void Continue()
        {
            CottonMediaAlbumSnapshot[] selectedAlbums = [.. Albums
                .Where(item => item.IsSelected)
                .Select(item => item.Album)];
            if (selectedAlbums.Length == 0)
            {
                return;
            }

            CompleteOnce(selectedAlbums);
        }

        private void CompleteOnce(IReadOnlyList<CottonMediaAlbumSnapshot>? albums)
        {
            if (_didComplete)
            {
                return;
            }

            _didComplete = true;
            OnPropertyChanged(nameof(IsInteractionLocked));
            ContinueCommand.ChangeCanExecute();
            _complete(albums);
        }

        private void OnAlbumPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs eventArgs)
        {
            if (string.Equals(eventArgs.PropertyName, nameof(CottonMediaAlbumListItem.IsSelected), StringComparison.Ordinal))
            {
                ContinueCommand.ChangeCanExecute();
            }
        }
    }
}
