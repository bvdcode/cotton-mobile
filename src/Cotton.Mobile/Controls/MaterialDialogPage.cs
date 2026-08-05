// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Microsoft.Maui.ApplicationModel;

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
            MaterialDialogContent dialogContent = new(
                title,
                message,
                primaryAction,
                secondaryAction,
                promptInitialValue,
                promptMaxLength,
                CompleteAsync);
            _scrim = dialogContent.Scrim;
            _dialog = dialogContent.Dialog;
            _promptEntry = dialogContent.PromptEntry;
            PrepareInitialMotionState();
            Content = dialogContent.Root;
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

    }
}
