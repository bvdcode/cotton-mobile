// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public interface ICottonCameraBackupUploadedMediaStore
    {
        Task<IReadOnlyList<CottonCameraBackupUploadedMediaSnapshot>> LoadAsync(
            Uri instanceUri,
            CancellationToken cancellationToken = default);

        Task SaveAsync(
            Uri instanceUri,
            IReadOnlyCollection<CottonCameraBackupUploadedMediaSnapshot> items,
            CancellationToken cancellationToken = default);

        Task AddOrReplaceAsync(
            Uri instanceUri,
            CottonCameraBackupUploadedMediaSnapshot item,
            CancellationToken cancellationToken = default);

        Task ClearAsync(
            Uri instanceUri,
            CancellationToken cancellationToken = default);
    }
}
