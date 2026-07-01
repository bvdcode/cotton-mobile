// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Behaviors;
using Microsoft.Maui.Controls.Shapes;
using System.Windows.Input;

namespace Cotton.Mobile.Controls
{
    public class OutlinedInputField : ContentView
    {
        public static readonly BindableProperty TextProperty = BindableProperty.Create(
            nameof(Text),
            typeof(string),
            typeof(OutlinedInputField),
            string.Empty,
            BindingMode.TwoWay,
            propertyChanged: OnTextChanged);

        public static readonly BindableProperty PlaceholderProperty = BindableProperty.Create(
            nameof(Placeholder),
            typeof(string),
            typeof(OutlinedInputField),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IconDataProperty = BindableProperty.Create(
            nameof(IconData),
            typeof(Geometry),
            typeof(OutlinedInputField),
            default(Geometry),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty KeyboardProperty = BindableProperty.Create(
            nameof(Keyboard),
            typeof(Keyboard),
            typeof(OutlinedInputField),
            Keyboard.Default,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty ReturnTypeProperty = BindableProperty.Create(
            nameof(ReturnType),
            typeof(ReturnType),
            typeof(OutlinedInputField),
            ReturnType.Done,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty ReturnCommandProperty = BindableProperty.Create(
            nameof(ReturnCommand),
            typeof(ICommand),
            typeof(OutlinedInputField),
            default(ICommand),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty ClearButtonVisibilityProperty = BindableProperty.Create(
            nameof(ClearButtonVisibility),
            typeof(ClearButtonVisibility),
            typeof(OutlinedInputField),
            ClearButtonVisibility.WhileEditing,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty MaxLengthProperty = BindableProperty.Create(
            nameof(MaxLength),
            typeof(int),
            typeof(OutlinedInputField),
            -1,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty SemanticHintProperty = BindableProperty.Create(
            nameof(SemanticHint),
            typeof(string),
            typeof(OutlinedInputField),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        private readonly Border _field;
        private readonly Border _iconFrame;
        private readonly IconView _icon;
        private readonly Entry _entry;

        public OutlinedInputField()
        {
            _icon = new IconView();
            _icon.SetDynamicResource(StyleProperty, "M3InputIcon");

            _iconFrame = new Border
            {
                Content = _icon,
            };
            _iconFrame.SetDynamicResource(StyleProperty, "M3InputIconFrame");

            _entry = new Entry();
            _entry.TextChanged += OnEntryTextChanged;

            Grid grid = new()
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Star },
                },
            };
            grid.SetDynamicResource(StyleProperty, "M3InputGrid");
            grid.Add(_iconFrame, 0);
            grid.Add(_entry, 1);

            _field = new Border
            {
                Content = grid,
            };
            _field.SetDynamicResource(StyleProperty, "M3OutlinedInputField");

            _entry.Behaviors.Add(new FocusedInputChromeBehavior
            {
                Field = _field,
                LeadingIconFrame = _iconFrame,
                LeadingIcon = _icon,
            });

            Content = _field;
            UpdateVisualState();
        }

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public string Placeholder
        {
            get => (string)GetValue(PlaceholderProperty);
            set => SetValue(PlaceholderProperty, value);
        }

        public Geometry? IconData
        {
            get => (Geometry?)GetValue(IconDataProperty);
            set => SetValue(IconDataProperty, value);
        }

        public Keyboard Keyboard
        {
            get => (Keyboard)GetValue(KeyboardProperty);
            set => SetValue(KeyboardProperty, value);
        }

        public ReturnType ReturnType
        {
            get => (ReturnType)GetValue(ReturnTypeProperty);
            set => SetValue(ReturnTypeProperty, value);
        }

        public ICommand? ReturnCommand
        {
            get => (ICommand?)GetValue(ReturnCommandProperty);
            set => SetValue(ReturnCommandProperty, value);
        }

        public ClearButtonVisibility ClearButtonVisibility
        {
            get => (ClearButtonVisibility)GetValue(ClearButtonVisibilityProperty);
            set => SetValue(ClearButtonVisibilityProperty, value);
        }

        public int MaxLength
        {
            get => (int)GetValue(MaxLengthProperty);
            set => SetValue(MaxLengthProperty, value);
        }

        public string SemanticHint
        {
            get => (string)GetValue(SemanticHintProperty);
            set => SetValue(SemanticHintProperty, value);
        }

        public bool FocusInput()
        {
            return _entry.Focus();
        }

        public void UnfocusInput()
        {
            _entry.Unfocus();
        }

        private static void OnTextChanged(BindableObject bindable, object oldValue, object newValue)
        {
            OutlinedInputField field = (OutlinedInputField)bindable;
            field.UpdateText((string?)newValue ?? string.Empty);
        }

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            OutlinedInputField field = (OutlinedInputField)bindable;
            field.UpdateVisualState();
        }

        private void OnEntryTextChanged(object? sender, TextChangedEventArgs e)
        {
            string text = e.NewTextValue ?? string.Empty;
            if (!string.Equals(Text, text, StringComparison.Ordinal))
            {
                Text = text;
            }
        }

        private void UpdateText(string text)
        {
            if (!string.Equals(_entry.Text, text, StringComparison.Ordinal))
            {
                _entry.Text = text;
            }
        }

        private void UpdateVisualState()
        {
            _entry.Placeholder = Placeholder;
            _entry.Keyboard = Keyboard ?? Keyboard.Default;
            _entry.ReturnType = ReturnType;
            _entry.ReturnCommand = ReturnCommand;
            _entry.ClearButtonVisibility = ClearButtonVisibility;
            _entry.MaxLength = MaxLength >= 0 ? MaxLength : int.MaxValue;
            _entry.IsEnabled = IsEnabled;
            _icon.IconData = IconData;
            SemanticProperties.SetHint(_entry, SemanticHint);
        }

        protected override void OnPropertyChanged(string? propertyName = null)
        {
            base.OnPropertyChanged(propertyName);

            if (string.Equals(propertyName, nameof(IsEnabled), StringComparison.Ordinal))
            {
                UpdateVisualState();
            }
        }
    }
}
