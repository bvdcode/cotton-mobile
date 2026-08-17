// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public static class CottonSyncRootActionMenu
    {
        public static IReadOnlyList<string> CreateActions(CottonSyncRootListItem item)
        {
            ArgumentNullException.ThrowIfNull(item);

            List<string> actions = [];
            if (item.CanShowFailureDetails)
            {
                actions.Add(CottonSyncRootManagementText.FailureDetailsAction);
            }

            if (item.CanUsePrimaryAction)
            {
                actions.Add(item.PrimaryActionText);
            }

            if (item.CanPauseSync)
            {
                actions.Add(item.PauseSyncActionText);
            }

            if (item.CanResumeSync)
            {
                actions.Add(item.ResumeSyncActionText);
            }

            return actions;
        }

        public static string? CreateDestructionAction(CottonSyncRootListItem item)
        {
            ArgumentNullException.ThrowIfNull(item);
            return item.CanStopSync ? item.StopSyncActionText : null;
        }

        public static CottonSyncRootAction? Resolve(
            CottonSyncRootListItem item,
            string? selected)
        {
            ArgumentNullException.ThrowIfNull(item);
            if (selected is null)
            {
                return null;
            }

            if (string.Equals(selected, CottonSyncRootManagementText.FailureDetailsAction, StringComparison.Ordinal))
            {
                return CottonSyncRootAction.ShowFailureDetails;
            }

            if (string.Equals(selected, item.PrimaryActionText, StringComparison.Ordinal))
            {
                return CottonSyncRootAction.UsePrimaryAction;
            }

            if (string.Equals(selected, item.PauseSyncActionText, StringComparison.Ordinal))
            {
                return CottonSyncRootAction.Pause;
            }

            if (string.Equals(selected, item.ResumeSyncActionText, StringComparison.Ordinal))
            {
                return CottonSyncRootAction.Resume;
            }

            if (string.Equals(selected, item.StopSyncActionText, StringComparison.Ordinal))
            {
                return CottonSyncRootAction.Stop;
            }

            throw new ArgumentException("Selected sync-root action is not available.", nameof(selected));
        }
    }
}
