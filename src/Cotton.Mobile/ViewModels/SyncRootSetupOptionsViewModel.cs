// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Cotton.Mobile.ViewModels
{
    public class SyncRootSetupOptionsViewModel : ObservableObject
    {
        private readonly Action<SyncRootSetupOptions?> _complete;
        private CottonSyncRootStorageKind? _storageKind;
        private bool _deleteOriginalsAfterUpload;
        private bool _didComplete;

        public SyncRootSetupOptionsViewModel(
            Action<SyncRootSetupOptions?> complete,
            MediaLocationAccessViewModel mediaLocationAccess)
        {
            ArgumentNullException.ThrowIfNull(complete);

            _complete = complete;
            MediaLocationAccess = mediaLocationAccess;
            MediaLocationAccess.Refresh();
            ContinueCommand = new Command(Continue, () => _storageKind.HasValue && !_didComplete);
            CancelCommand = new Command(Cancel);
        }

        public MediaLocationAccessViewModel MediaLocationAccess { get; }

        public Command ContinueCommand { get; }

        public Command CancelCommand { get; }

        public CottonSyncRootStorageKind? StorageKind
        {
            get => _storageKind;
            set => SelectStorageKind(value);
        }

        public bool IsDeleteOptionVisible =>
            StorageKind == CottonSyncRootStorageKind.UserSelectedDocumentTree;

        public bool IsInteractionLocked => _didComplete;

        public bool DeleteOriginalsAfterUpload
        {
            get => _deleteOriginalsAfterUpload;
            set => SetProperty(
                ref _deleteOriginalsAfterUpload,
                value && StorageKind == CottonSyncRootStorageKind.UserSelectedDocumentTree);
        }

        public void Cancel()
        {
            CompleteOnce(options: null);
        }

        private void SelectStorageKind(CottonSyncRootStorageKind? storageKind)
        {
            switch (storageKind)
            {
                case null:
                case CottonSyncRootStorageKind.UserSelectedDocumentTree:
                    break;

                case CottonSyncRootStorageKind.MediaStore:
                    DeleteOriginalsAfterUpload = false;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(storageKind), "Sync source is not supported.");
            }

            if (!SetProperty(ref _storageKind, storageKind, nameof(StorageKind)))
            {
                return;
            }

            OnPropertyChanged(nameof(IsDeleteOptionVisible));
            ContinueCommand.ChangeCanExecute();
        }

        private void Continue()
        {
            CottonSyncRootStorageKind storageKind = _storageKind
                ?? throw new InvalidOperationException("A sync source must be selected before continuing.");
            CottonUploadOriginalRetention retention = DeleteOriginalsAfterUpload
                ? CottonUploadOriginalRetention.DeleteAfterConfirmedUpload
                : CottonUploadOriginalRetention.KeepOriginals;
            CompleteOnce(new SyncRootSetupOptions(storageKind, retention));
        }

        private void CompleteOnce(SyncRootSetupOptions? options)
        {
            if (_didComplete)
            {
                return;
            }

            _didComplete = true;
            OnPropertyChanged(nameof(IsInteractionLocked));
            ContinueCommand.ChangeCanExecute();
            _complete(options);
        }
    }
}
