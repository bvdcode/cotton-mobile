using Cotton.Mobile.Services;

namespace Cotton.Mobile.Tests
{
    internal record FolderCreateCall(
        Uri InstanceUri,
        CottonDeviceToCloudSyncPlanItem Item,
        CottonFolderHandle ParentFolder);
}
