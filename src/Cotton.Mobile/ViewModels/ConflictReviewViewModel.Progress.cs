// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Services;
using Cotton.Mobile.Resources.Localization;

namespace Cotton.Mobile.ViewModels
{
    public partial class ConflictReviewViewModel
    {
        private readonly CottonSyncProgressHub _progressHub;
        private readonly ICottonAutomaticSyncStatusStore _statusStore;
        private bool _isDisposed;

        private void SetItems(CottonSyncReviewState state)
        {
            Dictionary<string, CottonSyncConflictSnapshot> selected = Items.Where(item => item.IsSelected)
                .ToDictionary(item => item.Conflict.LocalFile.LocalSourceId!, item => item.Conflict, StringComparer.Ordinal);
            Dictionary<string, CottonSyncReplacementApproval> approvals = state.Approvals
                .ToDictionary(item => item.Conflict.LocalFile.LocalSourceId!, StringComparer.Ordinal);
            foreach (ConflictReviewListItem item in Items)
            {
                item.PropertyChanged -= OnSelectionChanged;
            }

            Items.ReplaceWith(state.Conflicts.Select(conflict =>
            {
                string source = conflict.LocalFile.LocalSourceId!;
                bool queued = approvals.TryGetValue(source, out CottonSyncReplacementApproval? approval)
                    && approval.Conflict.Matches(conflict);
                return new ConflictReviewListItem(conflict, queued)
                {
                    IsSelected = selected.TryGetValue(source, out CottonSyncConflictSnapshot? previous)
                        && previous.Matches(conflict),
                };
            }));
            foreach (ConflictReviewListItem item in Items)
            {
                item.PropertyChanged += OnSelectionChanged;
            }
            NotifyActions();
        }

        private void OnProgressChanged(object? sender, CottonSyncProgressChangedEventArgs args)
        {
            if (args.RootId != _root.Id || args.Progress is null)
            {
                return;
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (!_isDisposed && !_lifetime.IsCancellationRequested && !IsBusy)
                {
                    Status = CottonSyncProgressText.Create(args.Progress);
                }
            });
        }

        private void OnStatusesChanged(object? sender, CottonAutomaticSyncStatusesChangedEventArgs args)
        {
            if (args.InstanceUri != _root.InstanceUri || !args.Statuses.TryGetValue(_root.Id, out CottonAutomaticSyncRootStatusSnapshot? status))
            {
                return;
            }

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                if (_isDisposed || _lifetime.IsCancellationRequested || IsBusy)
                {
                    return;
                }

                try
                {
                    CottonSyncReviewState state = await _service.LoadAsync(_root, _lifetime.Token);
                    if (!_isDisposed && !_lifetime.IsCancellationRequested && !IsBusy)
                    {
                        SetItems(state);
                        Status = CottonAutomaticSyncStatusText.Create(status);
                    }
                }
                catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
                {
                    Status = null;
                }
                catch (Exception exception)
                {
                    CottonLog.Warning(_logger, "Could not update the upload comparison.", exception);
                    Status = ConflictReviewResources.Failed;
                }
            });
        }
    }
}
