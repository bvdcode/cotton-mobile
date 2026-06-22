// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public interface IPhotoUploadPickerService
    {
        Task<CottonFileUploadSource?> PickPhotoAsync(CancellationToken cancellationToken = default);

        Task<IReadOnlyList<CottonFileUploadSource>> PickPhotosAsync(CancellationToken cancellationToken = default);
    }
}
