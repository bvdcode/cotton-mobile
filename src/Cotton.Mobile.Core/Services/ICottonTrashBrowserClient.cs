using Cotton.Nodes;

namespace Cotton.Mobile.Services
{
    public interface ICottonTrashBrowserClient
    {
        Task<NodeDto> GetTrashRootAsync(
            Uri instanceUri,
            CancellationToken cancellationToken = default);

        Task<NodeContentDto> GetChildrenAsync(
            Uri instanceUri,
            Guid trashFolderId,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default);
    }
}
