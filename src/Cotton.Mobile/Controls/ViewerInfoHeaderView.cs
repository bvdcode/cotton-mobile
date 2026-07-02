// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Controls
{
    public class ViewerInfoHeaderView : ContentView
    {
        private const string DefaultStackStyleResourceKey = "M3ScreenHeaderTextStack";
        private const string DefaultDetailsStyleResourceKey = "M3CardSupportingLine";
        private const string DefaultStatusStyleResourceKey = "M3CardSupportingLine";
        private const string StatusOpacityAnimationName = "M3ViewerInfoStatusOpacity";

        public static readonly BindableProperty DetailsProperty = BindableProperty.Create(
            nameof(Details),
            typeof(string),
            typeof(ViewerInfoHeaderView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty StatusProperty = BindableProperty.Create(
            nameof(Status),
            typeof(string),
            typeof(ViewerInfoHeaderView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsStatusVisibleProperty = BindableProperty.Create(
            nameof(IsStatusVisible),
            typeof(bool),
            typeof(ViewerInfoHeaderView),
            false,
            propertyChanged: OnStatusVisiblePropertyChanged);

        public static readonly BindableProperty StackStyleResourceKeyProperty = BindableProperty.Create(
            nameof(StackStyleResourceKey),
            typeof(string),
            typeof(ViewerInfoHeaderView),
            DefaultStackStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty DetailsStyleResourceKeyProperty = BindableProperty.Create(
            nameof(DetailsStyleResourceKey),
            typeof(string),
            typeof(ViewerInfoHeaderView),
            DefaultDetailsStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty StatusStyleResourceKeyProperty = BindableProperty.Create(
            nameof(StatusStyleResourceKey),
            typeof(string),
            typeof(ViewerInfoHeaderView),
            DefaultStatusStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        private readonly Label _details;
        private readonly Label _status;
        private readonly VerticalStackLayout _stack;
        private bool _hasAppliedStatusVisibility;

        public ViewerInfoHeaderView()
        {
            _details = new Label();
            _status = new Label();

            _stack = new VerticalStackLayout
            {
                Children =
                {
                    _details,
                    _status,
                },
            };

            Content = _stack;
            UpdateVisualState(animateStatusVisibility: false);
        }

        public string Details
        {
            get => (string)GetValue(DetailsProperty);
            set => SetValue(DetailsProperty, value);
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

        public string StackStyleResourceKey
        {
            get => (string)GetValue(StackStyleResourceKeyProperty);
            set => SetValue(StackStyleResourceKeyProperty, value);
        }

        public string DetailsStyleResourceKey
        {
            get => (string)GetValue(DetailsStyleResourceKeyProperty);
            set => SetValue(DetailsStyleResourceKeyProperty, value);
        }

        public string StatusStyleResourceKey
        {
            get => (string)GetValue(StatusStyleResourceKeyProperty);
            set => SetValue(StatusStyleResourceKeyProperty, value);
        }

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            ViewerInfoHeaderView view = (ViewerInfoHeaderView)bindable;
            view.UpdateVisualState(animateStatusVisibility: false);
        }

        private static void OnStatusVisiblePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            ViewerInfoHeaderView view = (ViewerInfoHeaderView)bindable;
            view.UpdateVisualState(animateStatusVisibility: true);
        }

        private void UpdateVisualState(bool animateStatusVisibility)
        {
            string stackStyleResourceKey = string.IsNullOrWhiteSpace(StackStyleResourceKey)
                ? DefaultStackStyleResourceKey
                : StackStyleResourceKey;
            string detailsStyleResourceKey = string.IsNullOrWhiteSpace(DetailsStyleResourceKey)
                ? DefaultDetailsStyleResourceKey
                : DetailsStyleResourceKey;
            string statusStyleResourceKey = string.IsNullOrWhiteSpace(StatusStyleResourceKey)
                ? DefaultStatusStyleResourceKey
                : StatusStyleResourceKey;

            _stack.SetDynamicResource(StyleProperty, stackStyleResourceKey);
            _details.SetDynamicResource(StyleProperty, detailsStyleResourceKey);
            _status.SetDynamicResource(StyleProperty, statusStyleResourceKey);
            _details.Text = Details ?? string.Empty;
            _status.Text = Status ?? string.Empty;
            UpdateStatusVisibility(animateStatusVisibility);
        }

        private void UpdateStatusVisibility(bool animateStatusVisibility)
        {
            bool isStatusVisible = IsStatusVisible;
            bool shouldAnimate = animateStatusVisibility && _hasAppliedStatusVisibility;
            double targetOpacity = isStatusVisible
                ? MaterialMotion.Value("M3MotionVisibleOpacity")
                : MaterialMotion.Value("M3MotionHiddenOpacity");
            int duration = MaterialResources.Get<int>("M3MotionStatusDuration");

            if (isStatusVisible)
            {
                _status.IsVisible = true;
            }

            MaterialMotion.UpdateDouble(
                _status,
                _status.Opacity,
                targetOpacity,
                duration,
                StatusOpacityAnimationName,
                shouldAnimate,
                opacity => _status.Opacity = opacity,
                CompleteStatusVisibility);
            _hasAppliedStatusVisibility = true;
        }

        private void CompleteStatusVisibility()
        {
            if (IsStatusVisible)
            {
                _status.IsVisible = true;
                return;
            }

            _status.IsVisible = false;
        }
    }
}
