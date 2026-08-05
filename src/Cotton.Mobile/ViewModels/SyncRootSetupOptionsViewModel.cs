// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Cotton.Mobile.ViewModels
{
    public class SyncRootSetupOptionsViewModel : ObservableObject
    {
        private readonly Action<SyncRootSetupOptions?> _complete;
        private CottonSyncDirection _direction = CottonSyncDirection.DeviceToCloud;
        private bool _deleteOriginalsAfterUpload;
        private bool _didComplete;

        public SyncRootSetupOptionsViewModel(Action<SyncRootSetupOptions?> complete)
        {
            ArgumentNullException.ThrowIfNull(complete);

            _complete = complete;
            SelectUploadOnlyCommand = new Command(
                () => SelectDirection(CottonSyncDirection.DeviceToCloud));
            SelectBidirectionalCommand = new Command(
                () => SelectDirection(CottonSyncDirection.Bidirectional));
            ContinueCommand = new Command(Continue);
            CancelCommand = new Command(Cancel);
        }

        public Command SelectUploadOnlyCommand { get; }

        public Command SelectBidirectionalCommand { get; }

        public Command ContinueCommand { get; }

        public Command CancelCommand { get; }

        public bool IsUploadOnlySelected => _direction == CottonSyncDirection.DeviceToCloud;

        public bool IsBidirectionalSelected => _direction == CottonSyncDirection.Bidirectional;

        public bool IsDeleteOptionVisible => IsUploadOnlySelected;

        public bool IsInteractionLocked => _didComplete;

        public bool DeleteOriginalsAfterUpload
        {
            get => _deleteOriginalsAfterUpload;
            set => SetProperty(ref _deleteOriginalsAfterUpload, value && IsUploadOnlySelected);
        }

        public void Cancel()
        {
            CompleteOnce(options: null);
        }

        private void SelectDirection(CottonSyncDirection direction)
        {
            switch (direction)
            {
                case CottonSyncDirection.DeviceToCloud:
                    break;

                case CottonSyncDirection.Bidirectional:
                    DeleteOriginalsAfterUpload = false;
                    break;

                case CottonSyncDirection.CloudToDevice:
                    throw new ArgumentException(
                        "Cloud-to-device sync cannot be selected during setup.",
                        nameof(direction));

                default:
                    throw new ArgumentOutOfRangeException(nameof(direction), "Sync direction is not supported.");
            }

            if (_direction == direction)
            {
                return;
            }

            _direction = direction;
            OnPropertyChanged(nameof(IsUploadOnlySelected));
            OnPropertyChanged(nameof(IsBidirectionalSelected));
            OnPropertyChanged(nameof(IsDeleteOptionVisible));
        }

        private void Continue()
        {
            CottonUploadOriginalRetention retention = _direction switch
            {
                CottonSyncDirection.DeviceToCloud => DeleteOriginalsAfterUpload
                    ? CottonUploadOriginalRetention.DeleteAfterConfirmedUpload
                    : CottonUploadOriginalRetention.KeepOriginals,
                CottonSyncDirection.Bidirectional => CottonUploadOriginalRetention.KeepOriginals,
                CottonSyncDirection.CloudToDevice => throw new InvalidOperationException(
                    "Cloud-to-device sync cannot be completed during setup."),
                _ => throw new ArgumentOutOfRangeException(
                    nameof(_direction),
                    "Sync direction is not supported."),
            };

            CompleteOnce(new SyncRootSetupOptions(_direction, retention));
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
