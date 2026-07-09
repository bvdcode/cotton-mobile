// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

using System.Windows.Input;

namespace Cotton.Mobile.Controls
{
    public class AuthSignInPanelView : ContentView
    {
        private const string DefaultCardStyleResourceKey = "M3AuthPanel";
        private const string DefaultFlatCardStyleResourceKey = "M3AuthPanelFlat";
        private const string DefaultActionRowStyleResourceKey = "M3AuthActionRow";
        private const string DefaultFormStackStyleResourceKey = "M3AuthFormStack";
        private const string DefaultStatusTextStyleResourceKey = "M3AuthStatus";
        private const string DefaultButtonStyleResourceKey = "M3AuthFilledButton";
        private const string DefaultServerActionButtonStyleResourceKey = "M3AuthServerActionButton";
        private const string ChangeServerActionText = "Change server";
        private const string UseDefaultServerActionText = "Use default server";
        private const string PanelOpacityAnimationName = "M3AuthSignInPanelOpacity";

        public static readonly BindableProperty PlaceholderProperty = BindableProperty.Create(
            nameof(Placeholder),
            typeof(string),
            typeof(AuthSignInPanelView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty InstanceUrlProperty = BindableProperty.Create(
            nameof(InstanceUrl),
            typeof(string),
            typeof(AuthSignInPanelView),
            string.Empty,
            BindingMode.TwoWay,
            propertyChanged: OnInstanceUrlChanged);

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
        private readonly Grid _actionRow;
        private readonly ContentCardView _card;
        private readonly VerticalStackLayout _formStack;
        private readonly IconButton _serverActionButton;
        private readonly ScreenStatusView _status;
        private readonly ICommand _toggleServerFieldCommand;
        private readonly OutlinedInputField _urlField;
        private bool _hasAppliedPanelVisibility;
        private bool _isServerFieldExpanded;

        public AuthSignInPanelView()
        {
            _toggleServerFieldCommand = new Command(ToggleServerField);

            _urlField = new OutlinedInputField
            {
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

            _serverActionButton = new IconButton
            {
                Command = _toggleServerFieldCommand,
            };
            _serverActionButton.SetDynamicResource(StyleProperty, DefaultServerActionButtonStyleResourceKey);

            _actionRow = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto },
                },
            };
            _actionRow.SetDynamicResource(StyleProperty, DefaultActionRowStyleResourceKey);
            _actionRow.Add(_button, 0, 0);
            _actionRow.Add(_serverActionButton, 1, 0);

            _formStack = new VerticalStackLayout
            {
                Children =
                {
                    _urlField,
                    _status,
                    _actionRow,
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
            UpdateServerFieldState();
            UpdatePanelVisibility(animatePanelVisibility: false);
            UpdateInputTransparency();
        }

        public string InstanceUrl
        {
            get => (string)GetValue(InstanceUrlProperty);
            set => SetValue(InstanceUrlProperty, value);
        }

        public string Placeholder
        {
            get => (string)GetValue(PlaceholderProperty);
            set => SetValue(PlaceholderProperty, value);
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

        private static void OnInstanceUrlChanged(BindableObject bindable, object oldValue, object newValue)
        {
            AuthSignInPanelView view = (AuthSignInPanelView)bindable;
            view.UpdateServerFieldState();
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
            _urlField.Placeholder = Placeholder ?? string.Empty;
            _urlField.IsEnabled = IsInputEnabled;
            _urlField.ReturnCommand = ConnectCommand;

            _status.Text = Status ?? string.Empty;
            _status.IsStatusVisible = IsStatusVisible;

            _button.Command = ConnectCommand;
            _button.IsEnabled = IsInputEnabled;
            _serverActionButton.IsEnabled = IsInputEnabled;
            UpdateServerFieldState();
            UpdateInputTransparency();
        }

        private void ToggleServerField()
        {
            if (!IsInputEnabled)
            {
                return;
            }

            if (IsServerFieldVisible())
            {
                _isServerFieldExpanded = false;
                InstanceUrl = string.Empty;
                _urlField.UnfocusInput();
                UpdateServerFieldState();
                return;
            }

            _isServerFieldExpanded = true;
            UpdateServerFieldState();
        }

        private void UpdateServerFieldState()
        {
            bool isServerFieldVisible = IsServerFieldVisible();
            _urlField.IsFieldVisible = isServerFieldVisible;
            _serverActionButton.IconData = isServerFieldVisible ? IconPathData.Close : IconPathData.Edit;
            SemanticProperties.SetDescription(
                _serverActionButton,
                isServerFieldVisible ? UseDefaultServerActionText : ChangeServerActionText);
            SemanticProperties.SetHint(
                _serverActionButton,
                "Choose the default Cotton Cloud server or enter a custom address.");
            _card.CardStyleResourceKey = ShouldUseFramedPanel()
                ? DefaultCardStyleResourceKey
                : DefaultFlatCardStyleResourceKey;
        }

        private bool IsServerFieldVisible()
        {
            return _isServerFieldExpanded || !string.IsNullOrWhiteSpace(InstanceUrl);
        }

        private bool ShouldUseFramedPanel()
        {
            return IsServerFieldVisible() || IsStatusVisible;
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
            else
            {
                UpdateInputTransparency();
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
            UpdateInputTransparency();
        }

        private void UpdateInputTransparency()
        {
            InputTransparent = !IsVisible || !IsPanelVisible || !IsInputEnabled;
        }
    }
}
