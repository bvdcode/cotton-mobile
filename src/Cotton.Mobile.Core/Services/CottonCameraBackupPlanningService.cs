// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public sealed class CottonCameraBackupPlanningService : ICottonCameraBackupPlanningService
    {
        private readonly ICottonCameraBackupUploadedMediaStore _uploadedMediaStore;
        private readonly ICottonCameraBackupScanner _scanner;

        public CottonCameraBackupPlanningService(
            ICottonCameraBackupUploadedMediaStore uploadedMediaStore,
            ICottonCameraBackupScanner scanner)
        {
            ArgumentNullException.ThrowIfNull(uploadedMediaStore);
            ArgumentNullException.ThrowIfNull(scanner);

            _uploadedMediaStore = uploadedMediaStore;
            _scanner = scanner;
        }

        public async Task<CottonCameraBackupPlanSnapshot> PlanAsync(
            Uri instanceUri,
            CottonCameraBackupSettings settings,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(settings);
            cancellationToken.ThrowIfCancellationRequested();

            IReadOnlyList<CottonCameraBackupUploadedMediaSnapshot> uploadedMedia =
                await _uploadedMediaStore.LoadAsync(instanceUri, cancellationToken).ConfigureAwait(false);
            CottonCameraBackupScanResult scanResult =
                await _scanner.ScanAsync(settings, uploadedMedia, cancellationToken).ConfigureAwait(false);
            var health = new CottonCameraBackupHealthSnapshot(
                scanResult.Candidates.Count,
                uploadedMedia.Count,
                failedCount: 0,
                blockedCount: 0);

            return new CottonCameraBackupPlanSnapshot(scanResult, uploadedMedia, health);
        }
    }
}
