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
