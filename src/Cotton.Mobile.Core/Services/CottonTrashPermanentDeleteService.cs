namespace Cotton.Mobile.Services
{
    public class CottonTrashPermanentDeleteService : ICottonTrashPermanentDeleteService
    {
        private readonly ICottonTrashPermanentDeleteClient _client;

        public CottonTrashPermanentDeleteService(ICottonTrashPermanentDeleteClient client)
        {
            ArgumentNullException.ThrowIfNull(client);

            _client = client;
        }

        public async Task<CottonTrashPermanentDeleteResult> DeleteForeverAsync(
            Uri instanceUri,
            CottonFileBrowserEntry item,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(item);

            if (item.Type == CottonFileBrowserEntryType.File)
            {
                CottonSyncDeleteSemanticsSnapshot semantics = CottonSyncDeleteSemantics.Create(
                    item,
                    CottonSyncDeleteMode.Permanent);
                if (semantics.SafetyStatus != CottonSyncDeleteSafetyStatus.ConflictSafe
                    || string.IsNullOrWhiteSpace(semantics.ExpectedETag))
                {
                    throw new InvalidOperationException(CottonTrashPermanentDeleteStatusText.NeedsRefreshStatus);
                }

                await _client.DeleteFileForeverAsync(
                        instanceUri,
                        item.Id,
                        semantics.ExpectedETag,
                        cancellationToken)
                    .ConfigureAwait(false);
                return CottonTrashPermanentDeleteResult.Deleted(item);
            }

            if (item.Type == CottonFileBrowserEntryType.Folder)
            {
                await _client.DeleteFolderForeverAsync(
                        instanceUri,
                        item.Id,
                        cancellationToken)
                    .ConfigureAwait(false);
                return CottonTrashPermanentDeleteResult.Deleted(item);
            }

            throw new ArgumentOutOfRangeException(nameof(item), "Delete item type is not supported.");
        }
    }
}
