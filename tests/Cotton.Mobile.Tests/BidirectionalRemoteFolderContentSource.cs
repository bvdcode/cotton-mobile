using Cotton.Mobile.Services;

namespace Cotton.Mobile.Tests
{
    internal class BidirectionalRemoteFolderContentSource : ICottonDeviceToCloudRemoteFolderContentSource
    {
        private readonly Dictionary<Guid, CottonFolderContent> _contentByFolderId = [];

        public void SetContent(Guid folderId, CottonFolderContent content)
        {
            _contentByFolderId[folderId] = content;
        }

        public Task<CottonFolderContent> LoadAsync(
            Uri instanceUri,
            CottonFolderHandle folder,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_contentByFolderId[folder.Id]);
        }
    }
}
