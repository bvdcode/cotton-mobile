namespace Cotton.Mobile.Services
{
    public interface ICottonFolderContentCache
    {
        Task SaveRootAsync(Uri instanceUri, CottonFolderContent content, CancellationToken cancellationToken = default);

        Task<CottonFolderContent?> LoadRootAsync(Uri instanceUri, CancellationToken cancellationToken = default);

        Task<CottonCachedFolderContentSnapshot?> LoadRootSnapshotAsync(
            Uri instanceUri,
            CancellationToken cancellationToken = default);

        Task SaveFolderAsync(Uri instanceUri, CottonFolderContent content, CancellationToken cancellationToken = default);

        Task<CottonFolderContent?> LoadFolderAsync(
            Uri instanceUri,
            CottonFolderHandle folder,
            CancellationToken cancellationToken = default);

        Task<CottonCachedFolderContentSnapshot?> LoadFolderSnapshotAsync(
            Uri instanceUri,
            CottonFolderHandle folder,
            CancellationToken cancellationToken = default);
    }
}
