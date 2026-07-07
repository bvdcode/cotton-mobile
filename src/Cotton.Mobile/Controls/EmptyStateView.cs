// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

using System.Windows.Input;
using Microsoft.Maui.Controls.Shapes;

namespace Cotton.Mobile.Controls
{
    public class EmptyStateView : ContentView
    {
        private const string ActionIconOnlyButtonOpacityAnimationName = "M3EmptyStateActionIconOnlyOpacity";
        private const string ActionRowOpacityAnimationName = "M3EmptyStateActionRowOpacity";
        private const string BodyOpacityAnimationName = "M3EmptyStateBodyOpacity";
        private const string BusyIndicatorOpacityAnimationName = "M3EmptyStateBusyIndicatorOpacity";
        private const string DefaultActionIconButtonStyleResourceKey = "M3EmptyStateActionIconButton";
        private const string DefaultActionRowStyleResourceKey = "M3PanelActionListItemGrid";
        private const string DefaultCardStyleResourceKey = "M3EmptyStateCard";
        private const string DefaultFilledActionButtonStyleResourceKey = "M3PanelActionFilledButton";
        private const string DefaultIconFrameStyleResourceKey = "M3EmptyStateIconFrame";
        private const string FilledActionButtonOpacityAnimationName = "M3EmptyStateFilledActionOpacity";
        private const string StateOpacityAnimationName = "M3EmptyStateOpacity";

