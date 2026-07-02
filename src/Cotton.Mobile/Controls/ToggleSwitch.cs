// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Microsoft.Maui.Controls.Shapes;

namespace Cotton.Mobile.Controls
{
    public class ToggleSwitch : PressableContentView
    {
        private const string OpacityAnimationName = "M3SwitchOpacity";
        private const string ThumbColorAnimationName = "M3SwitchThumbColor";
        private const string ThumbTranslationAnimationName = "M3SwitchThumbTranslation";
        private const string TrackBorderColorAnimationName = "M3SwitchTrackBorderColor";
        private const string TrackColorAnimationName = "M3SwitchTrackColor";

        public static readonly BindableProperty IsToggledProperty = BindableProperty.Create(
            nameof(IsToggled),
            typeof(bool),
            typeof(ToggleSwitch),
            false,
            defaultBindingMode: BindingMode.TwoWay,
            propertyChanged: OnToggledPropertyChanged);

        public static readonly BindableProperty TrackOnColorProperty = BindableProperty.Create(
            nameof(TrackOnColor),
            typeof(Color),
            typeof(ToggleSwitch),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<Color>("M3Accent"));

        public static readonly BindableProperty TrackOffColorProperty = BindableProperty.Create(
            nameof(TrackOffColor),
            typeof(Color),
            typeof(ToggleSwitch),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.GetThemeColor("M3LightSurfaceContainerHighest", "M3DarkSurfaceContainerHighest"));

        public static readonly BindableProperty TrackOnPressedColorProperty = BindableProperty.Create(
            nameof(TrackOnPressedColor),
            typeof(Color),
            typeof(ToggleSwitch),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<Color>("M3AccentPressed"));

        public static readonly BindableProperty TrackOffPressedColorProperty = BindableProperty.Create(
            nameof(TrackOffPressedColor),
            typeof(Color),
            typeof(ToggleSwitch),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.GetThemeColor("M3LightSurfaceContainerHigh", "M3DarkSurfaceContainerHigh"));

        public static readonly BindableProperty TrackDisabledColorProperty = BindableProperty.Create(
            nameof(TrackDisabledColor),
            typeof(Color),
            typeof(ToggleSwitch),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.GetThemeColor("M3LightSurfaceContainer", "M3DarkSurfaceContainer"));

        public static readonly BindableProperty TrackOnBorderColorProperty = BindableProperty.Create(
            nameof(TrackOnBorderColor),
            typeof(Color),
            typeof(ToggleSwitch),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<Color>("M3Accent"));

        public static readonly BindableProperty TrackOffBorderColorProperty = BindableProperty.Create(
            nameof(TrackOffBorderColor),
            typeof(Color),
            typeof(ToggleSwitch),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.GetThemeColor("M3LightOutlineVariant", "M3DarkOutlineVariant"));

        public static readonly BindableProperty TrackDisabledBorderColorProperty = BindableProperty.Create(
            nameof(TrackDisabledBorderColor),
            typeof(Color),
            typeof(ToggleSwitch),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<Color>("M3Transparent"));

        public static readonly BindableProperty ThumbOnColorProperty = BindableProperty.Create(
            nameof(ThumbOnColor),
            typeof(Color),
            typeof(ToggleSwitch),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<Color>("M3OnAccent"));

        public static readonly BindableProperty ThumbOffColorProperty = BindableProperty.Create(
            nameof(ThumbOffColor),
            typeof(Color),
            typeof(ToggleSwitch),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.GetThemeColor("M3LightSurfaceContainerLowest", "M3DarkOnSurface"));

        public static readonly BindableProperty ThumbDisabledColorProperty = BindableProperty.Create(
            nameof(ThumbDisabledColor),
            typeof(Color),
            typeof(ToggleSwitch),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.GetThemeColor("M3LightOutlineVariant", "M3DarkOutlineVariant"));

        public static readonly BindableProperty TrackWidthProperty = BindableProperty.Create(
            nameof(TrackWidth),
            typeof(double),
            typeof(ToggleSwitch),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<double>("M3SwitchTrackWidth"));

        public static readonly BindableProperty TrackHeightProperty = BindableProperty.Create(
            nameof(TrackHeight),
            typeof(double),
            typeof(ToggleSwitch),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<double>("M3SwitchTrackHeight"));

        public static readonly BindableProperty TrackCornerRadiusProperty = BindableProperty.Create(
            nameof(TrackCornerRadius),
            typeof(double),
            typeof(ToggleSwitch),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<double>("M3SwitchTrackCornerRadius"));

        public static readonly BindableProperty TrackBorderWidthProperty = BindableProperty.Create(
            nameof(TrackBorderWidth),
            typeof(double),
            typeof(ToggleSwitch),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<double>("M3StrokeThin"));

        public static readonly BindableProperty ThumbSizeProperty = BindableProperty.Create(
            nameof(ThumbSize),
            typeof(double),
            typeof(ToggleSwitch),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<double>("M3SwitchThumbSize"));

        public static readonly BindableProperty ThumbCornerRadiusProperty = BindableProperty.Create(
            nameof(ThumbCornerRadius),
            typeof(double),
            typeof(ToggleSwitch),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<double>("M3SwitchThumbCornerRadius"));

