// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID
using System.Windows.Input;
using Android.App;
using Android.Views;
using Cotton.Mobile.Services;
using Microsoft.Maui.ApplicationModel;
using AndroidPopupMenu = Android.Widget.PopupMenu;

namespace Cotton.Mobile.Platforms.Android
{
    public static class AndroidSyncRootActionsMenu
    {
        public static void Show(
            Microsoft.Maui.Controls.Button anchor,
            CottonSyncRootListItem item,
            ICommand command)
        {
            ArgumentNullException.ThrowIfNull(anchor);
            ArgumentNullException.ThrowIfNull(item);
            ArgumentNullException.ThrowIfNull(command);

            Activity activity = Platform.CurrentActivity
                ?? throw new InvalidOperationException("Sync-root actions require an active Android activity.");
            global::Android.Views.View platformAnchor = anchor.Handler?.PlatformView as global::Android.Views.View
                ?? throw new InvalidOperationException("Sync-root actions require an Android anchor view.");
            AndroidPopupMenu menu = new(activity, platformAnchor, GravityFlags.End);
            AddAvailableActions(menu, item, command);
            AttachLifetime(menu, item, command);
            menu.Show();
        }

        private static void AddAvailableActions(
            AndroidPopupMenu menu,
            CottonSyncRootListItem item,
            ICommand command)
        {
            if (item.CanUsePrimaryAction)
            {
                AddAction(
                    menu,
                    AndroidSyncRootActionMenuItem.Primary,
                    item.PrimaryActionText,
                    item.PrimaryAction,
                    command);
            }

            if (item.CanPauseSync)
            {
                AddAction(
                    menu,
                    AndroidSyncRootActionMenuItem.Pause,
                    item.PauseSyncActionText,
                    item.PauseAction,
                    command);
            }

            if (item.CanResumeSync)
            {
                AddAction(
                    menu,
                    AndroidSyncRootActionMenuItem.Resume,
                    item.ResumeSyncActionText,
                    item.ResumeAction,
                    command);
            }

            if (item.CanStopSync)
            {
                AddAction(
                    menu,
                    AndroidSyncRootActionMenuItem.Stop,
                    item.StopSyncActionText,
                    item.StopAction,
                    command);
            }
        }

        private static void AddAction(
            AndroidPopupMenu menu,
            AndroidSyncRootActionMenuItem id,
            string text,
            CottonSyncRootActionRequest request,
            ICommand command)
        {
            IMenuItem menuItem = menu.Menu?.Add(IMenu.None, (int)id, (int)id, text)
                ?? throw new InvalidOperationException("Android sync-root menu item is unavailable.");
            _ = menuItem.SetEnabled(command.CanExecute(request));
        }

        private static void AttachLifetime(
            AndroidPopupMenu menu,
            CottonSyncRootListItem item,
            ICommand command)
        {
            EventHandler<AndroidPopupMenu.MenuItemClickEventArgs>? itemClick = null;
            EventHandler<AndroidPopupMenu.DismissEventArgs>? dismissed = null;
            itemClick = (_, eventArgs) =>
            {
                CottonSyncRootActionRequest request = ResolveRequest(item, eventArgs.Item?.ItemId);
                eventArgs.Handled = command.CanExecute(request);
                if (eventArgs.Handled)
                {
                    command.Execute(request);
                }
            };
            dismissed = (_, _) =>
            {
                menu.MenuItemClick -= itemClick;
                menu.DismissEvent -= dismissed;
                menu.Dispose();
            };
            menu.MenuItemClick += itemClick;
            menu.DismissEvent += dismissed;
        }

        private static CottonSyncRootActionRequest ResolveRequest(
            CottonSyncRootListItem item,
            int? itemId)
        {
            return (AndroidSyncRootActionMenuItem?)itemId switch
            {
                AndroidSyncRootActionMenuItem.Primary => item.PrimaryAction,
                AndroidSyncRootActionMenuItem.Pause => item.PauseAction,
                AndroidSyncRootActionMenuItem.Resume => item.ResumeAction,
                AndroidSyncRootActionMenuItem.Stop => item.StopAction,
                _ => throw new InvalidOperationException("Android sync-root menu action is not supported."),
            };
        }
    }
}
#endif
