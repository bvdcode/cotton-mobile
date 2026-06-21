namespace Cotton.Mobile.Services
{
    public static class CottonAndroidTransferExecutionStrategyResolver
    {
        private const int Android14ApiLevel = 34;

        public static CottonAndroidTransferExecutionStrategy Resolve(
            CottonAndroidTransferWorkKind workKind,
            int androidApiLevel)
        {
            if (androidApiLevel < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(androidApiLevel));
            }

            return workKind switch
            {
                CottonAndroidTransferWorkKind.CameraBackupUpload => CreateCameraBackupStrategy(workKind),
                CottonAndroidTransferWorkKind.ManualUpload
                    or CottonAndroidTransferWorkKind.ManualDownload
                    or CottonAndroidTransferWorkKind.ShareInboxUpload
                    or CottonAndroidTransferWorkKind.SelectedMediaUpload => CreateUserStartedStrategy(workKind, androidApiLevel),
                _ => throw new ArgumentOutOfRangeException(nameof(workKind), "Transfer work kind is not supported."),
            };
        }

        private static CottonAndroidTransferExecutionStrategy CreateUserStartedStrategy(
            CottonAndroidTransferWorkKind workKind,
            int androidApiLevel)
        {
            if (androidApiLevel >= Android14ApiLevel)
            {
                return new CottonAndroidTransferExecutionStrategy(
                    workKind,
                    CottonAndroidTransferExecutionHost.UserInitiatedDataTransfer,
                    requiresUserInitiation: true,
                    requiresUserVisibleNotification: true,
                    supportsRetry: true,
                    supportsNetworkConstraint: true,
                    supportsChargingConstraint: false,
                    supportsUnmeteredNetworkConstraint: false,
                    "Use Android user-initiated data transfer for long user-started file transfers.");
            }

            return new CottonAndroidTransferExecutionStrategy(
                workKind,
                CottonAndroidTransferExecutionHost.ForegroundManual,
                requiresUserInitiation: true,
                requiresUserVisibleNotification: true,
                supportsRetry: true,
                supportsNetworkConstraint: false,
                supportsChargingConstraint: false,
                supportsUnmeteredNetworkConstraint: false,
                "Run user-started file transfers from the foreground queue on this Android version.");
        }

        private static CottonAndroidTransferExecutionStrategy CreateCameraBackupStrategy(
            CottonAndroidTransferWorkKind workKind)
        {
            return new CottonAndroidTransferExecutionStrategy(
                workKind,
                CottonAndroidTransferExecutionHost.WorkManagerConstrained,
                requiresUserInitiation: false,
                requiresUserVisibleNotification: true,
                supportsRetry: true,
                supportsNetworkConstraint: true,
                supportsChargingConstraint: true,
                supportsUnmeteredNetworkConstraint: true,
                "Use constrained persistent background work for automatic camera backup uploads.");
        }
    }
}