        public static readonly BindableProperty IconDataProperty = BindableProperty.Create(
            nameof(IconData),
            typeof(Geometry),
            typeof(EmptyStateView),
            default(Geometry),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty TitleProperty = BindableProperty.Create(
            nameof(Title),
            typeof(string),
            typeof(EmptyStateView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty BodyProperty = BindableProperty.Create(
            nameof(Body),
            typeof(string),
            typeof(EmptyStateView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsBodyVisibleProperty = BindableProperty.Create(
            nameof(IsBodyVisible),
            typeof(bool),
            typeof(EmptyStateView),
            true,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsBusyProperty = BindableProperty.Create(
            nameof(IsBusy),
            typeof(bool),
            typeof(EmptyStateView),
            false,
            propertyChanged: OnBusyPropertyChanged);

        public static readonly BindableProperty CardStyleResourceKeyProperty = BindableProperty.Create(
            nameof(CardStyleResourceKey),
            typeof(string),
            typeof(EmptyStateView),
            DefaultCardStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IconFrameStyleResourceKeyProperty = BindableProperty.Create(
            nameof(IconFrameStyleResourceKey),
            typeof(string),
            typeof(EmptyStateView),
            DefaultIconFrameStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty ActionTextProperty = BindableProperty.Create(
            nameof(ActionText),
            typeof(string),
            typeof(EmptyStateView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty ActionIconDataProperty = BindableProperty.Create(
            nameof(ActionIconData),
            typeof(Geometry),
            typeof(EmptyStateView),
            default(Geometry),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty ActionCommandProperty = BindableProperty.Create(
            nameof(ActionCommand),
            typeof(ICommand),
            typeof(EmptyStateView),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsActionVisibleProperty = BindableProperty.Create(
            nameof(IsActionVisible),
            typeof(bool),
            typeof(EmptyStateView),
            false,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsActionEnabledProperty = BindableProperty.Create(
            nameof(IsActionEnabled),
            typeof(bool),
            typeof(EmptyStateView),
            true,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsFilledActionProperty = BindableProperty.Create(
            nameof(IsFilledAction),
            typeof(bool),
            typeof(EmptyStateView),
            false,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty ActionSemanticDescriptionProperty = BindableProperty.Create(
            nameof(ActionSemanticDescription),
            typeof(string),
            typeof(EmptyStateView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty ActionIconButtonStyleResourceKeyProperty = BindableProperty.Create(
            nameof(ActionIconButtonStyleResourceKey),
            typeof(string),
            typeof(EmptyStateView),
            DefaultActionIconButtonStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty ActionRowStyleResourceKeyProperty = BindableProperty.Create(
            nameof(ActionRowStyleResourceKey),
            typeof(string),
            typeof(EmptyStateView),
            DefaultActionRowStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty FilledActionButtonStyleResourceKeyProperty = BindableProperty.Create(
            nameof(FilledActionButtonStyleResourceKey),
            typeof(string),
            typeof(EmptyStateView),
            DefaultFilledActionButtonStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsStateVisibleProperty = BindableProperty.Create(
            nameof(IsStateVisible),
            typeof(bool),
            typeof(EmptyStateView),
            true,
            propertyChanged: OnStateVisiblePropertyChanged);

        private readonly IconButton _actionButton;
        private readonly IconButton _actionIconOnlyButton;
        private readonly Label _actionLabel;
        private readonly Grid _actionRow;
        private readonly TouchSurfaceView _actionTouchSurface;
        private readonly Border _card;
        private readonly FilledButton _filledActionButton;
        private readonly IconView _icon;
        private readonly Border _iconFrame;
        private readonly LoadingIndicatorView _loadingIndicator;
        private readonly Label _title;
        private readonly Label _body;
        private bool _hasAppliedBusyState;
        private bool _hasAppliedContentVisibilityState;
        private bool _hasAppliedStateVisibility;

        public EmptyStateView()
        {
            _icon = new IconView();
            _icon.SetDynamicResource(StyleProperty, "M3EmptyStateIcon");

            _iconFrame = new Border
            {
                Content = _icon,
            };

            _title = new Label();
            _title.SetDynamicResource(StyleProperty, "M3EmptyTitle");

            _body = new Label();
            _body.SetDynamicResource(StyleProperty, "M3EmptyBody");

            _loadingIndicator = new LoadingIndicatorView
            {
                IsVisible = false,
                Opacity = MaterialMotion.Value("M3MotionHiddenOpacity"),
            };

            _actionLabel = new Label();
            _actionLabel.SetDynamicResource(StyleProperty, "M3ActionListItemLabel");

            _actionTouchSurface = new TouchSurfaceView();
            Grid.SetColumnSpan(_actionTouchSurface, 2);

            _actionButton = new IconButton();
            Grid.SetColumn(_actionButton, 1);

            _actionRow = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition
                    {
                        Width = GridLength.Auto,
                    },
                    new ColumnDefinition
                    {
                        Width = GridLength.Auto,
                    },
                },
                Children =
                {
                    _actionLabel,
                    _actionTouchSurface,
                    _actionButton,
                },
            };

            _actionIconOnlyButton = new IconButton();
            _filledActionButton = new FilledButton();

            VerticalStackLayout stack = new()
            {
                Children =
                {
                    _iconFrame,
                    _title,
                    _body,
                    _loadingIndicator,
                    _actionRow,
                    _actionIconOnlyButton,
                    _filledActionButton,
                },
            };
            stack.SetDynamicResource(StyleProperty, "M3EmptyStateStack");

            _card = new Border
            {
                Content = stack,
            };
            _card.SetDynamicResource(StyleProperty, "M3EmptyStateCard");

            Content = _card;
            UpdateVisualState(animateBusy: false);
            UpdateStateVisibility(animateStateVisibility: false);
        }

        public Geometry? IconData
        {
            get => (Geometry?)GetValue(IconDataProperty);
            set => SetValue(IconDataProperty, value);
        }

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public string Body
        {
            get => (string)GetValue(BodyProperty);
            set => SetValue(BodyProperty, value);
        }

        public bool IsBodyVisible
        {
            get => (bool)GetValue(IsBodyVisibleProperty);
            set => SetValue(IsBodyVisibleProperty, value);
        }

        public bool IsBusy
        {
            get => (bool)GetValue(IsBusyProperty);
            set => SetValue(IsBusyProperty, value);
        }

        public string CardStyleResourceKey
        {
            get => (string)GetValue(CardStyleResourceKeyProperty);
            set => SetValue(CardStyleResourceKeyProperty, value);
        }

        public string IconFrameStyleResourceKey
        {
            get => (string)GetValue(IconFrameStyleResourceKeyProperty);
            set => SetValue(IconFrameStyleResourceKeyProperty, value);
        }

        public string ActionText
        {
            get => (string)GetValue(ActionTextProperty);
            set => SetValue(ActionTextProperty, value);
        }

        public Geometry? ActionIconData
        {
            get => (Geometry?)GetValue(ActionIconDataProperty);
            set => SetValue(ActionIconDataProperty, value);
        }

        public ICommand? ActionCommand
        {
            get => (ICommand?)GetValue(ActionCommandProperty);
            set => SetValue(ActionCommandProperty, value);
        }

        public bool IsActionVisible
        {
            get => (bool)GetValue(IsActionVisibleProperty);
            set => SetValue(IsActionVisibleProperty, value);
        }

        public bool IsActionEnabled
        {
            get => (bool)GetValue(IsActionEnabledProperty);
            set => SetValue(IsActionEnabledProperty, value);
        }

        public bool IsFilledAction
        {
            get => (bool)GetValue(IsFilledActionProperty);
            set => SetValue(IsFilledActionProperty, value);
        }

        public string ActionSemanticDescription
        {
            get => (string)GetValue(ActionSemanticDescriptionProperty);
            set => SetValue(ActionSemanticDescriptionProperty, value);
        }

        public string ActionIconButtonStyleResourceKey
        {
            get => (string)GetValue(ActionIconButtonStyleResourceKeyProperty);
            set => SetValue(ActionIconButtonStyleResourceKeyProperty, value);
        }

        public string ActionRowStyleResourceKey
        {
            get => (string)GetValue(ActionRowStyleResourceKeyProperty);
            set => SetValue(ActionRowStyleResourceKeyProperty, value);
        }

        public string FilledActionButtonStyleResourceKey
        {
            get => (string)GetValue(FilledActionButtonStyleResourceKeyProperty);
            set => SetValue(FilledActionButtonStyleResourceKeyProperty, value);
        }

        public bool IsStateVisible
        {
            get => (bool)GetValue(IsStateVisibleProperty);
            set => SetValue(IsStateVisibleProperty, value);
        }

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            EmptyStateView emptyStateView = (EmptyStateView)bindable;
            emptyStateView.UpdateVisualState(animateBusy: false);
        }

        private static void OnBusyPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            EmptyStateView emptyStateView = (EmptyStateView)bindable;
            emptyStateView.UpdateVisualState(animateBusy: true);
        }

        private static void OnStateVisiblePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            EmptyStateView emptyStateView = (EmptyStateView)bindable;
            emptyStateView.UpdateStateVisibility(animateStateVisibility: true);
        }

        private void UpdateVisualState(bool animateBusy)
        {
            bool shouldAnimateContentVisibility = _hasAppliedContentVisibilityState;
            string title = Title ?? string.Empty;
            string body = Body ?? string.Empty;
            string actionText = ActionText ?? string.Empty;
            string actionSemanticDescription = ActionSemanticDescription ?? string.Empty;
            string cardStyleResourceKey = MaterialResources.ResolveStyleResourceKey(
                CardStyleResourceKey,
                DefaultCardStyleResourceKey);
            string iconFrameStyleResourceKey = MaterialResources.ResolveStyleResourceKey(
                IconFrameStyleResourceKey,
                DefaultIconFrameStyleResourceKey);
            string actionRowStyleResourceKey = MaterialResources.ResolveStyleResourceKey(
                ActionRowStyleResourceKey,
                DefaultActionRowStyleResourceKey);
            string actionIconButtonStyleResourceKey = MaterialResources.ResolveStyleResourceKey(
                ActionIconButtonStyleResourceKey,
                DefaultActionIconButtonStyleResourceKey);
            string filledActionButtonStyleResourceKey = MaterialResources.ResolveStyleResourceKey(
                FilledActionButtonStyleResourceKey,
                DefaultFilledActionButtonStyleResourceKey);
            bool isBodyVisible = IsBodyVisible && !string.IsNullOrWhiteSpace(body);
            bool isFilledActionVisible = IsActionVisible && IsFilledAction && !string.IsNullOrWhiteSpace(actionText);
            bool isActionTextVisible = IsActionVisible && !IsFilledAction && !string.IsNullOrWhiteSpace(actionText);
            bool isIconOnlyActionVisible = IsActionVisible && !IsFilledAction && string.IsNullOrWhiteSpace(actionText);
            ICommand? actionCommand = ActionCommand;

            _icon.IconData = IconData;
            _title.Text = title;
            _body.Text = body;
            UpdateElementVisibility(_body, isBodyVisible, BodyOpacityAnimationName, shouldAnimateContentVisibility);
            UpdateBusyState(animateBusy);

            _card.SetDynamicResource(StyleProperty, cardStyleResourceKey);
            _iconFrame.SetDynamicResource(StyleProperty, iconFrameStyleResourceKey);
            _actionRow.SetDynamicResource(StyleProperty, actionRowStyleResourceKey);
            _actionButton.SetDynamicResource(StyleProperty, actionIconButtonStyleResourceKey);
            _actionIconOnlyButton.SetDynamicResource(StyleProperty, actionIconButtonStyleResourceKey);
            _filledActionButton.SetDynamicResource(StyleProperty, filledActionButtonStyleResourceKey);

            _actionLabel.Text = actionText;
            _actionButton.IconData = ActionIconData;
            _actionButton.Command = actionCommand;
            _actionButton.IsEnabled = IsActionEnabled;
            SemanticProperties.SetDescription(_actionButton, actionSemanticDescription);
            _actionIconOnlyButton.IconData = ActionIconData;
            _actionIconOnlyButton.Command = actionCommand;
            _actionIconOnlyButton.IsEnabled = IsActionEnabled;
            SemanticProperties.SetDescription(_actionIconOnlyButton, actionSemanticDescription);
            _filledActionButton.Text = actionText;
            _filledActionButton.Command = actionCommand;
            _filledActionButton.IsEnabled = IsActionEnabled;
            SemanticProperties.SetDescription(_filledActionButton, actionSemanticDescription);
            _actionTouchSurface.TapCommand = IsActionEnabled ? actionCommand : null;
            _actionTouchSurface.IsVisible = isActionTextVisible && IsActionEnabled && actionCommand is not null;
            _actionRow.IsEnabled = IsActionEnabled;

            UpdateElementVisibility(
                _actionRow,
                isActionTextVisible,
                ActionRowOpacityAnimationName,
                shouldAnimateContentVisibility);
            UpdateElementVisibility(
                _actionIconOnlyButton,
                isIconOnlyActionVisible,
                ActionIconOnlyButtonOpacityAnimationName,
                shouldAnimateContentVisibility);
            UpdateElementVisibility(
                _filledActionButton,
                isFilledActionVisible,
                FilledActionButtonOpacityAnimationName,
                shouldAnimateContentVisibility);

            string description = !isBodyVisible
                ? title
                : $"{title}. {body}";
            SemanticProperties.SetDescription(_card, description);
            _hasAppliedContentVisibilityState = true;
        }

        private void UpdateStateVisibility(bool animateStateVisibility)
        {
            bool isStateVisible = IsStateVisible;
            bool shouldAnimate = animateStateVisibility && _hasAppliedStateVisibility;
            double targetOpacity = isStateVisible
                ? MaterialMotion.Value("M3MotionVisibleOpacity")
                : MaterialMotion.Value("M3MotionHiddenOpacity");
            int duration = MaterialResources.Get<int>("M3MotionStatusDuration");

            if (isStateVisible)
            {
                IsVisible = true;
            }

            MaterialMotion.UpdateDouble(
                this,
                Opacity,
                targetOpacity,
                duration,
                StateOpacityAnimationName,
                shouldAnimate,
                opacity => Opacity = opacity,
                CompleteStateVisibility);
            _hasAppliedStateVisibility = true;
        }

        private void CompleteStateVisibility()
        {
            IsVisible = IsStateVisible;
        }

        private void UpdateBusyState(bool animateBusy)
        {
            bool isBusy = IsBusy;
            bool shouldAnimate = animateBusy && _hasAppliedBusyState;
            double targetOpacity = isBusy
                ? MaterialMotion.Value("M3MotionVisibleOpacity")
                : MaterialMotion.Value("M3MotionHiddenOpacity");
            int duration = MaterialResources.Get<int>("M3MotionStatusDuration");

            if (isBusy)
            {
                _loadingIndicator.IsVisible = true;
                _loadingIndicator.IsRunning = true;
            }

            MaterialMotion.UpdateDouble(
                _loadingIndicator,
                _loadingIndicator.Opacity,
                targetOpacity,
                duration,
                BusyIndicatorOpacityAnimationName,
                shouldAnimate,
                opacity => _loadingIndicator.Opacity = opacity,
                CompleteBusyState);
            _hasAppliedBusyState = true;
        }

        private void CompleteBusyState()
        {
            if (IsBusy)
            {
                _loadingIndicator.IsVisible = true;
                _loadingIndicator.IsRunning = true;
                return;
            }

            _loadingIndicator.IsRunning = false;
            _loadingIndicator.IsVisible = false;
        }

        private static void UpdateElementVisibility(
            VisualElement element,
            bool isElementVisible,
            string opacityAnimationName,
            bool animateVisibility)
        {
            double targetOpacity = isElementVisible
                ? MaterialMotion.Value("M3MotionVisibleOpacity")
                : MaterialMotion.Value("M3MotionHiddenOpacity");
            int duration = MaterialResources.Get<int>("M3MotionStatusDuration");

            if (isElementVisible)
            {
                element.IsVisible = true;
            }

            MaterialMotion.UpdateDouble(
                element,
                element.Opacity,
                targetOpacity,
                duration,
                opacityAnimationName,
                animateVisibility,
                opacity => element.Opacity = opacity,
                () => CompleteElementVisibility(element, isElementVisible));
        }

        private static void CompleteElementVisibility(VisualElement element, bool isElementVisible)
        {
            if (isElementVisible)
            {
                element.IsVisible = true;
                return;
            }

            element.IsVisible = false;
        }
    }
}
