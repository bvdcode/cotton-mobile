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
            SetThemeColor(scrim, BoxView.ColorProperty, "M3LightScrim", "M3DarkScrim");
            scrim.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = CreateDismissCommand(null),
            });
            root.Add(scrim, 0, 0);
            Grid.SetRowSpan(scrim, 2);

            Border sheet = CreateSheetSurface();
            VerticalStackLayout stack = new()
            {
                Padding = GetResource<Thickness>("M3ActionSheetPadding"),
                Spacing = GetResource<double>("Space8"),
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
                MaximumWidthRequest = GetResource<double>("M3ActionSheetMaxWidth"),
                StrokeThickness = GetResource<double>("M3StrokeNone"),
                StrokeShape = new RoundRectangle
                {
                    CornerRadius = GetResource<CornerRadius>("M3ActionSheetCornerRadius"),
                },
            };
            SetThemeColor(sheet, BackgroundColorProperty, "M3LightSurfaceContainerLowest", "M3DarkSurfaceContainerLow");
            return sheet;
        }

        private Border CreateHandle()
        {
            Border handle = new()
            {
                WidthRequest = GetResource<double>("M3ActionSheetHandleWidth"),
                HeightRequest = GetResource<double>("M3ActionSheetHandleHeight"),
                HorizontalOptions = LayoutOptions.Center,
                StrokeThickness = GetResource<double>("M3StrokeNone"),
                StrokeShape = new RoundRectangle
                {
                    CornerRadius = new CornerRadius(GetResource<double>("ShapeFull")),
                },
            };
            SetThemeColor(handle, BackgroundColorProperty, "M3LightOutlineVariant", "M3DarkOutlineVariant");
            return handle;
        }

        private Label CreateTitle(string title)
        {
            Label label = new()
            {
                Text = title,
                Style = GetResource<Style>("M3PanelTitle"),
                Margin = GetResource<Thickness>("M3ActionSheetTitleMargin"),
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
                RowCornerRadius = GetResource<double>("ShapeExtraLarge"),
                RowPadding = GetResource<Thickness>("M3ActionSheetRowPadding"),
                RowMinHeight = GetResource<double>("M3ActionSheetRowMinHeight"),
                IconFrameSize = GetResource<double>("M3ActionSheetRowIconFrameSize"),
                IconSize = GetResource<double>("M3ActionSheetRowIconSize"),
                IconFrameBorderWidth = GetResource<double>("M3StrokeThin"),
                TextFontSize = GetResource<double>("M3LabelLargeFontSize"),
                TextLineHeight = GetResource<double>("M3LabelLargeLineHeight"),
                ContentSpacing = GetResource<double>("Space12"),
                PressedOpacityMultiplier = GetResource<double>("M3InteractionPressedOpacityFactor"),
                DisabledOpacity = GetResource<double>("M3InteractionDisabledOpacity"),
            };

            SemanticProperties.SetDescription(row, displayLabel);
            SetThemeColor(
                row,
                ActionSheetItemView.RowBackgroundColorProperty,
                "M3LightSurfaceContainerLowest",
                "M3DarkSurfaceContainerLow");
            SetThemeColor(
                row,
                ActionSheetItemView.PressedRowBackgroundColorProperty,
                "M3LightSurfaceContainerHigh",
                "M3DarkSurfaceContainerHigh");
            SetThemeColor(
                row,
                ActionSheetItemView.IconFrameBackgroundColorProperty,
                "M3LightSurfaceContainer",
                "M3DarkSurfaceContainer");
            SetThemeColor(
                row,
                ActionSheetItemView.IconFrameBorderColorProperty,
                "M3LightOutlineVariant",
                "M3DarkOutlineVariant");

            if (isDestructive)
            {
                SetThemeColor(row, ActionSheetItemView.TextColorProperty, "M3LightError", "M3DarkError");
                SetThemeColor(row, ActionSheetItemView.IconColorProperty, "M3LightError", "M3DarkError");
            }
            else
            {
                SetThemeColor(row, ActionSheetItemView.TextColorProperty, "M3LightOnSurface", "M3DarkOnSurface");
                row.IconColor = GetResource<Color>("M3Accent");
            }

            return row;
        }

        private BoxView CreateDivider()
        {
            BoxView divider = new()
            {
                HeightRequest = GetResource<double>("M3StrokeThin"),
                Margin = new Thickness(GetResource<double>("Space8"), GetResource<double>("Space4")),
            };
            SetThemeColor(divider, BoxView.ColorProperty, "M3LightOutlineVariant", "M3DarkOutlineVariant");
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

        private static void SetThemeColor(
            BindableObject bindable,
            BindableProperty property,
            string lightResourceKey,
            string darkResourceKey)
        {
            bindable.SetAppThemeColor(
                property,
                GetResource<Color>(lightResourceKey),
                GetResource<Color>(darkResourceKey));
        }

        private static T GetResource<T>(string key)
        {
            ResourceDictionary? resources = Application.Current?.Resources;
            if (resources?.TryGetValue(key, out object value) == true && value is T typedValue)
            {
                return typedValue;
            }

            throw new InvalidOperationException($"Material action sheet resource '{key}' was not found.");
        }
    }
}
