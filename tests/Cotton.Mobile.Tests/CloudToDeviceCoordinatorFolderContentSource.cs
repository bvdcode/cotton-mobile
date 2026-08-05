using Cotton.Mobile.Services;

namespace Cotton.Mobile.Tests
{
    internal class CloudToDeviceCoordinatorFolderContentSource : ICottonCloudToDeviceSyncFolderContentSource
    {
        private readonly Dictionary<Guid, CottonFolderContent> _contentByFolderId = [];

        public List<Guid> RequestedFolderIds { get; } = [];

        public void SetContent(Guid folderId, CottonFolderContent content)
        {
            _contentByFolderId[folderId] = content;
        }

        public Task<CottonFolderContent> LoadAsync(
            Uri instanceUri,
            CottonFolderHandle folder,
            CancellationToken cancellationToken = default)
        {
            RequestedFolderIds.Add(folder.Id);
            return Task.FromResult(_contentByFolderId[folder.Id]);
        }
    }
}