        public static readonly BindableProperty ThumbInsetProperty = BindableProperty.Create(
            nameof(ThumbInset),
            typeof(double),
            typeof(ToggleSwitch),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<double>("M3SwitchThumbInset"));

        public static readonly BindableProperty TouchTargetSizeProperty = BindableProperty.Create(
            nameof(TouchTargetSize),
            typeof(double),
            typeof(ToggleSwitch),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<double>("TouchTarget"));

        private readonly Grid _trackContent;
        private readonly Border _track;
        private readonly Border _thumb;
        private bool _hasAppliedVisualState;

        public ToggleSwitch()
        {
            _thumb = new Border
            {
                HorizontalOptions = LayoutOptions.Start,
                VerticalOptions = LayoutOptions.Center,
            };

            _trackContent = new Grid();
            _trackContent.Children.Add(_thumb);

            _track = new Border
            {
                Content = _trackContent,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
            };

            Content = _track;
            UpdateVisualState(animateState: false, animateThumbTranslation: false);
        }

        public bool IsToggled
        {
            get => (bool)GetValue(IsToggledProperty);
            set => SetValue(IsToggledProperty, value);
        }

        public Color TrackOnColor
        {
            get => (Color)GetValue(TrackOnColorProperty);
            set => SetValue(TrackOnColorProperty, value);
        }

        public Color TrackOffColor
        {
            get => (Color)GetValue(TrackOffColorProperty);
            set => SetValue(TrackOffColorProperty, value);
        }

        public Color TrackOnPressedColor
        {
            get => (Color)GetValue(TrackOnPressedColorProperty);
            set => SetValue(TrackOnPressedColorProperty, value);
        }

        public Color TrackOffPressedColor
        {
            get => (Color)GetValue(TrackOffPressedColorProperty);
            set => SetValue(TrackOffPressedColorProperty, value);
        }

        public Color TrackDisabledColor
        {
            get => (Color)GetValue(TrackDisabledColorProperty);
            set => SetValue(TrackDisabledColorProperty, value);
        }

        public Color TrackOnBorderColor
        {
            get => (Color)GetValue(TrackOnBorderColorProperty);
            set => SetValue(TrackOnBorderColorProperty, value);
        }

        public Color TrackOffBorderColor
        {
            get => (Color)GetValue(TrackOffBorderColorProperty);
            set => SetValue(TrackOffBorderColorProperty, value);
        }

        public Color TrackDisabledBorderColor
        {
            get => (Color)GetValue(TrackDisabledBorderColorProperty);
            set => SetValue(TrackDisabledBorderColorProperty, value);
        }

        public Color ThumbOnColor
        {
            get => (Color)GetValue(ThumbOnColorProperty);
            set => SetValue(ThumbOnColorProperty, value);
        }

        public Color ThumbOffColor
        {
            get => (Color)GetValue(ThumbOffColorProperty);
            set => SetValue(ThumbOffColorProperty, value);
        }

        public Color ThumbDisabledColor
        {
            get => (Color)GetValue(ThumbDisabledColorProperty);
            set => SetValue(ThumbDisabledColorProperty, value);
        }

        public double TrackWidth
        {
            get => (double)GetValue(TrackWidthProperty);
            set => SetValue(TrackWidthProperty, value);
        }

        public double TrackHeight
        {
            get => (double)GetValue(TrackHeightProperty);
            set => SetValue(TrackHeightProperty, value);
        }

        public double TrackCornerRadius
        {
            get => (double)GetValue(TrackCornerRadiusProperty);
            set => SetValue(TrackCornerRadiusProperty, value);
        }

        public double TrackBorderWidth
        {
            get => (double)GetValue(TrackBorderWidthProperty);
            set => SetValue(TrackBorderWidthProperty, value);
        }

        public double ThumbSize
        {
            get => (double)GetValue(ThumbSizeProperty);
            set => SetValue(ThumbSizeProperty, value);
        }

        public double ThumbCornerRadius
        {
            get => (double)GetValue(ThumbCornerRadiusProperty);
            set => SetValue(ThumbCornerRadiusProperty, value);
        }

        public double ThumbInset
        {
            get => (double)GetValue(ThumbInsetProperty);
            set => SetValue(ThumbInsetProperty, value);
        }

        public double TouchTargetSize
        {
            get => (double)GetValue(TouchTargetSizeProperty);
            set => SetValue(TouchTargetSizeProperty, value);
        }

        protected override void OnPropertyChanged(string? propertyName = null)
        {
            base.OnPropertyChanged(propertyName);

            if (string.Equals(propertyName, nameof(IsEnabled), StringComparison.Ordinal))
            {
                UpdateVisualState(animateState: true, animateThumbTranslation: false);
            }
        }

