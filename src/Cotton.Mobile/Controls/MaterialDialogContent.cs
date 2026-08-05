// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Controls
{
    internal class MaterialDialogContent
    {
        private readonly Func<string?, Task> _completeAsync;

        public MaterialDialogContent(
            string title,
            string message,
            string primaryAction,
            string? secondaryAction,
            string? promptInitialValue,
            int promptMaxLength,
            Func<string?, Task> completeAsync)
        {
            _completeAsync = completeAsync;
            PromptEntry = promptInitialValue is null
                ? null
                : CreatePromptEntry(message, promptInitialValue, promptMaxLength);
            Scrim = CreateScrim();
            Dialog = ApplyStyle(new Border(), "M3DialogSurface");
            Root = CreateRoot(title, message, primaryAction, secondaryAction);
        }

        public Grid Root { get; }

        public BoxView Scrim { get; }

        public Border Dialog { get; }

        public OutlinedInputField? PromptEntry { get; }

        private Grid CreateRoot(
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
            root.Add(Scrim, 0, 0);

            VerticalStackLayout stack = ApplyStyle(new VerticalStackLayout(), "M3DialogStack");
            stack.Add(CreateLabel(title, "M3DialogTitle"));
            if (ShouldShowMessage(message, PromptEntry is not null))
            {
                stack.Add(CreateLabel(message, "M3DialogMessage"));
            }

            if (PromptEntry is not null)
            {
                stack.Add(PromptEntry);
            }

            stack.Add(CreateButtonRow(primaryAction, secondaryAction));
            Dialog.Content = stack;
            root.Add(Dialog, 0, 0);
            return root;
        }

        private BoxView CreateScrim()
        {
            BoxView scrim = ApplyStyle(new BoxView(), "M3ModalScrim");
            scrim.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = CreateDismissCommand(() => null),
            });
            return scrim;
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
                SemanticHint = message,
            };
            field.ReturnCommand = CreateDismissCommand(() => field.Text ?? string.Empty);
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
                row.Add(ApplyStyle(new TextAction
                {
                    Text = secondaryAction,
                    Command = CreateDismissCommand(() => null),
                }, "M3DialogTextAction"));
            }

            row.Add(ApplyStyle(new FilledButton
            {
                Text = primaryAction,
                Command = CreateDismissCommand(CreatePrimaryResult),
            }, "M3DialogFilledButton"));
            return row;
        }

        private string CreatePrimaryResult()
        {
            return PromptEntry?.Text ?? string.Empty;
        }

        private Command CreateDismissCommand(Func<string?> resultFactory)
        {
            return new Command(async () => await _completeAsync(resultFactory()));
        }

        private static Label CreateLabel(string text, string styleResourceKey)
        {
            return ApplyStyle(new Label
            {
                Text = text,
            }, styleResourceKey);
        }

        private static T ApplyStyle<T>(T view, string styleResourceKey)
            where T : VisualElement
        {
            view.SetDynamicResource(VisualElement.StyleProperty, styleResourceKey);
            return view;
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
