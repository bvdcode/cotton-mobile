namespace Cotton.Mobile.Services
{
    public interface ICottonFileTrashClient
    {
        Task MoveFileToTrashAsync(
            Uri instanceUri,
            Guid fileId,
            string expectedETag,
            CancellationToken cancellationToken = default);
    }
}
