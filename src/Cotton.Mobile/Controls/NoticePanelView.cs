// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

using System.Windows.Input;
using Microsoft.Maui.Controls.Shapes;

namespace Cotton.Mobile.Controls
{
    public class NoticePanelView : ContentView
    {
        private const string DefaultGridStyleResourceKey = "M3FileNoticeGrid";
        private const string DefaultActionGridStyleResourceKey = "M3SettingsListItemGrid";
        private const string DefaultActionIconButtonStyleResourceKey = "M3FileChromeIconButton";
        private const string DefaultIconFrameStyleResourceKey = "M3FileNoticeIconFrame";
        private const string DefaultIconStyleResourceKey = "M3FileNoticeIcon";
        private const string DefaultMessageStyleResourceKey = "M3CardSupportingWrap";
        private const string DefaultPanelStyleResourceKey = "M3FileNoticePanel";
        private const string DefaultTextStackStyleResourceKey = "M3FileNoticeTextStack";
        private const string DefaultTitleStyleResourceKey = "M3CardSupportingStrong";
        private const string ActionItemOpacityAnimationName = "M3NoticePanelActionItemOpacity";
        private const string MessageOpacityAnimationName = "M3NoticePanelMessageOpacity";
        private const string PanelOpacityAnimationName = "M3NoticePanelOpacity";
        private const string TitleOpacityAnimationName = "M3NoticePanelTitleOpacity";

        public static readonly BindableProperty TitleProperty = BindableProperty.Create(
            nameof(Title),
            typeof(string),
            typeof(NoticePanelView),
            string.Empty,
            propertyChanged: OnTitleVisibilityPropertyChanged);

        public static readonly BindableProperty MessageProperty = BindableProperty.Create(
            nameof(Message),
            typeof(string),
            typeof(NoticePanelView),
            string.Empty,
            propertyChanged: OnMessageVisibilityPropertyChanged);

