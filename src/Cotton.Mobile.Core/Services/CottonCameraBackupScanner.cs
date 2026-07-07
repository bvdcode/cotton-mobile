// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonCameraBackupScanner : ICottonCameraBackupScanner
    {
        private readonly ICottonCameraBackupMediaSource _mediaSource;

        public CottonCameraBackupScanner(ICottonCameraBackupMediaSource mediaSource)
        {
            ArgumentNullException.ThrowIfNull(mediaSource);

            _mediaSource = mediaSource;
        }

        public async Task<CottonCameraBackupScanResult> ScanAsync(
            CottonCameraBackupSettings settings,
            IReadOnlyCollection<CottonCameraBackupUploadedMediaSnapshot> uploadedMedia,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(settings);
            ArgumentNullException.ThrowIfNull(uploadedMedia);
            cancellationToken.ThrowIfCancellationRequested();

            IReadOnlyList<CottonCameraBackupCandidate> sourceCandidates =
                await _mediaSource.ListCandidatesAsync(cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            var seenIdentities = new HashSet<CottonCameraBackupMediaIdentity>(
                uploadedMedia.Select(item => item.Identity));
            var candidates = new List<CottonCameraBackupCandidate>();
            int skippedAlreadyTracked = 0;
            int skippedByPolicy = 0;

            foreach (CottonCameraBackupCandidate candidate in sourceCandidates)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (settings.PhotosOnly && !candidate.IsPhoto)
                {
                    skippedByPolicy++;
                    continue;
                }

                if (!seenIdentities.Add(candidate.Identity))
                {
                    skippedAlreadyTracked++;
                    continue;
                }

                candidates.Add(candidate);
            }

            return new CottonCameraBackupScanResult(
                candidates,
                sourceCandidates.Count,
                skippedAlreadyTracked,
                skippedByPolicy);
        }
    }
}
