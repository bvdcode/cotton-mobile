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
        private const string DefaultPageStyleResourceKey = "M3ModalPage";

        public static readonly BindableProperty PageStyleResourceKeyProperty = BindableProperty.Create(
            nameof(PageStyleResourceKey),
            typeof(string),
            typeof(MaterialActionSheetPage),
            DefaultPageStyleResourceKey,
            propertyChanged: OnPageStyleResourceKeyChanged);

        private readonly TaskCompletionSource<string?> _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly BoxView _scrim;
        private readonly Border _sheet;
        private bool _hasPresented;
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
            ApplyPageStyle();
            _scrim = CreateScrim();
            _sheet = CreateSheetSurface();
            PrepareInitialMotionState();
            Content = CreateContent(title, cancel, destruction, buttons);
        }

        public string PageStyleResourceKey
        {
            get => (string)GetValue(PageStyleResourceKeyProperty);
            set => SetValue(PageStyleResourceKeyProperty, value);
        }

        public Task<string?> WaitForResultAsync()
        {
            return _completion.Task;
        }

        private static void OnPageStyleResourceKeyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            MaterialActionSheetPage page = (MaterialActionSheetPage)bindable;
            page.ApplyPageStyle();
        }

        private void ApplyPageStyle()
        {
            string pageStyleResourceKey = MaterialResources.ResolveStyleResourceKey(
                PageStyleResourceKey,
                DefaultPageStyleResourceKey);

            SetDynamicResource(StyleProperty, pageStyleResourceKey);
        }

        protected override bool OnBackButtonPressed()
        {
            MainThread.BeginInvokeOnMainThread(async () => await CompleteAsync(null));
            return true;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            await PresentAsync();
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

            root.Add(_scrim, 0, 0);
            Grid.SetRowSpan(_scrim, 2);

            VerticalStackLayout stack = ApplyStyle(new VerticalStackLayout(), "M3ActionSheetStack");

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
            _sheet.Content = stack;

            root.Add(_sheet, 0, 1);
            return root;
        }

        private BoxView CreateScrim()
        {
            BoxView scrim = ApplyStyle(new BoxView(), "M3ModalScrim");
            scrim.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = CreateDismissCommand(null),
            });
            return scrim;
        }

        private Border CreateSheetSurface()
        {
            return ApplyStyle(new Border(), "M3ActionSheetSurface");
        }

        private Border CreateHandle()
        {
            return ApplyStyle(new Border(), "M3ActionSheetHandle");
        }

        private Label CreateTitle(string title)
        {
            Label label = ApplyStyle(new Label
            {
                Text = title,
            }, "M3ActionSheetTitle");
            return label;
        }

        private ActionSheetItemView CreateActionRow(
            string actionLabel,
            string? result,
            bool isDestructive,
            bool isCancel)
        {
            bool isSelected = CottonActionSheetCurrentLabel.TryCreateDisplayLabel(actionLabel, out string displayLabel);
            string styleResourceKey = isDestructive
                ? "M3ActionSheetDestructiveItem"
                : "M3ActionSheetItem";
            ActionSheetItemView row = ApplyStyle(new ActionSheetItemView
            {
                Text = displayLabel,
                IconData = ResolveIconData(displayLabel, isDestructive, isCancel),
                IsSelected = isSelected,
                Command = CreateDismissCommand(result),
            }, styleResourceKey);

            SemanticProperties.SetDescription(row, displayLabel);
            return row;
        }

        private BoxView CreateDivider()
        {
            return ApplyStyle(new BoxView(), "M3ActionSheetDivider");
        }

        private static T ApplyStyle<T>(T view, string styleResourceKey)
            where T : VisualElement
        {
            view.SetDynamicResource(StyleProperty, styleResourceKey);
            return view;
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
                await DismissAsync();
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

        private void PrepareInitialMotionState()
        {
            _scrim.Opacity = MaterialMotion.Value("M3MotionHiddenOpacity");
            _sheet.TranslationY = MaterialMotion.Value("M3MotionActionSheetInitialOffset");
        }

        private async Task PresentAsync()
        {
            if (_hasPresented || _isCompleting)
            {
                return;
            }

            _hasPresented = true;
            uint duration = MaterialMotion.Duration("M3MotionModalEnterDuration");
            await Task.WhenAll(
                _scrim.FadeToAsync(MaterialMotion.Value("M3MotionVisibleOpacity"), duration, Easing.CubicOut),
                _sheet.TranslateToAsync(
                    MaterialMotion.Value("M3MotionRestOffset"),
                    MaterialMotion.Value("M3MotionRestOffset"),
                    duration,
                    Easing.CubicOut));
        }

        private async Task DismissAsync()
        {
            uint duration = MaterialMotion.Duration("M3MotionModalExitDuration");
            await Task.WhenAll(
                _scrim.FadeToAsync(MaterialMotion.Value("M3MotionHiddenOpacity"), duration, Easing.CubicIn),
                _sheet.TranslateToAsync(
                    MaterialMotion.Value("M3MotionRestOffset"),
                    MaterialMotion.Value("M3MotionActionSheetInitialOffset"),
                    duration,
                    Easing.CubicIn));
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
