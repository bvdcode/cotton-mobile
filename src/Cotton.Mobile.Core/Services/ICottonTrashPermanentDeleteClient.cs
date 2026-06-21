namespace Cotton.Mobile.Services
{
    public interface ICottonTrashPermanentDeleteClient
    {
        Task DeleteFileForeverAsync(
            Uri instanceUri,
            Guid fileId,
            string expectedETag,
            CancellationToken cancellationToken = default);

        Task DeleteFolderForeverAsync(
            Uri instanceUri,
            Guid folderId,
            CancellationToken cancellationToken = default);
    }
}
