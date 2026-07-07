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
        private const string PanelOpacityAnimationName = "M3AuthSignInPanelOpacity";

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

        public static readonly BindableProperty IsPanelVisibleProperty = BindableProperty.Create(
            nameof(IsPanelVisible),
            typeof(bool),
            typeof(AuthSignInPanelView),
            true,
            propertyChanged: OnPanelVisiblePropertyChanged);

        private readonly FilledButton _button;
        private readonly ContentCardView _card;
        private readonly VerticalStackLayout _formStack;
        private readonly ScreenStatusView _status;
        private readonly OutlinedInputField _urlField;
        private bool _hasAppliedPanelVisibility;

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
            UpdatePanelVisibility(animatePanelVisibility: false);
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

        public bool IsPanelVisible
        {
            get => (bool)GetValue(IsPanelVisibleProperty);
            set => SetValue(IsPanelVisibleProperty, value);
        }

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            AuthSignInPanelView view = (AuthSignInPanelView)bindable;
            view.UpdateVisualState();
        }

        private static void OnPanelVisiblePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            AuthSignInPanelView view = (AuthSignInPanelView)bindable;
            view.UpdatePanelVisibility(animatePanelVisibility: true);
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

        private void UpdatePanelVisibility(bool animatePanelVisibility)
        {
            bool isPanelVisible = IsPanelVisible;
            bool shouldAnimate = animatePanelVisibility && _hasAppliedPanelVisibility;
            double targetOpacity = isPanelVisible
                ? MaterialMotion.Value("M3MotionVisibleOpacity")
                : MaterialMotion.Value("M3MotionHiddenOpacity");
            int duration = MaterialResources.Get<int>("M3MotionStatusDuration");

            if (isPanelVisible)
            {
                IsVisible = true;
            }

            MaterialMotion.UpdateDouble(
                this,
                Opacity,
                targetOpacity,
                duration,
                PanelOpacityAnimationName,
                shouldAnimate,
                opacity => Opacity = opacity,
                CompletePanelVisibility);
            _hasAppliedPanelVisibility = true;
        }

        private void CompletePanelVisibility()
        {
            IsVisible = IsPanelVisible;
        }
    }
}
