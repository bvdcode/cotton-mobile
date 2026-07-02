// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

using System.Windows.Input;

namespace Cotton.Mobile.Controls
{
    public class AuthSignInPanelView : ContentView
    {
        private const string DefaultCardStyleResourceKey = "M3AuthPanel";
        private const string DefaultFormStackStyleResourceKey = "M3AuthFormStack";
        private const string DefaultStatusTextStyleResourceKey = "M3AuthStatus";
        private const string DefaultButtonStyleResourceKey = "M3AuthFilledButton";

        public static readonly BindableProperty InstanceUrlProperty = BindableProperty.Create(
            nameof(InstanceUrl),
            typeof(string),
            typeof(AuthSignInPanelView),
            string.Empty,
            BindingMode.TwoWay);

        public static readonly BindableProperty StatusProperty = BindableProperty.Create(
            nameof(Status),
            typeof(string),
            typeof(AuthSignInPanelView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsStatusVisibleProperty = BindableProperty.Create(
            nameof(IsStatusVisible),
            typeof(bool),
            typeof(AuthSignInPanelView),
            false,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsInputEnabledProperty = BindableProperty.Create(
            nameof(IsInputEnabled),
            typeof(bool),
            typeof(AuthSignInPanelView),
            true,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty ConnectCommandProperty = BindableProperty.Create(
            nameof(ConnectCommand),
            typeof(ICommand),
            typeof(AuthSignInPanelView),
            default(ICommand),
            propertyChanged: OnVisualPropertyChanged);

        private readonly FilledButton _button;
        private readonly ContentCardView _card;
        private readonly VerticalStackLayout _formStack;
        private readonly ScreenStatusView _status;
        private readonly OutlinedInputField _urlField;

        public AuthSignInPanelView()
        {
            _urlField = new OutlinedInputField
            {
                Placeholder = "https://app.cottoncloud.dev/",
                IconData = IconPathData.Cloud,
                Keyboard = Keyboard.Url,
                ReturnType = ReturnType.Go,
                ClearButtonVisibility = ClearButtonVisibility.WhileEditing,
                SemanticHint = "Cotton Cloud address",
            };
            _urlField.SetBinding(OutlinedInputField.TextProperty, new Binding(nameof(InstanceUrl), BindingMode.TwoWay, source: this));

            _status = new ScreenStatusView
            {
                TextStyleResourceKey = DefaultStatusTextStyleResourceKey,
            };

            _button = new FilledButton
            {
                Text = "Connect",
            };
            _button.SetDynamicResource(StyleProperty, DefaultButtonStyleResourceKey);

            _formStack = new VerticalStackLayout
            {
                Children =
                {
                    _urlField,
                    _status,
                    _button,
                },
            };
            _formStack.SetDynamicResource(StyleProperty, DefaultFormStackStyleResourceKey);

            _card = new ContentCardView
            {
                CardStyleResourceKey = DefaultCardStyleResourceKey,
                BodyContent = _formStack,
            };

            Content = _card;
            UpdateVisualState();
        }

        public string InstanceUrl
        {
            get => (string)GetValue(InstanceUrlProperty);
            set => SetValue(InstanceUrlProperty, value);
        }

        public string Status
        {
            get => (string)GetValue(StatusProperty);
            set => SetValue(StatusProperty, value);
        }

        public bool IsStatusVisible
        {
            get => (bool)GetValue(IsStatusVisibleProperty);
            set => SetValue(IsStatusVisibleProperty, value);
        }

        public bool IsInputEnabled
        {
            get => (bool)GetValue(IsInputEnabledProperty);
            set => SetValue(IsInputEnabledProperty, value);
        }

        public ICommand? ConnectCommand
        {
            get => (ICommand?)GetValue(ConnectCommandProperty);
            set => SetValue(ConnectCommandProperty, value);
        }

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            AuthSignInPanelView view = (AuthSignInPanelView)bindable;
            view.UpdateVisualState();
        }

        private void UpdateVisualState()
        {
            _urlField.IsEnabled = IsInputEnabled;
            _urlField.ReturnCommand = ConnectCommand;

            _status.Text = Status ?? string.Empty;
            _status.IsStatusVisible = IsStatusVisible;

            _button.Command = ConnectCommand;
            _button.IsEnabled = IsInputEnabled;
        }
    }
}