        private static void OnToggledPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            ToggleSwitch toggleSwitch = (ToggleSwitch)bindable;
            toggleSwitch.UpdateVisualState(animateState: true, animateThumbTranslation: true);
        }

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            ToggleSwitch toggleSwitch = (ToggleSwitch)bindable;
            toggleSwitch.UpdateVisualState(animateState: false, animateThumbTranslation: false);
        }

        protected override void OnPressedStateChanged()
        {
            UpdateVisualState(animateState: true, animateThumbTranslation: false);
        }

        protected override void ExecutePress()
        {
            bool isToggled = !IsToggled;
            IsToggled = isToggled;
            PerformToggleHapticFeedback();
        }

        private void PerformToggleHapticFeedback()
        {
#if ANDROID
            if (Handler?.PlatformView is Android.Views.View platformView)
            {
                platformView.PerformHapticFeedback(Android.Views.FeedbackConstants.VirtualKey);
            }
#endif
        }

        private void UpdateVisualState(bool animateState, bool animateThumbTranslation)
        {
            if (_track is null || _trackContent is null || _thumb is null)
            {
                return;
            }

            bool shouldAnimateState = animateState && _hasAppliedVisualState;
            bool shouldAnimateThumbTranslation = animateThumbTranslation && _hasAppliedVisualState;
            int stateDuration = ResolveStateMotionDuration();
            int thumbDuration = MaterialResources.Get<int>("M3MotionSelectionDuration");
            double targetOpacity = ResolvePressableOpacity(1);
            Color targetTrackColor = ResolveTrackColor();
            Color targetTrackBorderColor = ResolveTrackBorderColor();
            Color targetThumbColor = ResolveThumbColor();
            double targetThumbTranslation = ResolveThumbTranslation();

            MaterialMotion.UpdateDouble(
                this,
                Opacity,
                targetOpacity,
                stateDuration,
                OpacityAnimationName,
                shouldAnimateState,
                opacity => Opacity = opacity);

            double touchWidth = Math.Max(TrackWidth, TouchTargetSize);
            double touchHeight = Math.Max(TrackHeight, TouchTargetSize);
            WidthRequest = touchWidth;
            HeightRequest = touchHeight;
            MinimumWidthRequest = touchWidth;
            MinimumHeightRequest = touchHeight;

            _track.WidthRequest = TrackWidth;
            _track.HeightRequest = TrackHeight;
            _track.StrokeShape = new RoundRectangle
            {
                CornerRadius = new CornerRadius(TrackCornerRadius),
            };
            _track.StrokeThickness = TrackBorderWidth;
            MaterialMotion.UpdateColor(
                _track,
                ResolveCurrentTrackBorderColor(),
                targetTrackBorderColor,
                stateDuration,
                TrackBorderColorAnimationName,
                shouldAnimateState,
                color => _track.Stroke = new SolidColorBrush(color));
            MaterialMotion.UpdateBackgroundColor(
                _track,
                targetTrackColor,
                stateDuration,
                TrackColorAnimationName,
                shouldAnimateState);

            _trackContent.Padding = new Thickness(ThumbInset, 0);

            _thumb.WidthRequest = ThumbSize;
            _thumb.HeightRequest = ThumbSize;
            _thumb.StrokeThickness = MaterialResources.Get<double>("M3StrokeNone");
            _thumb.StrokeShape = new RoundRectangle
            {
                CornerRadius = new CornerRadius(ThumbCornerRadius),
            };
            _thumb.HorizontalOptions = LayoutOptions.Start;
            MaterialMotion.UpdateBackgroundColor(
                _thumb,
                targetThumbColor,
                stateDuration,
                ThumbColorAnimationName,
                shouldAnimateState);
            MaterialMotion.UpdateDouble(
                _thumb,
                _thumb.TranslationX,
                targetThumbTranslation,
                thumbDuration,
                ThumbTranslationAnimationName,
                shouldAnimateThumbTranslation,
                translation => _thumb.TranslationX = translation);
            _hasAppliedVisualState = true;
        }

        private Color ResolveTrackColor()
        {
            if (!IsEnabled)
            {
                return TrackDisabledColor;
            }

            if (IsPressed)
            {
                return IsToggled ? TrackOnPressedColor : TrackOffPressedColor;
            }

            return IsToggled ? TrackOnColor : TrackOffColor;
        }

        private Color ResolveTrackBorderColor()
        {
            if (!IsEnabled)
            {
                return TrackDisabledBorderColor;
            }

            return IsToggled ? TrackOnBorderColor : TrackOffBorderColor;
        }

        private Color ResolveThumbColor()
        {
            if (!IsEnabled)
            {
                return ThumbDisabledColor;
            }

            return IsToggled ? ThumbOnColor : ThumbOffColor;
        }

        private Color ResolveCurrentTrackBorderColor()
        {
            if (_track.Stroke is SolidColorBrush solidColorBrush)
            {
                return solidColorBrush.Color;
            }

            return ResolveTrackBorderColor();
        }

        private double ResolveThumbTranslation()
        {
            if (!IsToggled)
            {
                return MaterialMotion.Value("M3MotionRestOffset");
            }

            double contentWidth = TrackWidth - (ThumbInset * 2d);
            return Math.Max(MaterialMotion.Value("M3MotionRestOffset"), contentWidth - ThumbSize);
        }

        private int ResolveStateMotionDuration()
        {
            return IsPressed ? PressInDuration : PressOutDuration;
        }
    }
}
