// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonSyncRootSetupDraft
    {
        public CottonSyncRootSetupDraft(
            Guid requestId,
            CottonSyncRootSetupOperation operation,
            Uri instanceUri,
            string accountScopeKey,
            CottonSyncRootStorageKind storageKind,
            CottonUploadOriginalRetention retention,
            CottonUploadDestinationSnapshot? destination,
            Guid? rootId)
        {
            if (requestId == Guid.Empty)
            {
                throw new ArgumentException("Setup request id is required.", nameof(requestId));
            }

            if (!Enum.IsDefined(operation))
            {
                throw new ArgumentOutOfRangeException(nameof(operation), "Setup operation is not supported.");
            }

            CottonInstanceUri.EnsureSupported(instanceUri, nameof(instanceUri));
            ArgumentException.ThrowIfNullOrWhiteSpace(accountScopeKey);
            if (!Enum.IsDefined(storageKind))
            {
                throw new ArgumentOutOfRangeException(nameof(storageKind), "Local storage kind is not supported.");
            }

            if (!Enum.IsDefined(retention))
            {
                throw new ArgumentOutOfRangeException(nameof(retention), "Upload retention is not supported.");
            }

            bool isValidAdd = operation == CottonSyncRootSetupOperation.Add
                && destination is not null
                && !rootId.HasValue;
            bool isValidReconnect = operation == CottonSyncRootSetupOperation.Reconnect
                && destination is null
                && rootId.HasValue
                && rootId.Value != Guid.Empty;
            if (!isValidAdd && !isValidReconnect)
            {
                throw new ArgumentException("Setup draft does not match its operation.", nameof(operation));
            }

            RequestId = requestId;
            Operation = operation;
            InstanceUri = instanceUri;
            AccountScopeKey = accountScopeKey.Trim();
            StorageKind = storageKind;
            Retention = retention;
            Destination = destination;
            RootId = rootId;
        }

        public Guid RequestId { get; }

        public CottonSyncRootSetupOperation Operation { get; }

        public Uri InstanceUri { get; }

        public string AccountScopeKey { get; }

        public CottonSyncRootStorageKind StorageKind { get; }

        public CottonUploadOriginalRetention Retention { get; }

        public CottonUploadDestinationSnapshot? Destination { get; }

        public Guid? RootId { get; }
    }
}