        public static readonly BindableProperty IconDataProperty = BindableProperty.Create(
            nameof(IconData),
            typeof(Geometry),
            typeof(NoticePanelView),
            default(Geometry),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty ActionTextProperty = BindableProperty.Create(
            nameof(ActionText),
            typeof(string),
            typeof(NoticePanelView),
            string.Empty,
            propertyChanged: OnActionTextVisibilityPropertyChanged);

        public static readonly BindableProperty ActionIconDataProperty = BindableProperty.Create(
            nameof(ActionIconData),
            typeof(Geometry),
            typeof(NoticePanelView),
            default(Geometry),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty ActionCommandProperty = BindableProperty.Create(
            nameof(ActionCommand),
            typeof(ICommand),
            typeof(NoticePanelView),
            propertyChanged: OnActionCommandVisibilityPropertyChanged);

        public static readonly BindableProperty IsActionEnabledProperty = BindableProperty.Create(
            nameof(IsActionEnabled),
            typeof(bool),
            typeof(NoticePanelView),
            true,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsActionVisibleProperty = BindableProperty.Create(
            nameof(IsActionVisible),
            typeof(bool),
            typeof(NoticePanelView),
            true,
            propertyChanged: OnActionVisibilityPropertyChanged);

        public static readonly BindableProperty IsPanelVisibleProperty = BindableProperty.Create(
            nameof(IsPanelVisible),
            typeof(bool),
            typeof(NoticePanelView),
            true,
            propertyChanged: OnPanelVisiblePropertyChanged);

        public static readonly BindableProperty ActionSemanticDescriptionProperty = BindableProperty.Create(
            nameof(ActionSemanticDescription),
            typeof(string),
            typeof(NoticePanelView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty PanelStyleResourceKeyProperty = BindableProperty.Create(
            nameof(PanelStyleResourceKey),
            typeof(string),
            typeof(NoticePanelView),
            DefaultPanelStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty GridStyleResourceKeyProperty = BindableProperty.Create(
            nameof(GridStyleResourceKey),
            typeof(string),
            typeof(NoticePanelView),
            DefaultGridStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty ActionGridStyleResourceKeyProperty = BindableProperty.Create(
            nameof(ActionGridStyleResourceKey),
            typeof(string),
            typeof(NoticePanelView),
            DefaultActionGridStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty ActionIconButtonStyleResourceKeyProperty = BindableProperty.Create(
            nameof(ActionIconButtonStyleResourceKey),
            typeof(string),
            typeof(NoticePanelView),
            DefaultActionIconButtonStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IconFrameStyleResourceKeyProperty = BindableProperty.Create(
            nameof(IconFrameStyleResourceKey),
            typeof(string),
            typeof(NoticePanelView),
            DefaultIconFrameStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IconStyleResourceKeyProperty = BindableProperty.Create(
            nameof(IconStyleResourceKey),
            typeof(string),
            typeof(NoticePanelView),
            DefaultIconStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty TextStackStyleResourceKeyProperty = BindableProperty.Create(
            nameof(TextStackStyleResourceKey),
            typeof(string),
            typeof(NoticePanelView),
            DefaultTextStackStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty TitleStyleResourceKeyProperty = BindableProperty.Create(
            nameof(TitleStyleResourceKey),
            typeof(string),
            typeof(NoticePanelView),
            DefaultTitleStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty MessageStyleResourceKeyProperty = BindableProperty.Create(
            nameof(MessageStyleResourceKey),
            typeof(string),
            typeof(NoticePanelView),
            DefaultMessageStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        private readonly ActionListItemView _actionItem;
        private readonly Grid _grid;
        private readonly IconView _icon;
        private readonly Border _iconFrame;
        private readonly Label _message;
        private readonly Border _panel;
        private readonly VerticalStackLayout _textStack;
        private readonly Label _title;
        private bool _hasAppliedPanelVisibility;
        private bool _hasAppliedVisibilityState;

        public NoticePanelView()
        {
            _icon = new IconView();
            _iconFrame = new Border
            {
                Content = _icon,
            };

            _title = new Label();
            _message = new Label();
            _textStack = new VerticalStackLayout
            {
                Children =
                {
                    _title,
                    _message,
                },
            };
            Grid.SetColumn(_textStack, 1);

            _actionItem = new ActionListItemView();
            Grid.SetRow(_actionItem, 1);
            Grid.SetColumnSpan(_actionItem, 2);

            _grid = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition
                    {
                        Height = GridLength.Auto,
                    },
                    new RowDefinition
                    {
                        Height = GridLength.Auto,
                    },
                },
                ColumnDefinitions =
                {
                    new ColumnDefinition
                    {
                        Width = GridLength.Auto,
                    },
                    new ColumnDefinition
                    {
                        Width = GridLength.Star,
                    },
                },
                Children =
                {
                    _iconFrame,
                    _textStack,
                    _actionItem,
                },
            };

            _panel = new Border
            {
                Content = _grid,
            };

            Content = _panel;
            UpdateVisualState(
                animateTitleVisibility: false,
                animateMessageVisibility: false,
                animateActionVisibility: false);
            UpdatePanelVisibility(animatePanelVisibility: false);
        }

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public string Message
        {
            get => (string)GetValue(MessageProperty);
            set => SetValue(MessageProperty, value);
        }

        public Geometry? IconData
        {
            get => (Geometry?)GetValue(IconDataProperty);
            set => SetValue(IconDataProperty, value);
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

        public bool IsActionEnabled
        {
            get => (bool)GetValue(IsActionEnabledProperty);
            set => SetValue(IsActionEnabledProperty, value);
        }

        public bool IsActionVisible
        {
            get => (bool)GetValue(IsActionVisibleProperty);
            set => SetValue(IsActionVisibleProperty, value);
        }

        public bool IsPanelVisible
        {
            get => (bool)GetValue(IsPanelVisibleProperty);
            set => SetValue(IsPanelVisibleProperty, value);
        }

        public string ActionSemanticDescription
        {
            get => (string)GetValue(ActionSemanticDescriptionProperty);
            set => SetValue(ActionSemanticDescriptionProperty, value);
        }

        public string PanelStyleResourceKey
        {
            get => (string)GetValue(PanelStyleResourceKeyProperty);
            set => SetValue(PanelStyleResourceKeyProperty, value);
        }

        public string GridStyleResourceKey
        {
            get => (string)GetValue(GridStyleResourceKeyProperty);
            set => SetValue(GridStyleResourceKeyProperty, value);
        }

        public string ActionGridStyleResourceKey
        {
            get => (string)GetValue(ActionGridStyleResourceKeyProperty);
            set => SetValue(ActionGridStyleResourceKeyProperty, value);
        }

        public string ActionIconButtonStyleResourceKey
        {
            get => (string)GetValue(ActionIconButtonStyleResourceKeyProperty);
            set => SetValue(ActionIconButtonStyleResourceKeyProperty, value);
        }

        public string IconFrameStyleResourceKey
        {
            get => (string)GetValue(IconFrameStyleResourceKeyProperty);
            set => SetValue(IconFrameStyleResourceKeyProperty, value);
        }

        public string IconStyleResourceKey
        {
            get => (string)GetValue(IconStyleResourceKeyProperty);
            set => SetValue(IconStyleResourceKeyProperty, value);
        }

        public string TextStackStyleResourceKey
        {
            get => (string)GetValue(TextStackStyleResourceKeyProperty);
            set => SetValue(TextStackStyleResourceKeyProperty, value);
        }

        public string TitleStyleResourceKey
        {
            get => (string)GetValue(TitleStyleResourceKeyProperty);
            set => SetValue(TitleStyleResourceKeyProperty, value);
        }

        public string MessageStyleResourceKey
        {
            get => (string)GetValue(MessageStyleResourceKeyProperty);
            set => SetValue(MessageStyleResourceKeyProperty, value);
        }

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            NoticePanelView view = (NoticePanelView)bindable;
            view.UpdateVisualState(
                animateTitleVisibility: false,
                animateMessageVisibility: false,
                animateActionVisibility: false);
        }

        private static void OnTitleVisibilityPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            NoticePanelView view = (NoticePanelView)bindable;
            view.UpdateVisualState(
                animateTitleVisibility: HasTextVisibilityChanged(oldValue, newValue),
                animateMessageVisibility: false,
                animateActionVisibility: false);
        }

        private static void OnMessageVisibilityPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            NoticePanelView view = (NoticePanelView)bindable;
            view.UpdateVisualState(
                animateTitleVisibility: false,
                animateMessageVisibility: HasTextVisibilityChanged(oldValue, newValue),
                animateActionVisibility: false);
        }

        private static void OnActionTextVisibilityPropertyChanged(
            BindableObject bindable,
            object oldValue,
            object newValue)
        {
            NoticePanelView view = (NoticePanelView)bindable;
            view.UpdateVisualState(
                animateTitleVisibility: false,
                animateMessageVisibility: false,
                animateActionVisibility: view.HasActionTextVisibilityChanged(oldValue, newValue));
        }

        private static void OnActionCommandVisibilityPropertyChanged(
            BindableObject bindable,
            object oldValue,
            object newValue)
        {
            NoticePanelView view = (NoticePanelView)bindable;
            view.UpdateVisualState(
                animateTitleVisibility: false,
                animateMessageVisibility: false,
                animateActionVisibility: view.HasActionCommandVisibilityChanged(oldValue, newValue));
        }

        private static void OnActionVisibilityPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            NoticePanelView view = (NoticePanelView)bindable;
            view.UpdateVisualState(
                animateTitleVisibility: false,
                animateMessageVisibility: false,
                animateActionVisibility: view.HasActionVisibleFlagChanged(oldValue, newValue));
        }

        private static void OnPanelVisiblePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            NoticePanelView view = (NoticePanelView)bindable;
            view.UpdatePanelVisibility(animatePanelVisibility: true);
        }

        private void UpdateVisualState(
            bool animateTitleVisibility,
            bool animateMessageVisibility,
            bool animateActionVisibility)
        {
            bool shouldAnimateTitleVisibility = animateTitleVisibility && _hasAppliedVisibilityState;
            bool shouldAnimateMessageVisibility = animateMessageVisibility && _hasAppliedVisibilityState;
            bool shouldAnimateActionVisibility = animateActionVisibility && _hasAppliedVisibilityState;
            string title = Title ?? string.Empty;
            string message = Message ?? string.Empty;
            string actionText = ActionText ?? string.Empty;
            string actionSemanticDescription = string.IsNullOrWhiteSpace(ActionSemanticDescription)
                ? actionText
                : ActionSemanticDescription;
            ICommand? actionCommand = ActionCommand;
            string panelStyleResourceKey =
                MaterialResources.ResolveStyleResourceKey(PanelStyleResourceKey, DefaultPanelStyleResourceKey);
            string gridStyleResourceKey =
                MaterialResources.ResolveStyleResourceKey(GridStyleResourceKey, DefaultGridStyleResourceKey);
            string actionGridStyleResourceKey =
                MaterialResources.ResolveStyleResourceKey(ActionGridStyleResourceKey, DefaultActionGridStyleResourceKey);
            string actionIconButtonStyleResourceKey = MaterialResources.ResolveStyleResourceKey(
                ActionIconButtonStyleResourceKey,
                DefaultActionIconButtonStyleResourceKey);
            string iconFrameStyleResourceKey =
                MaterialResources.ResolveStyleResourceKey(IconFrameStyleResourceKey, DefaultIconFrameStyleResourceKey);
            string iconStyleResourceKey =
                MaterialResources.ResolveStyleResourceKey(IconStyleResourceKey, DefaultIconStyleResourceKey);
            string textStackStyleResourceKey =
                MaterialResources.ResolveStyleResourceKey(TextStackStyleResourceKey, DefaultTextStackStyleResourceKey);
            string titleStyleResourceKey =
                MaterialResources.ResolveStyleResourceKey(TitleStyleResourceKey, DefaultTitleStyleResourceKey);
            string messageStyleResourceKey =
                MaterialResources.ResolveStyleResourceKey(MessageStyleResourceKey, DefaultMessageStyleResourceKey);
            bool isTitleVisible = !string.IsNullOrWhiteSpace(title);
            bool isMessageVisible = !string.IsNullOrWhiteSpace(message);
            bool isActionVisible = IsActionVisible && !string.IsNullOrWhiteSpace(actionText) && actionCommand is not null;

            _panel.SetDynamicResource(StyleProperty, panelStyleResourceKey);
            _grid.SetDynamicResource(StyleProperty, gridStyleResourceKey);
            _iconFrame.SetDynamicResource(StyleProperty, iconFrameStyleResourceKey);
            _icon.SetDynamicResource(StyleProperty, iconStyleResourceKey);
            _textStack.SetDynamicResource(StyleProperty, textStackStyleResourceKey);
            _title.SetDynamicResource(StyleProperty, titleStyleResourceKey);
            _message.SetDynamicResource(StyleProperty, messageStyleResourceKey);

            _icon.IconData = IconData;
            _title.Text = title;
            UpdateElementVisibility(_title, isTitleVisible, TitleOpacityAnimationName, shouldAnimateTitleVisibility);
            _message.Text = message;
            UpdateElementVisibility(_message, isMessageVisible, MessageOpacityAnimationName, shouldAnimateMessageVisibility);
            _actionItem.Text = actionText;
            _actionItem.ActionIconData = ActionIconData;
            _actionItem.Command = actionCommand;
            _actionItem.IsActionEnabled = IsActionEnabled;
            _actionItem.GridStyleResourceKey = actionGridStyleResourceKey;
            _actionItem.ActionIconButtonStyleResourceKey = actionIconButtonStyleResourceKey;
            _actionItem.SemanticDescription = actionSemanticDescription;
            UpdateElementVisibility(_actionItem, isActionVisible, ActionItemOpacityAnimationName, shouldAnimateActionVisibility);
            SemanticProperties.SetDescription(this, BuildSemanticDescription(title, message));
            _hasAppliedVisibilityState = true;
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

        private static bool HasTextVisibilityChanged(object oldValue, object newValue)
        {
            return IsTextVisible(oldValue) != IsTextVisible(newValue);
        }

        private bool HasActionTextVisibilityChanged(object oldValue, object newValue)
        {
            bool wasActionVisible = IsActionVisible && ActionCommand is not null && IsTextVisible(oldValue);
            bool isActionVisible = IsActionVisible && ActionCommand is not null && IsTextVisible(newValue);

            return wasActionVisible != isActionVisible;
        }

        private bool HasActionCommandVisibilityChanged(object oldValue, object newValue)
        {
            bool hasActionText = !string.IsNullOrWhiteSpace(ActionText);
            bool wasActionVisible = IsActionVisible && hasActionText && oldValue is not null;
            bool isActionVisible = IsActionVisible && hasActionText && newValue is not null;

            return wasActionVisible != isActionVisible;
        }

        private bool HasActionVisibleFlagChanged(object oldValue, object newValue)
        {
            bool hasActionContent = !string.IsNullOrWhiteSpace(ActionText) && ActionCommand is not null;
            bool wasActionVisible = oldValue is bool oldIsActionVisible && oldIsActionVisible && hasActionContent;
            bool isActionVisible = newValue is bool newIsActionVisible && newIsActionVisible && hasActionContent;

            return wasActionVisible != isActionVisible;
        }

        private static bool IsTextVisible(object value)
        {
            return value is string text && !string.IsNullOrWhiteSpace(text);
        }

        private static string BuildSemanticDescription(string title, string message)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                return message;
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                return title;
            }

            return $"{title} {message}";
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

        private void CompletePanelVisibility()
        {
            IsVisible = IsPanelVisible;
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
