using Cotton.Files;

namespace Cotton.Mobile.Services
{
    public class CottonFileVersionHistoryService : ICottonFileVersionHistoryService
    {
        private readonly ICottonFileVersionHistoryClient _client;

        public CottonFileVersionHistoryService(ICottonFileVersionHistoryClient client)
        {
            ArgumentNullException.ThrowIfNull(client);

            _client = client;
        }

        public async Task<CottonFileVersionListSnapshot> GetVersionsAsync(
            Uri instanceUri,
            CottonFileBrowserEntry file,
            TimeZoneInfo displayTimeZone,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(file);
            ArgumentNullException.ThrowIfNull(displayTimeZone);
            if (file.Type != CottonFileBrowserEntryType.File)
            {
                throw new ArgumentException("Version history can only be loaded for files.", nameof(file));
            }

            IReadOnlyList<FileVersionDto> versions = await _client
                .GetVersionsAsync(instanceUri, file.Id, cancellationToken)
                .ConfigureAwait(false);
            return CottonFileVersionListSnapshot.Create(file.Name, versions, displayTimeZone);
        }
    }
}
