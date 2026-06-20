namespace Cotton.Mobile.Services
{
    public static class CottonBidirectionalSyncExecutionPlanner
    {
        public static CottonBidirectionalSyncExecutionPlan Create(CottonBidirectionalSyncPlanSnapshot preflightPlan)
        {
            ArgumentNullException.ThrowIfNull(preflightPlan);

            if (preflightPlan.HasBlockingItems)
            {
                return CreateEmptyExecutionPlan(preflightPlan);
            }

            var cloudItems = new List<CottonCloudToDeviceSyncPlanItem>();
            var deviceItems = new List<CottonDeviceToCloudSyncPlanItem>();

            foreach (CottonBidirectionalSyncPlanItem item in preflightPlan.Items)
            {
                AddMappedItem(item, cloudItems, deviceItems);
            }

            return new CottonBidirectionalSyncExecutionPlan(
                preflightPlan,
                CreateCloudToDevicePlan(preflightPlan, cloudItems),
                CreateDeviceToCloudPlan(preflightPlan, deviceItems));
        }

        private static CottonBidirectionalSyncExecutionPlan CreateEmptyExecutionPlan(
            CottonBidirectionalSyncPlanSnapshot preflightPlan)
        {
            return new CottonBidirectionalSyncExecutionPlan(
                preflightPlan,
                CreateCloudToDevicePlan(preflightPlan, []),
                CreateDeviceToCloudPlan(preflightPlan, []));
        }

        private static void AddMappedItem(
            CottonBidirectionalSyncPlanItem item,
            ICollection<CottonCloudToDeviceSyncPlanItem> cloudItems,
            ICollection<CottonDeviceToCloudSyncPlanItem> deviceItems)
        {
            switch (item.Action)
            {
                case CottonBidirectionalSyncActionKind.DownloadNewFile:
                    cloudItems.Add(CreateCloudToDeviceItem(CottonCloudToDeviceSyncActionKind.DownloadNewFile, item));
                    break;

                case CottonBidirectionalSyncActionKind.RefreshLocalFile:
                    cloudItems.Add(CreateCloudToDeviceItem(CottonCloudToDeviceSyncActionKind.RefreshChangedFile, item));
                    break;

                case CottonBidirectionalSyncActionKind.RenameLocalFile:
                    cloudItems.Add(CreateCloudToDeviceItem(CottonCloudToDeviceSyncActionKind.RenameLocalFile, item));
                    break;

                case CottonBidirectionalSyncActionKind.RemoveLocalFile:
                    cloudItems.Add(CreateCloudToDeviceItem(CottonCloudToDeviceSyncActionKind.RemoveLocalOrphan, item));
                    break;

                case CottonBidirectionalSyncActionKind.UploadNewFile:
                    deviceItems.Add(CreateDeviceToCloudItem(CottonDeviceToCloudSyncActionKind.UploadNewFile, item));
                    break;

                case CottonBidirectionalSyncActionKind.UploadChangedFile:
                    deviceItems.Add(CreateDeviceToCloudItem(CottonDeviceToCloudSyncActionKind.UploadChangedFile, item));
                    break;

                case CottonBidirectionalSyncActionKind.CreateRemoteFolder:
                    deviceItems.Add(CreateDeviceToCloudItem(CottonDeviceToCloudSyncActionKind.CreateRemoteFolder, item));
                    break;

                case CottonBidirectionalSyncActionKind.DeleteRemoteFile:
                    deviceItems.Add(CreateDeviceToCloudItem(CottonDeviceToCloudSyncActionKind.DeleteRemoteFile, item));
                    break;

                case CottonBidirectionalSyncActionKind.RemoveManifestOrphan:
                    deviceItems.Add(CreateDeviceToCloudItem(CottonDeviceToCloudSyncActionKind.RemoveManifestOrphan, item));
                    break;

                case CottonBidirectionalSyncActionKind.KeepExistingFolder:
                    deviceItems.Add(CreateDeviceToCloudItem(CottonDeviceToCloudSyncActionKind.KeepExistingFolder, item));
                    break;

                case CottonBidirectionalSyncActionKind.KeepExistingFile:
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(item), "Blocked bidirectional sync items cannot be mapped for execution.");
            }
        }

        private static CottonCloudToDeviceSyncPlanItem CreateCloudToDeviceItem(
            CottonCloudToDeviceSyncActionKind action,
            CottonBidirectionalSyncPlanItem item)
        {
            return new CottonCloudToDeviceSyncPlanItem(
                action,
                item.TargetType,
                GetRequiredCloudItemId(item),
                item.DisplayName,
                item.ExpectedRemoteETag,
                item.RemoteUpdatedAtUtc,
                item.SizeBytes,
                item.ContentType,
                item.RelativePath,
                item.PreviousRelativePath);
        }

        private static CottonDeviceToCloudSyncPlanItem CreateDeviceToCloudItem(
            CottonDeviceToCloudSyncActionKind action,
            CottonBidirectionalSyncPlanItem item)
        {
            return new CottonDeviceToCloudSyncPlanItem(
                action,
                item.TargetType,
                item.DisplayName,
                item.RelativePath,
                item.CloudItemId,
                item.ExpectedRemoteETag,
                item.LocalUpdatedAtUtc,
                item.SizeBytes,
                item.ContentType,
                item.LocalSourceId);
        }

        private static CottonCloudToDeviceSyncPlanSnapshot CreateCloudToDevicePlan(
            CottonBidirectionalSyncPlanSnapshot preflightPlan,
            IReadOnlyList<CottonCloudToDeviceSyncPlanItem> items)
        {
            return new CottonCloudToDeviceSyncPlanSnapshot(
                preflightPlan.SyncRootId,
                preflightPlan.FolderId,
                preflightPlan.FolderName,
                items);
        }

        private static CottonDeviceToCloudSyncPlanSnapshot CreateDeviceToCloudPlan(
            CottonBidirectionalSyncPlanSnapshot preflightPlan,
            IReadOnlyList<CottonDeviceToCloudSyncPlanItem> items)
        {
            return new CottonDeviceToCloudSyncPlanSnapshot(
                preflightPlan.SyncRootId,
                preflightPlan.FolderId,
                preflightPlan.FolderName,
                items);
        }

        private static Guid GetRequiredCloudItemId(CottonBidirectionalSyncPlanItem item)
        {
            return item.CloudItemId
                ?? throw new InvalidOperationException("Bidirectional cloud-to-device item requires a cloud item id.");
        }
    }
}
