using Cotton.Files;

namespace Cotton.Mobile.Services
{
    public interface ICottonFileVersionHistoryClient
    {
        Task<IReadOnlyList<FileVersionDto>> GetVersionsAsync(
            Uri instanceUri,
            Guid fileId,
            CancellationToken cancellationToken = default);
    }
}
