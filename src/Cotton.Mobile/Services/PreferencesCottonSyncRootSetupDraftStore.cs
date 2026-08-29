// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Microsoft.Maui.Storage;

namespace Cotton.Mobile.Services
{
    public class PreferencesCottonSyncRootSetupDraftStore(IPreferences preferences) :
        ICottonSyncRootSetupDraftStore
    {
        private const int SchemaVersion = 1;
        private const string Prefix = "Cotton.Mobile.SyncSetup.";
        private const string SchemaVersionKey = Prefix + "SchemaVersion";
        private const string RequestIdKey = Prefix + "RequestId";
        private const string OperationKey = Prefix + "Operation";
        private const string InstanceUriKey = Prefix + "InstanceUri";
        private const string AccountScopeKey = Prefix + "AccountScope";
        private const string StorageKindKey = Prefix + "StorageKind";
        private const string RetentionKey = Prefix + "Retention";
        private const string DestinationIdKey = Prefix + "DestinationId";
        private const string DestinationNameKey = Prefix + "DestinationName";
        private const string DestinationPathKey = Prefix + "DestinationPath";
        private const string RootIdKey = Prefix + "RootId";

        private readonly IPreferences _preferences =
            preferences ?? throw new ArgumentNullException(nameof(preferences));

        public Task<CottonSyncRootSetupDraft?> LoadAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int schemaVersion = _preferences.Get(SchemaVersionKey, 0);
            if (schemaVersion == 0)
            {
                return Task.FromResult<CottonSyncRootSetupDraft?>(null);
            }

            if (schemaVersion != SchemaVersion)
            {
                ClearValues();
                throw new InvalidDataException("Saved sync-root setup version is not supported.");
            }

            try
            {
                Guid requestId = Guid.Parse(_preferences.Get(RequestIdKey, string.Empty));
                CottonSyncRootSetupOperation operation = (CottonSyncRootSetupOperation)_preferences.Get(OperationKey, -1);
                Uri instanceUri = new(_preferences.Get(InstanceUriKey, string.Empty), UriKind.Absolute);
                string accountScope = _preferences.Get(AccountScopeKey, string.Empty);
                CottonSyncRootStorageKind storageKind =
                    (CottonSyncRootStorageKind)_preferences.Get(StorageKindKey, -1);
                CottonUploadOriginalRetention retention =
                    (CottonUploadOriginalRetention)_preferences.Get(RetentionKey, -1);
                CottonUploadDestinationSnapshot? destination = CreateDestination(operation);
                Guid? rootId = CreateRootId(operation);
                return Task.FromResult<CottonSyncRootSetupDraft?>(new CottonSyncRootSetupDraft(
                    requestId,
                    operation,
                    instanceUri,
                    accountScope,
                    storageKind,
                    retention,
                    destination,
                    rootId));
            }
            catch (Exception exception) when (exception is ArgumentException or FormatException or InvalidDataException)
            {
                ClearValues();
                throw new InvalidDataException("Saved sync-root setup is invalid.", exception);
            }
        }

        public Task SaveAsync(
            CottonSyncRootSetupDraft draft,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ArgumentNullException.ThrowIfNull(draft);
            ClearValues();
            _preferences.Set(RequestIdKey, draft.RequestId.ToString("N"));
            _preferences.Set(OperationKey, (int)draft.Operation);
            _preferences.Set(InstanceUriKey, draft.InstanceUri.AbsoluteUri);
            _preferences.Set(AccountScopeKey, draft.AccountScopeKey);
            _preferences.Set(StorageKindKey, (int)draft.StorageKind);
            _preferences.Set(RetentionKey, (int)draft.Retention);
            if (draft.Destination is CottonUploadDestinationSnapshot destination)
            {
                _preferences.Set(DestinationIdKey, destination.FolderId.ToString("N"));
                _preferences.Set(DestinationNameKey, destination.FolderName);
                _preferences.Set(DestinationPathKey, destination.Path);
            }

            if (draft.RootId.HasValue)
            {
                _preferences.Set(RootIdKey, draft.RootId.Value.ToString("N"));
            }

            _preferences.Set(SchemaVersionKey, SchemaVersion);
            return Task.CompletedTask;
        }

        public Task ClearAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ClearValues();
            return Task.CompletedTask;
        }

        private CottonUploadDestinationSnapshot? CreateDestination(CottonSyncRootSetupOperation operation)
        {
            return operation switch
            {
                CottonSyncRootSetupOperation.Add => new CottonUploadDestinationSnapshot(
                    Guid.Parse(_preferences.Get(DestinationIdKey, string.Empty)),
                    _preferences.Get(DestinationNameKey, string.Empty),
                    _preferences.Get(DestinationPathKey, string.Empty)),
                CottonSyncRootSetupOperation.Reconnect => null,
                _ => throw new InvalidDataException("Saved sync-root setup operation is not supported."),
            };
        }

        private Guid? CreateRootId(CottonSyncRootSetupOperation operation)
        {
            return operation switch
            {
                CottonSyncRootSetupOperation.Add => null,
                CottonSyncRootSetupOperation.Reconnect => Guid.Parse(_preferences.Get(RootIdKey, string.Empty)),
                _ => throw new InvalidDataException("Saved sync-root setup operation is not supported."),
            };
        }

        private void ClearValues()
        {
            _preferences.Remove(SchemaVersionKey);
            _preferences.Remove(RequestIdKey);
            _preferences.Remove(OperationKey);
            _preferences.Remove(InstanceUriKey);
            _preferences.Remove(AccountScopeKey);
            _preferences.Remove(StorageKindKey);
            _preferences.Remove(RetentionKey);
            _preferences.Remove(DestinationIdKey);
            _preferences.Remove(DestinationNameKey);
            _preferences.Remove(DestinationPathKey);
            _preferences.Remove(RootIdKey);
        }
    }
}
