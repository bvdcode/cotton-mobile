// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Commands;
using Cotton.Mobile.Services;

namespace Cotton.Mobile.ViewModels
{
    public class CloudFolderItemViewModel
    {
        private readonly CottonFolderHandle _folder;
        private readonly Func<CottonFolderHandle, Task> _openAsync;

        public CloudFolderItemViewModel(
            CottonFolderHandle folder,
            Func<CottonFolderHandle, Task> openAsync,
            Action<Exception> onUnhandledException)
        {
            ArgumentNullException.ThrowIfNull(folder);
            ArgumentNullException.ThrowIfNull(openAsync);
            ArgumentNullException.ThrowIfNull(onUnhandledException);

            _folder = folder;
            _openAsync = openAsync;
            Name = folder.Name;
            OpenCommand = new AsyncCommand(OpenAsync, onUnhandledException);
        }

        public string Name { get; }

        public AsyncCommand OpenCommand { get; }

        private Task OpenAsync()
        {
            return _openAsync(_folder);
        }
    }
}
