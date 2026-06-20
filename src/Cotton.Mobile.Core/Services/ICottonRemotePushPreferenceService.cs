namespace Cotton.Mobile.Services
{
    public interface ICottonRemotePushPreferenceService
    {
        Task<CottonRemotePushPreferences> GetCurrentAsync(
            Uri instanceUri,
            CancellationToken cancellationToken = default);

        Task<CottonRemotePushPreferences> UpdateCurrentAsync(
            Uri instanceUri,
            CottonRemotePushPreferences preferences,
            CancellationToken cancellationToken = default);
    }
}
