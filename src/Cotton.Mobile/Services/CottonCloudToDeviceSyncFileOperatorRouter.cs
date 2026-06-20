namespace Cotton.Mobile.Services
{
    public class CottonCloudToDeviceSyncFileOperatorRouter : ICottonCloudToDeviceSyncFileOperator
    {
        private readonly CottonAppPrivateCloudToDeviceSyncFileOperator _appPrivateOperator;
        private readonly ICottonUserSelectedDocumentTreeCloudToDeviceSyncFileOperator _documentTreeOperator;

        public CottonCloudToDeviceSyncFileOperatorRouter(
            CottonAppPrivateCloudToDeviceSyncFileOperator appPrivateOperator,
            ICottonUserSelectedDocumentTreeCloudToDeviceSyncFileOperator documentTreeOperator)
        {
            ArgumentNullException.ThrowIfNull(appPrivateOperator);
            ArgumentNullException.ThrowIfNull(documentTreeOperator);

            _appPrivateOperator = appPrivateOperator;
            _documentTreeOperator = documentTreeOperator;
        }

        public Task DownloadOrReplaceAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonCloudToDeviceSyncPlanItem item,
            CancellationToken cancellationToken = default)
        {
            return SelectOperator(root).DownloadOrReplaceAsync(instanceUri, root, item, cancellationToken);
        }

        public Task RenameAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonCloudToDeviceSyncPlanItem item,
            CancellationToken cancellationToken = default)
        {
            return SelectOperator(root).RenameAsync(instanceUri, root, item, cancellationToken);
        }

        public Task RemoveAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonCloudToDeviceSyncPlanItem item,
            CancellationToken cancellationToken = default)
        {
            return SelectOperator(root).RemoveAsync(instanceUri, root, item, cancellationToken);
        }

        private ICottonCloudToDeviceSyncFileOperator SelectOperator(CottonSyncRootSnapshot root)
        {
            ArgumentNullException.ThrowIfNull(root);

            if (root.LocalRoot.UsesAppPrivateStorage)
            {
                return _appPrivateOperator;
            }

            if (root.LocalRoot.RequiresPersistedUserGrant)
            {
                return _documentTreeOperator;
            }

            throw new InvalidOperationException("Cloud-to-device sync root storage is not supported.");
        }
    }
}
