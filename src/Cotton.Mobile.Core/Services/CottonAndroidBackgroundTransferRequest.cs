// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonAndroidBackgroundTransferRequest
    {
        public CottonAndroidBackgroundTransferRequest(
            Uri instanceUri,
            Guid transferId,
            string displayName,
            CottonAndroidTransferExecutionStrategy strategy,
            long? estimatedUploadBytes,
            bool requiresNetwork,
            bool requiresUnmeteredNetwork,
            bool requiresCharging)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(strategy);

            if (!instanceUri.IsAbsoluteUri)
            {
                throw new ArgumentException("Instance URI must be absolute.", nameof(instanceUri));
            }

            if (transferId == Guid.Empty)
            {
                throw new ArgumentException("Transfer id cannot be empty.", nameof(transferId));
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("Transfer display name is required.", nameof(displayName));
            }

            if (estimatedUploadBytes < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(estimatedUploadBytes),
                    "Estimated upload bytes cannot be negative.");
            }

            if (requiresUnmeteredNetwork && !requiresNetwork)
            {
                throw new ArgumentException("Unmetered network requires network access.", nameof(requiresUnmeteredNetwork));
            }

            if (requiresUnmeteredNetwork && !strategy.SupportsUnmeteredNetworkConstraint)
            {
                throw new ArgumentException(
                    "Selected transfer strategy does not support unmetered network constraints.",
                    nameof(requiresUnmeteredNetwork));
            }

            if (requiresCharging && !strategy.SupportsChargingConstraint)
            {
                throw new ArgumentException(
                    "Selected transfer strategy does not support charging constraints.",
                    nameof(requiresCharging));
            }

            InstanceUri = instanceUri;
            TransferId = transferId;
            DisplayName = displayName.Trim();
            Strategy = strategy;
            ScheduleIdentity = CottonAndroidBackgroundTransferScheduleIdentity.Create(instanceUri, transferId);
            EstimatedUploadBytes = estimatedUploadBytes;
            RequiresNetwork = requiresNetwork;
            RequiresUnmeteredNetwork = requiresUnmeteredNetwork;
            RequiresCharging = requiresCharging;
        }

        public Uri InstanceUri { get; }

        public Guid TransferId { get; }

        public string DisplayName { get; }

        public CottonAndroidTransferExecutionStrategy Strategy { get; }

        public CottonAndroidBackgroundTransferScheduleIdentity ScheduleIdentity { get; }

        public long? EstimatedUploadBytes { get; }

        public bool RequiresNetwork { get; }

        public bool RequiresUnmeteredNetwork { get; }

        public bool RequiresCharging { get; }

        public CottonAndroidTransferWorkKind WorkKind => Strategy.WorkKind;

        public CottonAndroidTransferExecutionHost Host => Strategy.Host;
    }
}
