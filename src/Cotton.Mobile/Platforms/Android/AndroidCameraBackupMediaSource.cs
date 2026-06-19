#if ANDROID
namespace Cotton.Mobile.Services
{
    public sealed class AndroidCameraBackupMediaSource : ICottonCameraBackupMediaSource
    {
        private readonly ICottonCameraBackupMediaAccessPolicy _accessPolicy;

        public AndroidCameraBackupMediaSource(ICottonCameraBackupMediaAccessPolicy accessPolicy)
        {
            ArgumentNullException.ThrowIfNull(accessPolicy);

            _accessPolicy = accessPolicy;
        }

        public async Task<IReadOnlyList<CottonCameraBackupCandidate>> ListCandidatesAsync(
            CancellationToken cancellationToken = default)
        {
            if (!await _accessPolicy.CanReadMediaAsync(cancellationToken).ConfigureAwait(false))
            {
                return Array.Empty<CottonCameraBackupCandidate>();
            }

            cancellationToken.ThrowIfCancellationRequested();
            return Array.Empty<CottonCameraBackupCandidate>();
        }

        internal static bool TryCreateCandidate(
            CottonCameraBackupMediaSourceRecord record,
            out CottonCameraBackupCandidate? candidate)
        {
            return CottonCameraBackupMediaSourceRecordMapper.TryCreateCandidate(record, out candidate);
        }
    }
}
#endif
