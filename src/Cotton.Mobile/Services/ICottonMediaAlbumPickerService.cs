// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public interface ICottonMediaAlbumPickerService
    {
        Task<IReadOnlyList<CottonMediaAlbumSnapshot>?> PickAsync(
            IReadOnlyList<CottonMediaAlbumSnapshot> albums,
            CancellationToken cancellationToken = default);
    }
}
