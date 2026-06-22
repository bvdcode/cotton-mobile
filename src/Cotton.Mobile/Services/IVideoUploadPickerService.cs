// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public interface IVideoUploadPickerService
    {
        Task<CottonFileUploadSource?> PickVideoAsync(CancellationToken cancellationToken = default);

        Task<IReadOnlyList<CottonFileUploadSource>> PickVideosAsync(CancellationToken cancellationToken = default);
    }
}
