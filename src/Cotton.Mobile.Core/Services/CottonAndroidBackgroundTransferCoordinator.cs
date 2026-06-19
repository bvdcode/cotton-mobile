namespace Cotton.Mobile.Services
{
    public sealed class CottonAndroidBackgroundTransferCoordinator :
        ICottonAndroidBackgroundTransferCoordinator
    {
        private readonly ICottonTransferMetadataStore _metadataStore;
        private readonly ICottonCameraBackupSettingsStore _cameraBackupSettingsStore;
        private readonly ICottonAndroidBackgroundTransferHost _host;

        public CottonAndroidBackgroundTransferCoordinator(
            ICottonTransferMetadataStore metadataStore,
            ICottonCameraBackupSettingsStore cameraBackupSettingsStore,
            ICottonAndroidBackgroundTransferHost host)
        {
            ArgumentNullException.ThrowIfNull(metadataStore);
            ArgumentNullException.ThrowIfNull(cameraBackupSettingsStore);
            ArgumentNullException.ThrowIfNull(host);

            _metadataStore = metadataStore;
            _cameraBackupSettingsStore = cameraBackupSettingsStore;
            _host = host;
        }

        public async Task<CottonAndroidBackgroundTransferScheduleResult> ScheduleNextQueuedUploadAsync(
            Uri instanceUri,
            int androidApiLevel,
            CancellationToken cancellationToken = default)
        {
            return await ScheduleNextQueuedUploadAsync(
                    instanceUri,
                    androidApiLevel,
                    static _ => true,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<CottonAndroidBackgroundTransferScheduleResult> ScheduleNextQueuedCameraBackupUploadAsync(
            Uri instanceUri,
            int androidApiLevel,
            CancellationToken cancellationToken = default)
        {
            return await ScheduleNextQueuedUploadAsync(
                    instanceUri,
                    androidApiLevel,
                    static item => item.Source?.Kind == CottonTransferSourceKind.CameraBackup,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        private async Task<CottonAndroidBackgroundTransferScheduleResult> ScheduleNextQueuedUploadAsync(
            Uri instanceUri,
            int androidApiLevel,
            Func<CottonTransferQueueItem, bool> transferFilter,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(transferFilter);

            IReadOnlyList<CottonTransferQueueItem> queue =
                await _metadataStore.LoadAsync(instanceUri, cancellationToken).ConfigureAwait(false);
            CottonTransferQueueItem? transfer = queue.FirstOrDefault(
                item => item.Kind == CottonTransferKind.Upload
                    && item.Status == CottonTransferStatus.Queued
                    && transferFilter(item));
            if (transfer is null)
            {
                return CottonAndroidBackgroundTransferScheduleResult.NoQueuedTransfer();
            }

            CottonAndroidBackgroundTransferRequest request =
                await CreateRequestAsync(
                        instanceUri,
                        transfer,
                        androidApiLevel,
                        cancellationToken)
                    .ConfigureAwait(false);
            if (!request.Strategy.IsBackgroundHost)
            {
                return CottonAndroidBackgroundTransferScheduleResult.ForegroundRequired(
                    request,
                    request.Strategy.StatusText);
            }

            return await _host.ScheduleAsync(request, cancellationToken).ConfigureAwait(false);
        }

        private async Task<CottonAndroidBackgroundTransferRequest> CreateRequestAsync(
            Uri instanceUri,
            CottonTransferQueueItem transfer,
            int androidApiLevel,
            CancellationToken cancellationToken)
        {
            CottonAndroidTransferWorkKind workKind = ResolveWorkKind(transfer);
            CottonAndroidTransferExecutionStrategy strategy =
                CottonAndroidTransferExecutionStrategyResolver.Resolve(workKind, androidApiLevel);
            CottonCameraBackupSettings? cameraBackupSettings = workKind == CottonAndroidTransferWorkKind.CameraBackupUpload
                ? await _cameraBackupSettingsStore.GetAsync(instanceUri, cancellationToken).ConfigureAwait(false)
                : null;

            bool requiresUnmeteredNetwork = cameraBackupSettings is not null
                && cameraBackupSettings.WifiOnly
                && !cameraBackupSettings.AllowCellular;
            bool requiresCharging = cameraBackupSettings?.ChargingOnly == true;

            return new CottonAndroidBackgroundTransferRequest(
                instanceUri,
                transfer.Id,
                transfer.DisplayName,
                strategy,
                transfer.Progress.TotalBytes ?? transfer.Source?.SizeBytes,
                requiresNetwork: true,
                requiresUnmeteredNetwork,
                requiresCharging);
        }

        private static CottonAndroidTransferWorkKind ResolveWorkKind(CottonTransferQueueItem transfer)
        {
            if (transfer.Source?.Kind == CottonTransferSourceKind.CameraBackup)
            {
                return CottonAndroidTransferWorkKind.CameraBackupUpload;
            }

            if (transfer.Source?.Kind == CottonTransferSourceKind.ShareInbox)
            {
                return CottonAndroidTransferWorkKind.ShareInboxUpload;
            }

            return CottonAndroidTransferWorkKind.ManualUpload;
        }
    }
}
