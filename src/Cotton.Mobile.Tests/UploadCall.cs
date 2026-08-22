using Cotton.Mobile.Services;

namespace Cotton.Mobile.Tests
{
    internal record UploadCall(
        Uri InstanceUri,
        CottonDeviceToCloudSyncPlanItem Item,
        CottonFolderHandle ParentFolder);
}
