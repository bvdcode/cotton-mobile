// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using System.Globalization;
using Cotton.Mobile.Resources.Localization;
using Cotton.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.ViewModels
{
    public class MediaOriginalRestoreReviewHandler : IDisposable
    {
        private readonly CottonMediaOriginalRestoreReview _review;
        private readonly SyncSettingsRootProvider _rootProvider;
        private readonly IApplicationForegroundService _foreground;
        private readonly IConnectivity _connectivity;
        private readonly IUserDialogService _dialogs;
        private readonly ILogger<MediaOriginalRestoreReviewHandler> _logger;
        private CancellationTokenSource _sessionCancellation = new();
        private Uri? _instanceUri;
        private string? _accountScopeKey;
        private bool _isChecking;
        private long _permissionVersion;
        private long _reviewedPermissionVersion;
        private long _requestVersion;

        public MediaOriginalRestoreReviewHandler(
            CottonMediaOriginalRestoreReview review,
            SyncSettingsRootProvider rootProvider,
            IApplicationForegroundService foreground,
            IConnectivity connectivity,
            IUserDialogService dialogs,
            ILogger<MediaOriginalRestoreReviewHandler> logger)
        {
            _review = review;
            _rootProvider = rootProvider;
            _foreground = foreground;
            _connectivity = connectivity;
            _dialogs = dialogs;
            _logger = logger;
            _foreground.Resumed += OnResumed;
        }

        public void Configure(Uri instanceUri, string accountScopeKey)
        {
            if (_instanceUri != instanceUri || _accountScopeKey != accountScopeKey)
            {
                Clear();
                _instanceUri = instanceUri;
                _accountScopeKey = accountScopeKey;
            }

            RequestReview();
        }

        public void PermissionGranted()
        {
            _permissionVersion++;
            RequestReview();
        }

        public void Clear()
        {
            _instanceUri = null;
            _accountScopeKey = null;
            _permissionVersion = 0;
            _reviewedPermissionVersion = 0;
            CancellationTokenSource previous = _sessionCancellation;
            _sessionCancellation = new CancellationTokenSource();
            _ = CancelSessionAsync(previous);
        }

        public void Dispose()
        {
            _foreground.Resumed -= OnResumed;
            _ = CancelSessionAsync(_sessionCancellation);
            GC.SuppressFinalize(this);
        }

        private void OnResumed(object? sender, EventArgs eventArgs)
        {
            RequestReview();
        }

        private void RequestReview()
        {
            _requestVersion++;
            MainThread.BeginInvokeOnMainThread(() => _ = ReviewAsync());
        }

        private async Task ReviewAsync()
        {
            if (_isChecking || !_foreground.IsForeground || _instanceUri is null || _accountScopeKey is null
                || _connectivity.NetworkAccess != NetworkAccess.Internet)
            {
                return;
            }

            long version = _requestVersion;
            long permissionVersion = _permissionVersion;
            CancellationToken sessionCancellation = _sessionCancellation.Token;
            _isChecking = true;
            try
            {
                SyncRootCollectionSnapshot roots = await _rootProvider.LoadAsync(_instanceUri, _accountScopeKey, sessionCancellation);
                await _review.RunAsync(roots, permissionVersion != _reviewedPermissionVersion, ConfirmAsync, sessionCancellation);
                sessionCancellation.ThrowIfCancellationRequested();
                _reviewedPermissionVersion = permissionVersion;
            }
            catch (OperationCanceledException)
            {
                CottonLog.Information(_logger, "Original media review was interrupted.");
            }
            catch (Exception exception)
            {
                CottonLog.Warning(_logger, "Failed to review original media copies.", exception);
            }
            finally
            {
                _isChecking = false;
                if (version != _requestVersion)
                {
                    MainThread.BeginInvokeOnMainThread(() => _ = ReviewAsync());
                }
            }
        }

        internal Task<bool> ConfirmAsync(int count, CancellationToken cancellationToken)
        {
            return MainThread.InvokeOnMainThreadAsync(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!_foreground.IsForeground)
                {
                    throw new OperationCanceledException("Original media review requires the application on screen.", cancellationToken);
                }

                return _dialogs.ShowConfirmationAsync(MediaRestoreResources.Title,
                    string.Format(CultureInfo.CurrentCulture, MediaRestoreResources.ConfirmationFormat, count),
                    MediaRestoreResources.Restore, MediaRestoreResources.KeepCopies);
            });
        }

        private async Task CancelReviewAsync(CancellationTokenSource cancellation)
        {
            try
            {
                await cancellation.CancelAsync();
            }
            catch (Exception exception)
            {
                CottonLog.Warning(_logger, "Failed to interrupt original media review.", exception);
            }
        }

        private async Task CancelSessionAsync(CancellationTokenSource cancellation)
        {
            await CancelReviewAsync(cancellation);
            cancellation.Dispose();
        }
    }
}
