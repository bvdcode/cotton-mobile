namespace Cotton.Mobile.Services
{
    public interface ICottonFileVersionHistoryService
    {
        Task<CottonFileVersionListSnapshot> GetVersionsAsync(
            Uri instanceUri,
            CottonFileBrowserEntry file,
            TimeZoneInfo displayTimeZone,
            CancellationToken cancellationToken = default);
    }
}
