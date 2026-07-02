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
        private const string TitleOpacityAnimationName = "M3NoticePanelTitleOpacity";

        public static readonly BindableProperty TitleProperty = BindableProperty.Create(
            nameof(Title),
            typeof(string),
            typeof(NoticePanelView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty MessageProperty = BindableProperty.Create(
            nameof(Message),
            typeof(string),
            typeof(NoticePanelView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

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
            propertyChanged: OnVisualPropertyChanged);

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
            propertyChanged: OnVisualPropertyChanged);

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
            propertyChanged: OnVisualPropertyChanged);

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
            UpdateVisualState();
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
            view.UpdateVisualState();
        }

        private void UpdateVisualState()
        {
            bool shouldAnimateVisibility = _hasAppliedVisibilityState;
            string title = Title ?? string.Empty;
            string message = Message ?? string.Empty;
            string actionText = ActionText ?? string.Empty;
            string actionSemanticDescription = string.IsNullOrWhiteSpace(ActionSemanticDescription)
                ? actionText
                : ActionSemanticDescription;
            ICommand? actionCommand = ActionCommand;
            string panelStyleResourceKey = ResolveStyleResourceKey(PanelStyleResourceKey, DefaultPanelStyleResourceKey);
            string gridStyleResourceKey = ResolveStyleResourceKey(GridStyleResourceKey, DefaultGridStyleResourceKey);
            string actionGridStyleResourceKey =
                ResolveStyleResourceKey(ActionGridStyleResourceKey, DefaultActionGridStyleResourceKey);
            string actionIconButtonStyleResourceKey = ResolveStyleResourceKey(
                ActionIconButtonStyleResourceKey,
                DefaultActionIconButtonStyleResourceKey);
            string iconFrameStyleResourceKey = ResolveStyleResourceKey(IconFrameStyleResourceKey, DefaultIconFrameStyleResourceKey);
            string iconStyleResourceKey = ResolveStyleResourceKey(IconStyleResourceKey, DefaultIconStyleResourceKey);
            string textStackStyleResourceKey = ResolveStyleResourceKey(TextStackStyleResourceKey, DefaultTextStackStyleResourceKey);
            string titleStyleResourceKey = ResolveStyleResourceKey(TitleStyleResourceKey, DefaultTitleStyleResourceKey);
            string messageStyleResourceKey = ResolveStyleResourceKey(MessageStyleResourceKey, DefaultMessageStyleResourceKey);
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
            UpdateElementVisibility(_title, isTitleVisible, TitleOpacityAnimationName, shouldAnimateVisibility);
            _message.Text = message;
            UpdateElementVisibility(_message, isMessageVisible, MessageOpacityAnimationName, shouldAnimateVisibility);
            _actionItem.Text = actionText;
            _actionItem.ActionIconData = ActionIconData;
            _actionItem.Command = actionCommand;
            _actionItem.IsActionEnabled = IsActionEnabled;
            _actionItem.GridStyleResourceKey = actionGridStyleResourceKey;
            _actionItem.ActionIconButtonStyleResourceKey = actionIconButtonStyleResourceKey;
            _actionItem.SemanticDescription = actionSemanticDescription;
            UpdateElementVisibility(_actionItem, isActionVisible, ActionItemOpacityAnimationName, shouldAnimateVisibility);
            SemanticProperties.SetDescription(this, BuildSemanticDescription(title, message));
            _hasAppliedVisibilityState = true;
        }

        private static string ResolveStyleResourceKey(string resourceKey, string defaultResourceKey)
        {
            return string.IsNullOrWhiteSpace(resourceKey)
                ? defaultResourceKey
                : resourceKey;
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
