// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

using System;

namespace Cotton.Mobile.Controls
{
    public class ScreenHeaderView : ContentView
    {
        private const string ActionContentOpacityAnimationName = "M3ScreenHeaderActionContentOpacity";
        private const string BusyFrameOpacityAnimationName = "M3ScreenHeaderBusyFrameOpacity";
        private const string DetailTextOpacityAnimationName = "M3ScreenHeaderDetailTextOpacity";
        private const string SupportingTextOpacityAnimationName = "M3ScreenHeaderSupportingTextOpacity";

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
            propertyChanged: OnSupportingTextVisibilityPropertyChanged);

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
            propertyChanged: OnSupportingTextVisibilityPropertyChanged);

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
            propertyChanged: OnDetailTextVisibilityPropertyChanged);

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
            propertyChanged: OnDetailTextVisibilityPropertyChanged);

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
            propertyChanged: OnActionContentVisibilityPropertyChanged);

        private readonly HorizontalStackLayout _actionCluster;
        private readonly ContentView _actionContentHost;
        private readonly ActivityIndicator _busyIndicator;
        private readonly Border _busyIndicatorFrame;
        private readonly Grid _container;
        private readonly Label _detailText;
        private readonly Label _supportingText;
        private readonly Label _title;
        private bool _hasAppliedActionContentVisibility;
        private bool _hasAppliedBusyState;
        private bool _hasAppliedDetailTextVisibility;
        private bool _hasAppliedSupportingTextVisibility;

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
            UpdateVisualState(
                animateActionContentVisibility: false,
                animateBusy: false,
                animateSupportingTextVisibility: false,
                animateDetailTextVisibility: false);
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
            screenHeaderView.UpdateVisualState(
                animateActionContentVisibility: false,
                animateBusy: false,
                animateSupportingTextVisibility: false,
                animateDetailTextVisibility: false);
        }

        private static void OnActionContentVisibilityPropertyChanged(
            BindableObject bindable,
            object oldValue,
            object newValue)
        {
            ScreenHeaderView screenHeaderView = (ScreenHeaderView)bindable;
            screenHeaderView.UpdateVisualState(
                animateActionContentVisibility: true,
                animateBusy: false,
                animateSupportingTextVisibility: false,
                animateDetailTextVisibility: false);
        }

        private static void OnBusyPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            ScreenHeaderView screenHeaderView = (ScreenHeaderView)bindable;
            screenHeaderView.UpdateVisualState(
                animateActionContentVisibility: false,
                animateBusy: true,
                animateSupportingTextVisibility: false,
                animateDetailTextVisibility: false);
        }

        private static void OnSupportingTextVisibilityPropertyChanged(
            BindableObject bindable,
            object oldValue,
            object newValue)
        {
            ScreenHeaderView screenHeaderView = (ScreenHeaderView)bindable;
            screenHeaderView.UpdateVisualState(
                animateActionContentVisibility: false,
                animateBusy: false,
                animateSupportingTextVisibility: true,
                animateDetailTextVisibility: false);
        }

        private static void OnDetailTextVisibilityPropertyChanged(
            BindableObject bindable,
            object oldValue,
            object newValue)
        {
            ScreenHeaderView screenHeaderView = (ScreenHeaderView)bindable;
            screenHeaderView.UpdateVisualState(
                animateActionContentVisibility: false,
                animateBusy: false,
                animateSupportingTextVisibility: false,
                animateDetailTextVisibility: true);
        }

        private void UpdateVisualState(
            bool animateActionContentVisibility,
            bool animateBusy,
            bool animateSupportingTextVisibility,
            bool animateDetailTextVisibility)
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
            _supportingText.SetDynamicResource(StyleProperty, supportingTextStyleResourceKey);
            UpdateSupportingTextVisibility(supportingText, animateSupportingTextVisibility);
            _detailText.Text = detailText;
            _detailText.SetDynamicResource(StyleProperty, detailTextStyleResourceKey);
            UpdateDetailTextVisibility(detailText, animateDetailTextVisibility);

            View? actionContent = ActionContent;
            bool hasActionContent = actionContent is not null;
            bool hasActionContentLayout = ResolveActionContentLayoutVisibility(hasActionContent, animateActionContentVisibility);
            if (hasActionContent && _actionContentHost.Content != actionContent)
            {
                _actionContentHost.Content = actionContent;
            }

            UpdateActionContentVisibility(hasActionContent, animateActionContentVisibility);
            UpdateBusyState(hasActionContentLayout, animateBusy);

            string description = CreateDescription(title, supportingText, detailText);
            SemanticProperties.SetDescription(_container, description);
        }

        private void UpdateActionContentVisibility(bool hasActionContent, bool animateActionContentVisibility)
        {
            UpdateElementVisibility(
                _actionContentHost,
                hasActionContent,
                animateActionContentVisibility,
                ref _hasAppliedActionContentVisibility,
                ActionContentOpacityAnimationName,
                CompleteActionContentVisibility);
        }

        private void UpdateSupportingTextVisibility(string supportingText, bool animateSupportingTextVisibility)
        {
            UpdateElementVisibility(
                _supportingText,
                IsSupportingTextActuallyVisible(supportingText),
                animateSupportingTextVisibility,
                ref _hasAppliedSupportingTextVisibility,
                SupportingTextOpacityAnimationName,
                CompleteSupportingTextVisibility);
        }

        private void UpdateDetailTextVisibility(string detailText, bool animateDetailTextVisibility)
        {
            UpdateElementVisibility(
                _detailText,
                IsDetailTextActuallyVisible(detailText),
                animateDetailTextVisibility,
                ref _hasAppliedDetailTextVisibility,
                DetailTextOpacityAnimationName,
                CompleteDetailTextVisibility);
        }

        private void UpdateElementVisibility(
            VisualElement element,
            bool isElementVisible,
            bool animateVisibility,
            ref bool hasAppliedVisibility,
            string animationName,
            Action completeVisibility)
        {
            bool shouldAnimate = animateVisibility && hasAppliedVisibility;
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
                animationName,
                shouldAnimate,
                opacity => element.Opacity = opacity,
                completeVisibility);
            hasAppliedVisibility = true;
        }

        private void CompleteActionContentVisibility()
        {
            View? actionContent = ActionContent;
            if (actionContent is not null)
            {
                if (_actionContentHost.Content != actionContent)
                {
                    _actionContentHost.Content = actionContent;
                }

                _actionContentHost.IsVisible = true;
                _actionCluster.IsVisible = true;
                return;
            }

            _actionContentHost.Content = null;
            _actionContentHost.IsVisible = false;
            _actionCluster.IsVisible = IsBusy || _busyIndicatorFrame.IsVisible;
        }

        private void CompleteSupportingTextVisibility()
        {
            if (IsSupportingTextActuallyVisible(SupportingText ?? string.Empty))
            {
                _supportingText.IsVisible = true;
                return;
            }

            _supportingText.IsVisible = false;
        }

        private void CompleteDetailTextVisibility()
        {
            if (IsDetailTextActuallyVisible(DetailText ?? string.Empty))
            {
                _detailText.IsVisible = true;
                return;
            }

            _detailText.IsVisible = false;
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

            if (IsSupportingTextActuallyVisible(supportingText))
            {
                parts.Add(supportingText);
            }

            if (IsDetailTextActuallyVisible(detailText))
            {
                parts.Add(detailText);
            }

            return string.Join(". ", parts);
        }

        private bool IsSupportingTextActuallyVisible(string supportingText)
        {
            return IsSupportingTextVisible && !string.IsNullOrWhiteSpace(supportingText);
        }

        private bool IsDetailTextActuallyVisible(string detailText)
        {
            return IsDetailTextVisible && !string.IsNullOrWhiteSpace(detailText);
        }

        private bool ResolveActionContentLayoutVisibility(
            bool hasActionContent,
            bool animateActionContentVisibility)
        {
            if (hasActionContent)
            {
                return true;
            }

            return animateActionContentVisibility && _hasAppliedActionContentVisibility && _actionContentHost.IsVisible;
        }
    }
}
