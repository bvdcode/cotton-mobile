// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Services;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls.Shapes;
using System.Windows.Input;

namespace Cotton.Mobile.Controls
{
    public class MaterialActionSheetPage : ContentPage
    {
        private readonly TaskCompletionSource<string?> _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private bool _isCompleting;

        public MaterialActionSheetPage(
            string title,
            string cancel,
            string? destruction,
            IReadOnlyList<string> buttons)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(title);
            ArgumentException.ThrowIfNullOrWhiteSpace(cancel);
            ArgumentNullException.ThrowIfNull(buttons);

            Shell.SetNavBarIsVisible(this, false);
            NavigationPage.SetHasNavigationBar(this, false);
            Style = MaterialResources.Get<Style>("M3ModalPage");
            Content = CreateContent(title, cancel, destruction, buttons);
        }

        public Task<string?> WaitForResultAsync()
        {
            return _completion.Task;
        }

        protected override bool OnBackButtonPressed()
        {
            MainThread.BeginInvokeOnMainThread(async () => await CompleteAsync(null));
            return true;
        }

        private Grid CreateContent(
            string title,
            string cancel,
            string? destruction,
            IReadOnlyList<string> buttons)
        {
            Grid root = new()
            {
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Star },
                    new RowDefinition { Height = GridLength.Auto },
                },
            };

            BoxView scrim = new()
            {
                Style = MaterialResources.Get<Style>("M3ModalScrim"),
            };
            scrim.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = CreateDismissCommand(null),
            });
            root.Add(scrim, 0, 0);
            Grid.SetRowSpan(scrim, 2);

            Border sheet = CreateSheetSurface();
            VerticalStackLayout stack = new()
            {
                Style = MaterialResources.Get<Style>("M3ActionSheetStack"),
            };

            stack.Add(CreateHandle());
            stack.Add(CreateTitle(title));

            foreach (string button in buttons)
            {
                stack.Add(CreateActionRow(button, button, isDestructive: false, isCancel: false));
            }

            if (!string.IsNullOrWhiteSpace(destruction))
            {
                stack.Add(CreateDivider());
                stack.Add(CreateActionRow(destruction, destruction, isDestructive: true, isCancel: false));
            }

            stack.Add(CreateDivider());
            stack.Add(CreateActionRow(cancel, null, isDestructive: false, isCancel: true));
            sheet.Content = stack;

            root.Add(sheet, 0, 1);
            return root;
        }

        private Border CreateSheetSurface()
        {
            return new Border
            {
                Style = MaterialResources.Get<Style>("M3ActionSheetSurface"),
            };
        }

        private Border CreateHandle()
        {
            return new Border
            {
                Style = MaterialResources.Get<Style>("M3ActionSheetHandle"),
            };
        }

        private Label CreateTitle(string title)
        {
            Label label = new()
            {
                Text = title,
                Style = MaterialResources.Get<Style>("M3ActionSheetTitle"),
            };
            return label;
        }

        private ActionSheetItemView CreateActionRow(
            string actionLabel,
            string? result,
            bool isDestructive,
            bool isCancel)
        {
            bool isSelected = CottonActionSheetCurrentLabel.TryCreateDisplayLabel(actionLabel, out string displayLabel);
            ActionSheetItemView row = new()
            {
                Text = displayLabel,
                IconData = ResolveIconData(displayLabel, isDestructive, isCancel),
                IsSelected = isSelected,
                Command = CreateDismissCommand(result),
                Style = MaterialResources.Get<Style>(
                    isDestructive
                        ? "M3ActionSheetDestructiveItem"
                        : "M3ActionSheetItem"),
            };

            SemanticProperties.SetDescription(row, displayLabel);
            return row;
        }

        private BoxView CreateDivider()
        {
            return new BoxView
            {
                Style = MaterialResources.Get<Style>("M3ActionSheetDivider"),
            };
        }

        private ICommand CreateDismissCommand(string? result)
        {
            return new Command(async () => await CompleteAsync(result));
        }

        private async Task CompleteAsync(string? result)
        {
            if (_isCompleting)
            {
                return;
            }

            _isCompleting = true;
            try
            {
                if (Navigation.ModalStack.Contains(this))
                {
                    await Navigation.PopModalAsync(animated: false);
                }
            }
            finally
            {
                _completion.TrySetResult(result);
            }
        }

        private static Geometry ResolveIconData(string label, bool isDestructive, bool isCancel)
        {
            if (isCancel)
            {
                return IconPathData.Close;
            }

            if (isDestructive || ContainsAny(label, "delete", "trash", "remove", "clear", "empty", "log out", "revoke"))
            {
                return IconPathData.Delete;
            }

            if (ContainsAny(label, "share"))
            {
                return IconPathData.Share;
            }

            if (ContainsAny(label, "copy", "link"))
            {
                return IconPathData.Copy;
            }

            if (ContainsAny(label, "open", "privacy"))
            {
                return IconPathData.OpenInNew;
            }

            if (ContainsAny(label, "download"))
            {
                return IconPathData.Download;
            }

            if (ContainsAny(label, "upload"))
            {
                return IconPathData.ArrowUp;
            }

            if (ContainsAny(label, "folder"))
            {
                return IconPathData.Folder;
            }

            if (ContainsAny(label, "photo", "image"))
            {
                return IconPathData.Image;
            }

            if (ContainsAny(label, "video", "media", "run", "resume"))
            {
                return IconPathData.Play;
            }

            if (ContainsAny(label, "pause"))
            {
                return IconPathData.Pause;
            }

            if (ContainsAny(label, "sync", "transfer", "activity", "notification"))
            {
                return IconPathData.Transfer;
            }

            if (ContainsAny(label, "refresh", "update", "newest"))
            {
                return IconPathData.Refresh;
            }

            if (ContainsAny(label, "name", "size", "type", "sort"))
            {
                return IconPathData.Sort;
            }

            if (ContainsAny(label, "list", "tiles", "view"))
            {
                return IconPathData.ViewTiles;
            }

            if (ContainsAny(label, "save"))
            {
                return IconPathData.Save;
            }

            if (ContainsAny(label, "security", "device", "session", "storage"))
            {
                return IconPathData.Device;
            }

            return IconPathData.MoreVertical;
        }

        private static bool ContainsAny(string value, params string[] candidates)
        {
            foreach (string candidate in candidates)
            {
                if (value.Contains(candidate, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

    }
}
