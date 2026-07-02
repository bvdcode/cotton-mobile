// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Controls
{
    public class ScreenHeaderView : ContentView
    {
        private const string BusyFrameOpacityAnimationName = "M3ScreenHeaderBusyFrameOpacity";

        public static readonly BindableProperty TitleProperty = BindableProperty.Create(
            nameof(Title),
            typeof(string),
            typeof(ScreenHeaderView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty TitleStyleResourceKeyProperty = BindableProperty.Create(
            nameof(TitleStyleResourceKey),
            typeof(string),
            typeof(ScreenHeaderView),
            "M3ScreenTitle",
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty SupportingTextProperty = BindableProperty.Create(
            nameof(SupportingText),
            typeof(string),
            typeof(ScreenHeaderView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty SupportingTextStyleResourceKeyProperty = BindableProperty.Create(
            nameof(SupportingTextStyleResourceKey),
            typeof(string),
            typeof(ScreenHeaderView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsSupportingTextVisibleProperty = BindableProperty.Create(
            nameof(IsSupportingTextVisible),
            typeof(bool),
            typeof(ScreenHeaderView),
            true,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsSupportingTextMultilineProperty = BindableProperty.Create(
            nameof(IsSupportingTextMultiline),
            typeof(bool),
            typeof(ScreenHeaderView),
            false,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty DetailTextProperty = BindableProperty.Create(
            nameof(DetailText),
            typeof(string),
            typeof(ScreenHeaderView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty DetailTextStyleResourceKeyProperty = BindableProperty.Create(
            nameof(DetailTextStyleResourceKey),
            typeof(string),
            typeof(ScreenHeaderView),
            "M3ScreenHeaderSupporting",
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsDetailTextVisibleProperty = BindableProperty.Create(
            nameof(IsDetailTextVisible),
            typeof(bool),
            typeof(ScreenHeaderView),
            true,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsBusyProperty = BindableProperty.Create(
            nameof(IsBusy),
            typeof(bool),
            typeof(ScreenHeaderView),
            false,
            propertyChanged: OnBusyPropertyChanged);

        public static readonly BindableProperty ActionContentProperty = BindableProperty.Create(
            nameof(ActionContent),
            typeof(View),
            typeof(ScreenHeaderView),
            default(View),
            propertyChanged: OnVisualPropertyChanged);

        private readonly HorizontalStackLayout _actionCluster;
        private readonly ContentView _actionContentHost;
        private readonly ActivityIndicator _busyIndicator;
        private readonly Border _busyIndicatorFrame;
        private readonly Grid _container;
        private readonly Label _detailText;
        private readonly Label _supportingText;
        private readonly Label _title;
        private bool _hasAppliedBusyState;

        public ScreenHeaderView()
        {
            _title = new Label();
            _title.SetDynamicResource(StyleProperty, "M3ScreenTitle");

            _supportingText = new Label();

            _detailText = new Label();

            VerticalStackLayout textStack = new()
            {
                Children =
                {
                    _title,
                    _supportingText,
                    _detailText,
                },
            };
            textStack.SetDynamicResource(StyleProperty, "M3ScreenHeaderTextStack");

            _busyIndicator = new ActivityIndicator();
            _busyIndicator.SetDynamicResource(StyleProperty, "M3ScreenHeaderActivityIndicator");

            _busyIndicatorFrame = new Border
            {
                Content = _busyIndicator,
                IsVisible = false,
                Opacity = MaterialMotion.Value("M3MotionHiddenOpacity"),
            };
            _busyIndicatorFrame.SetDynamicResource(StyleProperty, "M3ScreenHeaderBusyFrame");

            _actionContentHost = new ContentView();

            _actionCluster = new HorizontalStackLayout
            {
                Children =
                {
                    _actionContentHost,
                    _busyIndicatorFrame,
                },
            };
            _actionCluster.SetDynamicResource(StyleProperty, "M3ScreenHeaderActionCluster");

            _container = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition
                    {
                        Width = GridLength.Star,
                    },
                    new ColumnDefinition
                    {
                        Width = GridLength.Auto,
                    },
                },
                Children =
                {
                    textStack,
                    _actionCluster,
                },
            };
            _container.SetDynamicResource(StyleProperty, "M3ScreenHeaderGrid");
            Grid.SetColumn(_actionCluster, 1);

            Content = _container;
            UpdateVisualState(animateBusy: false);
        }

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public string TitleStyleResourceKey
        {
            get => (string)GetValue(TitleStyleResourceKeyProperty);
            set => SetValue(TitleStyleResourceKeyProperty, value);
        }

        public string SupportingText
        {
            get => (string)GetValue(SupportingTextProperty);
            set => SetValue(SupportingTextProperty, value);
        }

        public string SupportingTextStyleResourceKey
        {
            get => (string)GetValue(SupportingTextStyleResourceKeyProperty);
            set => SetValue(SupportingTextStyleResourceKeyProperty, value);
        }

        public bool IsSupportingTextVisible
        {
            get => (bool)GetValue(IsSupportingTextVisibleProperty);
            set => SetValue(IsSupportingTextVisibleProperty, value);
        }

        public bool IsSupportingTextMultiline
        {
            get => (bool)GetValue(IsSupportingTextMultilineProperty);
            set => SetValue(IsSupportingTextMultilineProperty, value);
        }

        public string DetailText
        {
            get => (string)GetValue(DetailTextProperty);
            set => SetValue(DetailTextProperty, value);
        }

        public string DetailTextStyleResourceKey
        {
            get => (string)GetValue(DetailTextStyleResourceKeyProperty);
            set => SetValue(DetailTextStyleResourceKeyProperty, value);
        }

        public bool IsDetailTextVisible
        {
            get => (bool)GetValue(IsDetailTextVisibleProperty);
            set => SetValue(IsDetailTextVisibleProperty, value);
        }

        public bool IsBusy
        {
            get => (bool)GetValue(IsBusyProperty);
            set => SetValue(IsBusyProperty, value);
        }

        public View? ActionContent
        {
            get => (View?)GetValue(ActionContentProperty);
            set => SetValue(ActionContentProperty, value);
        }

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            ScreenHeaderView screenHeaderView = (ScreenHeaderView)bindable;
            screenHeaderView.UpdateVisualState(animateBusy: false);
        }

        private static void OnBusyPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            ScreenHeaderView screenHeaderView = (ScreenHeaderView)bindable;
            screenHeaderView.UpdateVisualState(animateBusy: true);
        }

        private void UpdateVisualState(bool animateBusy)
        {
            string title = Title ?? string.Empty;
            string supportingText = SupportingText ?? string.Empty;
            string detailText = DetailText ?? string.Empty;
            string titleStyleResourceKey = string.IsNullOrWhiteSpace(TitleStyleResourceKey)
                ? "M3ScreenTitle"
                : TitleStyleResourceKey;
            string supportingTextStyleResourceKey = string.IsNullOrWhiteSpace(SupportingTextStyleResourceKey)
                ? IsSupportingTextMultiline ? "M3ScreenHeaderSupportingMultiline" : "M3ScreenHeaderSupporting"
                : SupportingTextStyleResourceKey;
            string detailTextStyleResourceKey = string.IsNullOrWhiteSpace(DetailTextStyleResourceKey)
                ? "M3ScreenHeaderSupporting"
                : DetailTextStyleResourceKey;

            _title.Text = title;
            _title.SetDynamicResource(StyleProperty, titleStyleResourceKey);
            _supportingText.Text = supportingText;
            _supportingText.IsVisible = IsSupportingTextVisible && !string.IsNullOrWhiteSpace(supportingText);
            _supportingText.SetDynamicResource(StyleProperty, supportingTextStyleResourceKey);
            _detailText.Text = detailText;
            _detailText.IsVisible = IsDetailTextVisible && !string.IsNullOrWhiteSpace(detailText);
            _detailText.SetDynamicResource(StyleProperty, detailTextStyleResourceKey);

            View? actionContent = ActionContent;
            if (_actionContentHost.Content != actionContent)
            {
                _actionContentHost.Content = actionContent;
            }

            bool hasActionContent = actionContent is not null;
            _actionContentHost.IsVisible = hasActionContent;
            UpdateBusyState(hasActionContent, animateBusy);

            string description = CreateDescription(title, supportingText, detailText);
            SemanticProperties.SetDescription(_container, description);
        }

        private void UpdateBusyState(bool hasActionContent, bool animateBusy)
        {
            bool isBusy = IsBusy;
            bool shouldAnimate = animateBusy && _hasAppliedBusyState;
            double targetOpacity = isBusy
                ? MaterialMotion.Value("M3MotionVisibleOpacity")
                : MaterialMotion.Value("M3MotionHiddenOpacity");
            int duration = MaterialResources.Get<int>("M3MotionStatusDuration");

            if (isBusy)
            {
                _busyIndicatorFrame.IsVisible = true;
                _busyIndicator.IsVisible = true;
                _actionCluster.IsVisible = true;
                _busyIndicator.IsRunning = true;
            }
            else
            {
                _busyIndicator.IsRunning = false;
                _actionCluster.IsVisible = hasActionContent || _busyIndicatorFrame.IsVisible;
            }

            MaterialMotion.UpdateDouble(
                _busyIndicatorFrame,
                _busyIndicatorFrame.Opacity,
                targetOpacity,
                duration,
                BusyFrameOpacityAnimationName,
                shouldAnimate,
                opacity => _busyIndicatorFrame.Opacity = opacity,
                () => CompleteBusyState(hasActionContent));
            _hasAppliedBusyState = true;
        }

        private void CompleteBusyState(bool hasActionContent)
        {
            if (IsBusy)
            {
                _busyIndicatorFrame.IsVisible = true;
                _busyIndicator.IsVisible = true;
                _actionCluster.IsVisible = true;
                return;
            }

            _busyIndicatorFrame.IsVisible = false;
            _busyIndicator.IsVisible = false;
            _actionCluster.IsVisible = hasActionContent;
        }

        private string CreateDescription(string title, string supportingText, string detailText)
        {
            List<string> parts = [title];

            if (_supportingText.IsVisible)
            {
                parts.Add(supportingText);
            }

            if (_detailText.IsVisible)
            {
                parts.Add(detailText);
            }

            return string.Join(". ", parts);
        }
    }
}
