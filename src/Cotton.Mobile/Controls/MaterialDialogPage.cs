// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Microsoft.Maui.ApplicationModel;
using System.Windows.Input;

namespace Cotton.Mobile.Controls
{
    public class MaterialDialogPage : ContentPage
    {
        private const string DefaultPageStyleResourceKey = "M3ModalPage";

        public static readonly BindableProperty PageStyleResourceKeyProperty = BindableProperty.Create(
            nameof(PageStyleResourceKey),
            typeof(string),
            typeof(MaterialDialogPage),
            DefaultPageStyleResourceKey,
            propertyChanged: OnPageStyleResourceKeyChanged);

        private readonly TaskCompletionSource<string?> _completion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly BoxView _scrim;
        private readonly Border _dialog;
        private readonly OutlinedInputField? _promptEntry;
        private bool _hasPresented;
        private bool _isCompleting;

        private MaterialDialogPage(
            string title,
            string message,
            string primaryAction,
            string? secondaryAction,
            string? promptInitialValue,
            int promptMaxLength)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(title);
            ArgumentException.ThrowIfNullOrWhiteSpace(primaryAction);

            Shell.SetNavBarIsVisible(this, false);
            NavigationPage.SetHasNavigationBar(this, false);
            ApplyPageStyle();
            _scrim = CreateScrim();
            _dialog = CreateDialogSurface();
            PrepareInitialMotionState();
            _promptEntry = promptInitialValue is null
                ? null
                : CreatePromptEntry(message, promptInitialValue, promptMaxLength);
            Content = CreateContent(title, message, primaryAction, secondaryAction);
        }

        public static MaterialDialogPage Alert(string title, string message, string cancel)
        {
            return new MaterialDialogPage(title, message, cancel, null, null, -1);
        }

        public static MaterialDialogPage Confirmation(string title, string message, string accept, string cancel)
        {
            return new MaterialDialogPage(title, message, accept, cancel, null, -1);
        }

        public static MaterialDialogPage Prompt(
            string title,
            string message,
            string accept,
            string cancel,
            string? initialValue,
            int maxLength)
        {
            return new MaterialDialogPage(title, message, accept, cancel, initialValue ?? string.Empty, maxLength);
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
            MaterialDialogPage page = (MaterialDialogPage)bindable;
            page.ApplyPageStyle();
        }

