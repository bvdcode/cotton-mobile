#if ANDROID
using Android.Content;
using Android.Provider;
using AndroidUri = Android.Net.Uri;

namespace Cotton.Mobile.Services
{
    public class AndroidDocumentTreeDeviceToCloudLocalFileContentSource :
        ICottonDeviceToCloudLocalFileContentSource
    {
        public Task<Stream> OpenReadAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonDeviceToCloudSyncPlanItem item,
            CancellationToken cancellationToken = default)
        {
            EnsureSupportedInput(instanceUri, root, item);
            cancellationToken.ThrowIfCancellationRequested();

            ContentResolver resolver = GetContentResolver();
            AndroidUri treeUri = ParseTreeUri(root);
            AndroidUri documentUri = CreateDocumentUri(treeUri, item);
            Stream stream = resolver.OpenInputStream(documentUri)
                ?? throw new IOException("Could not open device-to-cloud local file for reading.");
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(stream);
        }

        private static AndroidUri CreateDocumentUri(
            AndroidUri treeUri,
            CottonDeviceToCloudSyncPlanItem item)
        {
            string documentId = item.LocalSourceId
                ?? throw new InvalidOperationException("Device-to-cloud upload item is missing a local source id.");
            return DocumentsContract.BuildDocumentUriUsingTree(treeUri, documentId)
                ?? throw new IOException("Could not build device-to-cloud local file URI.");
        }

        private static AndroidUri ParseTreeUri(CottonSyncRootSnapshot root)
        {
            AndroidUri? uri = AndroidUri.Parse(root.LocalRoot.RootKey);
            return uri ?? throw new InvalidOperationException("Document-tree sync root URI is invalid.");
        }

        private static ContentResolver GetContentResolver()
        {
            return Android.App.Application.Context.ContentResolver
                ?? throw new InvalidOperationException("Android content resolver is unavailable.");
        }

        private static void EnsureSupportedInput(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonDeviceToCloudSyncPlanItem item)
        {
            CottonInstanceUri.EnsureSupported(instanceUri, nameof(instanceUri));
            ArgumentNullException.ThrowIfNull(root);
            ArgumentNullException.ThrowIfNull(item);

            if (!string.Equals(
                CottonMobileStoragePaths.CreateInstanceStorageKey(instanceUri),
                CottonMobileStoragePaths.CreateInstanceStorageKey(root.InstanceUri),
                StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Device-to-cloud sync instance does not match the sync root.");
            }

            if (!root.CanRunSync)
            {
                throw new InvalidOperationException("Device-to-cloud sync root is not ready.");
            }

            if (!root.LocalRoot.RequiresPersistedUserGrant)
            {
                throw new InvalidOperationException("Device-to-cloud local file reading only supports user-selected folders.");
            }

            if (root.Direction == CottonSyncDirection.CloudToDevice)
            {
                throw new InvalidOperationException("Device-to-cloud local file reading requires device-to-cloud sync direction.");
            }

            if (!item.RequiresUpload || item.TargetType != CottonFileBrowserEntryType.File)
            {
                throw new InvalidOperationException("Only device-to-cloud file upload items can open local content.");
            }
        }
    }
}
#endif
