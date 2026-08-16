// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.ViewModels;
namespace Cotton.Mobile.Services
{
    public class CottonMediaAlbumPickerService : ICottonMediaAlbumPickerService
    {
        public async Task<IReadOnlyList<CottonMediaAlbumSnapshot>?> PickAsync(
            IReadOnlyList<CottonMediaAlbumSnapshot> albums,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(albums);
            cancellationToken.ThrowIfCancellationRequested();

            TaskCompletionSource<IReadOnlyList<CottonMediaAlbumSnapshot>?> completion = new(
                TaskCreationOptions.RunContinuationsAsynchronously);
            CottonMediaAlbumPickerViewModel viewModel = new(
                albums,
                selection => completion.TrySetResult(selection));
            CottonMediaAlbumPickerPage page = new(viewModel);
            INavigation navigation = await ModalPageNavigation.ShowAsync(page);
            using CancellationTokenRegistration registration = cancellationToken.Register(
                () => completion.TrySetCanceled(cancellationToken));
            try
            {
                return await completion.Task.ConfigureAwait(false);
            }
            finally
            {
                await ModalPageNavigation.DismissAsync(navigation, page);
            }
        }
    }
}