        private void ApplyPageStyle()
        {
            string pageStyleResourceKey = MaterialResources.ResolveStyleResourceKey(
                PageStyleResourceKey,
                DefaultPageStyleResourceKey);

            SetDynamicResource(StyleProperty, pageStyleResourceKey);
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await PresentAsync();
                if (_promptEntry is not null && !_isCompleting)
                {
                    _promptEntry.FocusInput();
                }
            });
        }

        protected override bool OnBackButtonPressed()
        {
            MainThread.BeginInvokeOnMainThread(async () => await CompleteAsync(null));
            return true;
        }

        private Grid CreateContent(
            string title,
            string message,
            string primaryAction,
            string? secondaryAction)
        {
            Grid root = new()
            {
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Star },
                },
            };

            root.Add(_scrim, 0, 0);

            VerticalStackLayout stack = ApplyStyle(new VerticalStackLayout(), "M3DialogStack");
            stack.Add(CreateTitle(title));

            if (ShouldShowMessage(message, isPrompt: _promptEntry is not null))
            {
                stack.Add(CreateMessage(message));
            }

            if (_promptEntry is not null)
            {
                stack.Add(_promptEntry);
            }

            stack.Add(CreateButtonRow(primaryAction, secondaryAction));
            _dialog.Content = stack;
            root.Add(_dialog, 0, 0);
            return root;
        }

        private BoxView CreateScrim()
        {
            BoxView scrim = ApplyStyle(new BoxView(), "M3ModalScrim");
            scrim.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = CreateDismissCommand((string?)null),
            });
            return scrim;
        }

        private Border CreateDialogSurface()
        {
            return ApplyStyle(new Border(), "M3DialogSurface");
        }

        private static Label CreateTitle(string title)
        {
            return ApplyStyle(new Label
            {
                Text = title,
            }, "M3DialogTitle");
        }

        private static Label CreateMessage(string message)
        {
            return ApplyStyle(new Label
            {
                Text = message,
            }, "M3DialogMessage");
        }

        private OutlinedInputField CreatePromptEntry(string message, string initialValue, int maxLength)
        {
            OutlinedInputField field = new()
            {
                Text = initialValue,
                Placeholder = ShouldShowMessage(message, isPrompt: true) ? string.Empty : message,
                IconData = IconPathData.Edit,
                ReturnType = ReturnType.Done,
                ClearButtonVisibility = ClearButtonVisibility.WhileEditing,
                ReturnCommand = CreateDismissCommand(() => _promptEntry?.Text ?? string.Empty),
                SemanticHint = message,
            };
            if (maxLength >= 0)
            {
                field.MaxLength = maxLength;
            }

            return field;
        }

        private HorizontalStackLayout CreateButtonRow(string primaryAction, string? secondaryAction)
        {
            HorizontalStackLayout row = ApplyStyle(new HorizontalStackLayout(), "M3DialogButtonRow");

            if (!string.IsNullOrWhiteSpace(secondaryAction))
            {
                row.Add(CreateSecondaryButton(secondaryAction));
            }

            row.Add(CreatePrimaryButton(primaryAction));
            return row;
        }

        private TextAction CreateSecondaryButton(string text)
        {
            return ApplyStyle(new TextAction
            {
                Text = text,
                Command = CreateDismissCommand((string?)null),
            }, "M3DialogTextAction");
        }

        private FilledButton CreatePrimaryButton(string text)
        {
            return ApplyStyle(new FilledButton
            {
                Text = text,
                Command = CreateDismissCommand(CreatePrimaryResult),
            }, "M3DialogFilledButton");
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

        private ICommand CreateDismissCommand(Func<string?> resultFactory)
        {
            return new Command(async () => await CompleteAsync(resultFactory()));
        }

        private string? CreatePrimaryResult()
        {
            return _promptEntry is null ? string.Empty : _promptEntry.Text ?? string.Empty;
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
                await DismissAndPopBestEffortAsync();
            }
            finally
            {
                _completion.TrySetResult(result);
            }
        }

        private async Task DismissAndPopBestEffortAsync()
        {
            try
            {
                await DismissAsync();
            }
            catch (Exception exception)
            {
                System.Diagnostics.Debug.WriteLine($"Material dialog dismiss animation failed: {exception}");
            }

            try
            {
                if (Navigation.ModalStack.Contains(this))
                {
                    await Navigation.PopModalAsync(animated: false);
                }
            }
            catch (Exception exception)
            {
                System.Diagnostics.Debug.WriteLine($"Material dialog modal pop failed: {exception}");
            }
        }

        private void PrepareInitialMotionState()
        {
            _scrim.Opacity = MaterialMotion.Value("M3MotionHiddenOpacity");
            _dialog.Opacity = MaterialMotion.Value("M3MotionHiddenOpacity");
            _dialog.Scale = MaterialMotion.Value("M3MotionDialogInitialScale");
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
                _dialog.FadeToAsync(MaterialMotion.Value("M3MotionVisibleOpacity"), duration, Easing.CubicOut),
                _dialog.ScaleToAsync(MaterialMotion.Value("M3InteractionRestScale"), duration, Easing.CubicOut));
        }

        private async Task DismissAsync()
        {
            uint duration = MaterialMotion.Duration("M3MotionModalExitDuration");
            await Task.WhenAll(
                _scrim.FadeToAsync(MaterialMotion.Value("M3MotionHiddenOpacity"), duration, Easing.CubicIn),
                _dialog.FadeToAsync(MaterialMotion.Value("M3MotionHiddenOpacity"), duration, Easing.CubicIn),
                _dialog.ScaleToAsync(MaterialMotion.Value("M3MotionDialogExitScale"), duration, Easing.CubicIn));
        }

        private static bool ShouldShowMessage(string message, bool isPrompt)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return false;
            }

            return !isPrompt || message.TrimEnd().EndsWith(".", StringComparison.Ordinal);
        }
    }
}
