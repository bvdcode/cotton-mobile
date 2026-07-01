// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Behaviors;
using Microsoft.Maui.ApplicationModel;
using System.Windows.Input;

namespace Cotton.Mobile.Controls
{
    public class MaterialDialogPage : ContentPage
    {
        private readonly TaskCompletionSource<string?> _completion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly Entry? _promptEntry;
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
            Style = MaterialResources.Get<Style>("M3ModalPage");
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

        public Task<string?> WaitForResultAsync()
        {
            return _completion.Task;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            if (_promptEntry is not null)
            {
                MainThread.BeginInvokeOnMainThread(() => _promptEntry.Focus());
            }
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

            BoxView scrim = new()
            {
                Style = MaterialResources.Get<Style>("M3ModalScrim"),
            };
            scrim.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = CreateDismissCommand((string?)null),
            });
            root.Add(scrim, 0, 0);

            Border dialog = CreateDialogSurface();
            VerticalStackLayout stack = new()
            {
                Style = MaterialResources.Get<Style>("M3DialogStack"),
            };
            stack.Add(CreateTitle(title));

            if (ShouldShowMessage(message, isPrompt: _promptEntry is not null))
            {
                stack.Add(CreateMessage(message));
            }

            if (_promptEntry is not null)
            {
                stack.Add(CreatePromptField(_promptEntry));
            }

            stack.Add(CreateButtonRow(primaryAction, secondaryAction));
            dialog.Content = stack;
            root.Add(dialog, 0, 0);
            return root;
        }

        private Border CreateDialogSurface()
        {
            return new Border
            {
                Style = MaterialResources.Get<Style>("M3DialogSurface"),
            };
        }

        private static Label CreateTitle(string title)
        {
            return new Label
            {
                Text = title,
                Style = MaterialResources.Get<Style>("M3DialogTitle"),
            };
        }

        private static Label CreateMessage(string message)
        {
            return new Label
            {
                Text = message,
                Style = MaterialResources.Get<Style>("M3DialogMessage"),
            };
        }

        private Border CreatePromptField(Entry entry)
        {
            Border field = new()
            {
                Style = MaterialResources.Get<Style>("M3OutlinedInputField"),
                Content = entry,
            };
            entry.Behaviors.Add(new FocusedInputChromeBehavior
            {
                Field = field,
            });
            return field;
        }

        private Entry CreatePromptEntry(string message, string initialValue, int maxLength)
        {
            Entry entry = new()
            {
                Text = initialValue,
                Placeholder = ShouldShowMessage(message, isPrompt: true) ? string.Empty : message,
                ReturnType = ReturnType.Done,
                ClearButtonVisibility = ClearButtonVisibility.WhileEditing,
            };
            if (maxLength >= 0)
            {
                entry.MaxLength = maxLength;
            }

            SemanticProperties.SetHint(entry, message);
            entry.Completed += async (_, _) => await CompleteAsync(entry.Text);
            return entry;
        }

        private HorizontalStackLayout CreateButtonRow(string primaryAction, string? secondaryAction)
        {
            HorizontalStackLayout row = new()
            {
                Style = MaterialResources.Get<Style>("M3DialogButtonRow"),
            };

            if (!string.IsNullOrWhiteSpace(secondaryAction))
            {
                row.Add(CreateSecondaryButton(secondaryAction));
            }

            row.Add(CreatePrimaryButton(primaryAction));
            return row;
        }

        private TextAction CreateSecondaryButton(string text)
        {
            return new TextAction
            {
                Text = text,
                Command = CreateDismissCommand((string?)null),
                Style = MaterialResources.Get<Style>("M3DialogTextAction"),
            };
        }

        private FilledButton CreatePrimaryButton(string text)
        {
            return new FilledButton
            {
                Text = text,
                Command = CreateDismissCommand(CreatePrimaryResult),
                Style = MaterialResources.Get<Style>("M3DialogFilledButton"),
            };
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
