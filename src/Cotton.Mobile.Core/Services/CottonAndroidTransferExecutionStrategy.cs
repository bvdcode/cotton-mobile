using System;

namespace Cotton.Mobile.Services
{
    public sealed class CottonAndroidTransferExecutionStrategy
    {
        public CottonAndroidTransferExecutionStrategy(
            CottonAndroidTransferWorkKind workKind,
            CottonAndroidTransferExecutionHost host,
            bool requiresUserInitiation,
            bool requiresUserVisibleNotification,
            bool supportsRetry,
            bool supportsNetworkConstraint,
            bool supportsChargingConstraint,
            bool supportsUnmeteredNetworkConstraint,
            string statusText)
        {
            if (!Enum.IsDefined(workKind))
            {
                throw new ArgumentOutOfRangeException(nameof(workKind), "Transfer work kind is not supported.");
            }

            if (!Enum.IsDefined(host))
            {
                throw new ArgumentOutOfRangeException(nameof(host), "Transfer execution host is not supported.");
            }

            if (string.IsNullOrWhiteSpace(statusText))
            {
                throw new ArgumentException("Transfer execution strategy status text is required.", nameof(statusText));
            }

            WorkKind = workKind;
            Host = host;
            RequiresUserInitiation = requiresUserInitiation;
            RequiresUserVisibleNotification = requiresUserVisibleNotification;
            SupportsRetry = supportsRetry;
            SupportsNetworkConstraint = supportsNetworkConstraint;
            SupportsChargingConstraint = supportsChargingConstraint;
            SupportsUnmeteredNetworkConstraint = supportsUnmeteredNetworkConstraint;
            StatusText = statusText.Trim();
        }

        public CottonAndroidTransferWorkKind WorkKind { get; }

        public CottonAndroidTransferExecutionHost Host { get; }

        public bool RequiresUserInitiation { get; }

        public bool RequiresUserVisibleNotification { get; }

        public bool SupportsRetry { get; }

        public bool SupportsNetworkConstraint { get; }

        public bool SupportsChargingConstraint { get; }

        public bool SupportsUnmeteredNetworkConstraint { get; }

        public string StatusText { get; }

        public bool IsBackgroundHost =>
            Host is CottonAndroidTransferExecutionHost.UserInitiatedDataTransfer
                or CottonAndroidTransferExecutionHost.WorkManagerConstrained;
    }
}
