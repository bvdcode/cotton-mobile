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
            BackgroundColor = Colors.Transparent;
            Padding = new Thickness(0);
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

            BoxView scrim = new();
            MaterialResources.SetThemeColor(scrim, BoxView.ColorProperty, "M3LightScrim", "M3DarkScrim");
            scrim.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = CreateDismissCommand(null),
            });
            root.Add(scrim, 0, 0);
            Grid.SetRowSpan(scrim, 2);

            Border sheet = CreateSheetSurface();
            VerticalStackLayout stack = new()
            {
                Padding = MaterialResources.Get<Thickness>("M3ActionSheetPadding"),
                Spacing = MaterialResources.Get<double>("Space8"),
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
            Border sheet = new()
            {
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.End,
                MaximumWidthRequest = MaterialResources.Get<double>("M3ActionSheetMaxWidth"),
                StrokeThickness = MaterialResources.Get<double>("M3StrokeNone"),
                StrokeShape = new RoundRectangle
                {
                    CornerRadius = MaterialResources.Get<CornerRadius>("M3ActionSheetCornerRadius"),
                },
            };
            MaterialResources.SetThemeColor(
                sheet,
                BackgroundColorProperty,
                "M3LightSurfaceContainerLowest",
                "M3DarkSurfaceContainerLow");
            return sheet;
        }

        private Border CreateHandle()
        {
            Border handle = new()
            {
                WidthRequest = MaterialResources.Get<double>("M3ActionSheetHandleWidth"),
                HeightRequest = MaterialResources.Get<double>("M3ActionSheetHandleHeight"),
                HorizontalOptions = LayoutOptions.Center,
                StrokeThickness = MaterialResources.Get<double>("M3StrokeNone"),
                StrokeShape = new RoundRectangle
                {
                    CornerRadius = new CornerRadius(MaterialResources.Get<double>("ShapeFull")),
                },
            };
            MaterialResources.SetThemeColor(
                handle,
                BackgroundColorProperty,
                "M3LightOutlineVariant",
                "M3DarkOutlineVariant");
            return handle;
        }

        private Label CreateTitle(string title)
        {
            Label label = new()
            {
                Text = title,
                Style = MaterialResources.Get<Style>("M3PanelTitle"),
                Margin = MaterialResources.Get<Thickness>("M3ActionSheetTitleMargin"),
                MaxLines = 3,
                LineBreakMode = LineBreakMode.TailTruncation,
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
                RowCornerRadius = MaterialResources.Get<double>("ShapeExtraLarge"),
                RowPadding = MaterialResources.Get<Thickness>("M3ActionSheetRowPadding"),
                RowMinHeight = MaterialResources.Get<double>("M3ActionSheetRowMinHeight"),
                IconFrameSize = MaterialResources.Get<double>("M3ActionSheetRowIconFrameSize"),
                IconSize = MaterialResources.Get<double>("M3ActionSheetRowIconSize"),
                IconFrameBorderWidth = MaterialResources.Get<double>("M3StrokeThin"),
                TextFontSize = MaterialResources.Get<double>("M3LabelLargeFontSize"),
                TextLineHeight = MaterialResources.Get<double>("M3LabelLargeLineHeight"),
                ContentSpacing = MaterialResources.Get<double>("Space12"),
                PressedOpacityMultiplier = MaterialResources.Get<double>("M3InteractionPressedOpacityFactor"),
                DisabledOpacity = MaterialResources.Get<double>("M3InteractionDisabledOpacity"),
            };

            SemanticProperties.SetDescription(row, displayLabel);
            MaterialResources.SetThemeColor(
                row,
                ActionSheetItemView.RowBackgroundColorProperty,
                "M3LightSurfaceContainerLowest",
                "M3DarkSurfaceContainerLow");
            MaterialResources.SetThemeColor(
                row,
                ActionSheetItemView.PressedRowBackgroundColorProperty,
                "M3LightSurfaceContainerHigh",
                "M3DarkSurfaceContainerHigh");
            MaterialResources.SetThemeColor(
                row,
                ActionSheetItemView.IconFrameBackgroundColorProperty,
                "M3LightSurfaceContainer",
                "M3DarkSurfaceContainer");
            MaterialResources.SetThemeColor(
                row,
                ActionSheetItemView.IconFrameBorderColorProperty,
                "M3LightOutlineVariant",
                "M3DarkOutlineVariant");

            if (isDestructive)
            {
                MaterialResources.SetThemeColor(
                    row,
                    ActionSheetItemView.TextColorProperty,
                    "M3LightError",
                    "M3DarkError");
                MaterialResources.SetThemeColor(
                    row,
                    ActionSheetItemView.IconColorProperty,
                    "M3LightError",
                    "M3DarkError");
            }
            else
            {
                MaterialResources.SetThemeColor(
                    row,
                    ActionSheetItemView.TextColorProperty,
                    "M3LightOnSurface",
                    "M3DarkOnSurface");
                row.IconColor = MaterialResources.Get<Color>("M3Accent");
            }

            return row;
        }

        private BoxView CreateDivider()
        {
            BoxView divider = new()
            {
                HeightRequest = MaterialResources.Get<double>("M3StrokeThin"),
                Margin = new Thickness(
                    MaterialResources.Get<double>("Space8"),
                    MaterialResources.Get<double>("Space4")),
            };
            MaterialResources.SetThemeColor(
                divider,
                BoxView.ColorProperty,
                "M3LightOutlineVariant",
                "M3DarkOutlineVariant");
            return divider;
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
