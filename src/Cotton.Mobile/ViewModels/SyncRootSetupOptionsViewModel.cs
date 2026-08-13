// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Resources.Localization;
using Cotton.Mobile.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Cotton.Mobile.ViewModels
{
    public class SyncRootSetupOptionsViewModel : ObservableObject
    {
        private readonly Action<SyncRootSetupOptions?> _complete;
        private CottonSyncRootStorageKind _storageKind = CottonSyncRootStorageKind.UserSelectedDocumentTree;
        private bool _deleteOriginalsAfterUpload;
        private bool _didComplete;

        public SyncRootSetupOptionsViewModel(Action<SyncRootSetupOptions?> complete)
        {
            ArgumentNullException.ThrowIfNull(complete);

            _complete = complete;
            SelectFolderCommand = new Command(
                () => SelectStorageKind(CottonSyncRootStorageKind.UserSelectedDocumentTree));
            SelectMediaCommand = new Command(
                () => SelectStorageKind(CottonSyncRootStorageKind.MediaStore));
            ContinueCommand = new Command(Continue);
            CancelCommand = new Command(Cancel);
        }

        public Command SelectFolderCommand { get; }

        public Command SelectMediaCommand { get; }

        public Command ContinueCommand { get; }

        public Command CancelCommand { get; }

        public bool IsFolderSelected => _storageKind == CottonSyncRootStorageKind.UserSelectedDocumentTree;

        public bool IsMediaSelected => _storageKind == CottonSyncRootStorageKind.MediaStore;

        public bool IsDeleteOptionVisible => IsFolderSelected;

        public string FolderDescription => SyncRootSetupResources.CreateSourceDescription(
            SyncRootSetupResources.FolderTitle,
            IsFolderSelected);

        public string MediaDescription => SyncRootSetupResources.CreateSourceDescription(
            SyncRootSetupResources.MediaTitle,
            IsMediaSelected);

        public bool IsInteractionLocked => _didComplete;

        public bool DeleteOriginalsAfterUpload
        {
            get => _deleteOriginalsAfterUpload;
            set => SetProperty(ref _deleteOriginalsAfterUpload, value && IsFolderSelected);
        }

        public void Cancel()
        {
            CompleteOnce(options: null);
        }

        private void SelectStorageKind(CottonSyncRootStorageKind storageKind)
        {
            switch (storageKind)
            {
                case CottonSyncRootStorageKind.UserSelectedDocumentTree:
                    break;

                case CottonSyncRootStorageKind.MediaStore:
                    DeleteOriginalsAfterUpload = false;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(storageKind), "Sync source is not supported.");
            }

            if (_storageKind == storageKind)
            {
                return;
            }

            _storageKind = storageKind;
            OnPropertyChanged(nameof(IsFolderSelected));
            OnPropertyChanged(nameof(IsMediaSelected));
            OnPropertyChanged(nameof(IsDeleteOptionVisible));
            OnPropertyChanged(nameof(FolderDescription));
            OnPropertyChanged(nameof(MediaDescription));
        }

        private void Continue()
        {
            CottonUploadOriginalRetention retention = DeleteOriginalsAfterUpload
                ? CottonUploadOriginalRetention.DeleteAfterConfirmedUpload
                : CottonUploadOriginalRetention.KeepOriginals;
            CompleteOnce(new SyncRootSetupOptions(_storageKind, retention));
        }

        private void CompleteOnce(SyncRootSetupOptions? options)
        {
            if (_didComplete)
            {
                return;
            }

            _didComplete = true;
            OnPropertyChanged(nameof(IsInteractionLocked));
            _complete(options);
        }
    }
}
