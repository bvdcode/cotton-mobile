// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Controls;

namespace Cotton.Mobile.Behaviors
{
    public class FocusedInputChromeBehavior : Behavior<Entry>
    {
        private const string FieldBackgroundAnimationName = "M3InputFieldBackground";
        private const string FieldStrokeAnimationName = "M3InputFieldStroke";
        private const string IconFrameBackgroundAnimationName = "M3InputIconFrameBackground";
        private const string IconFrameStrokeAnimationName = "M3InputIconFrameStroke";
        private const string IconColorAnimationName = "M3InputIconColor";
        public static readonly BindableProperty FieldProperty = BindableProperty.Create(
            nameof(Field),
            typeof(Border),
            typeof(FocusedInputChromeBehavior),
            default(Border),
            propertyChanged: OnVisualTargetChanged);

        public static readonly BindableProperty LeadingIconFrameProperty = BindableProperty.Create(
            nameof(LeadingIconFrame),
            typeof(Border),
            typeof(FocusedInputChromeBehavior),
            default(Border),
            propertyChanged: OnVisualTargetChanged);

        public static readonly BindableProperty LeadingIconProperty = BindableProperty.Create(
            nameof(LeadingIcon),
            typeof(IconView),
            typeof(FocusedInputChromeBehavior),
            default(IconView),
            propertyChanged: OnVisualTargetChanged);

        private const string LightFocusResourceKey = "M3LightPrimary";
        private const string LightOnFocusResourceKey = "M3LightOnPrimary";
        private const string LightOutlineResourceKey = "M3LightOutline";
        private const string LightSurfaceContainerHighResourceKey = "M3LightSurfaceContainerHigh";
        private const string LightSurfaceContainerLowResourceKey = "M3LightSurfaceContainerLow";
        private const string LightOnSurfaceVariantResourceKey = "M3LightOnSurfaceVariant";
        private const string DarkFocusResourceKey = "M3DarkAction";
        private const string DarkOnFocusResourceKey = "M3DarkOnAction";
        private const string DarkOutlineResourceKey = "M3DarkOutline";
        private const string DarkSurfaceContainerHighResourceKey = "M3DarkSurfaceContainerHigh";
        private const string DarkSurfaceContainerLowResourceKey = "M3DarkSurfaceContainerLow";
        private const string DarkOnSurfaceVariantResourceKey = "M3DarkOnSurfaceVariant";
        private const string TransparentResourceKey = "M3Transparent";
        private const string FocusStrokeResourceKey = "M3StrokeFocus";
        private const string FocusMotionDurationResourceKey = "M3MotionFocusDuration";
        private const string RestStrokeResourceKey = "M3StrokeThin";

        private Entry? _entry;

        public Border? Field
        {
            get => (Border?)GetValue(FieldProperty);
            set => SetValue(FieldProperty, value);
        }

        public Border? LeadingIconFrame
        {
            get => (Border?)GetValue(LeadingIconFrameProperty);
            set => SetValue(LeadingIconFrameProperty, value);
        }

        public IconView? LeadingIcon
        {
            get => (IconView?)GetValue(LeadingIconProperty);
            set => SetValue(LeadingIconProperty, value);
        }

        protected override void OnAttachedTo(Entry bindable)
        {
            base.OnAttachedTo(bindable);

            _entry = bindable;
            bindable.Focused += OnFocusChanged;
            bindable.Unfocused += OnFocusChanged;

            Application application = GetApplication();
            application.RequestedThemeChanged += OnRequestedThemeChanged;
            ApplyCurrentState(false);
        }

        protected override void OnDetachingFrom(Entry bindable)
        {
            Application application = GetApplication();
            application.RequestedThemeChanged -= OnRequestedThemeChanged;

            bindable.Focused -= OnFocusChanged;
            bindable.Unfocused -= OnFocusChanged;
            ClearFocusedState();
            _entry = null;

            base.OnDetachingFrom(bindable);
        }

        private static void OnVisualTargetChanged(BindableObject bindable, object oldValue, object newValue)
        {
            FocusedInputChromeBehavior behavior = (FocusedInputChromeBehavior)bindable;
            behavior.ApplyCurrentState(false);
        }

        private void OnFocusChanged(object? sender, FocusEventArgs e)
        {
            ApplyCurrentState(true);
        }

        private void OnRequestedThemeChanged(object? sender, AppThemeChangedEventArgs e)
        {
            ApplyCurrentState(false);
        }

        private void ApplyCurrentState(bool animate)
        {
            if (_entry is null || !_entry.IsFocused)
            {
                ApplyRestingState(animate);
                return;
            }

            ApplyFocusedState(animate);
        }

        private void ApplyFocusedState(bool animate)
        {
            Color focusColor = MaterialResources.GetThemeColor(LightFocusResourceKey, DarkFocusResourceKey);
            Color onFocusColor = MaterialResources.GetThemeColor(LightOnFocusResourceKey, DarkOnFocusResourceKey);
            Color fieldBackgroundColor = MaterialResources.GetThemeColor(
                LightSurfaceContainerHighResourceKey,
                DarkSurfaceContainerHighResourceKey);
            double focusStroke = MaterialResources.Get<double>(FocusStrokeResourceKey);

            if (Field is not null)
            {
                AnimateBorderStroke(Field, focusColor, FieldStrokeAnimationName, animate);
                Field.StrokeThickness = focusStroke;
                AnimateBackground(Field, fieldBackgroundColor, FieldBackgroundAnimationName, animate);
            }

            if (LeadingIconFrame is not null)
            {
                AnimateBorderStroke(LeadingIconFrame, focusColor, IconFrameStrokeAnimationName, animate);
                AnimateBackground(LeadingIconFrame, focusColor, IconFrameBackgroundAnimationName, animate);
            }

            if (LeadingIcon is not null)
            {
                AnimateIconColor(LeadingIcon, onFocusColor, animate);
            }
        }

        private void ApplyRestingState(bool animate)
        {
            Color fieldStrokeColor = MaterialResources.GetThemeColor(
                LightOutlineResourceKey,
                DarkOutlineResourceKey);
            Color fieldBackgroundColor = MaterialResources.GetThemeColor(
                LightSurfaceContainerLowResourceKey,
                DarkSurfaceContainerLowResourceKey);
            Color iconFrameStrokeColor = MaterialResources.Get<Color>(TransparentResourceKey);
            Color iconFrameBackgroundColor = MaterialResources.GetThemeColor(
                LightSurfaceContainerHighResourceKey,
                DarkSurfaceContainerHighResourceKey);
            Color iconColor = MaterialResources.GetThemeColor(
                LightOnSurfaceVariantResourceKey,
                DarkOnSurfaceVariantResourceKey);
            double restStroke = MaterialResources.Get<double>(RestStrokeResourceKey);

            if (Field is not null)
            {
                AnimateBorderStroke(Field, fieldStrokeColor, FieldStrokeAnimationName, animate);
                Field.StrokeThickness = restStroke;
                AnimateBackground(Field, fieldBackgroundColor, FieldBackgroundAnimationName, animate);
            }

            if (LeadingIconFrame is not null)
            {
                AnimateBorderStroke(LeadingIconFrame, iconFrameStrokeColor, IconFrameStrokeAnimationName, animate);
                AnimateBackground(LeadingIconFrame, iconFrameBackgroundColor, IconFrameBackgroundAnimationName, animate);
            }

            if (LeadingIcon is not null)
            {
                AnimateIconColor(LeadingIcon, iconColor, animate);
            }
        }

        private void ClearFocusedState()
        {
            Field?.ClearValue(Border.StrokeProperty);
            Field?.ClearValue(Border.StrokeThicknessProperty);
            Field?.ClearValue(VisualElement.BackgroundColorProperty);

            LeadingIconFrame?.ClearValue(Border.StrokeProperty);
            LeadingIconFrame?.ClearValue(VisualElement.BackgroundColorProperty);

            LeadingIcon?.ClearValue(IconView.IconColorProperty);
        }

        private static Application GetApplication()
        {
            return Application.Current
                ?? throw new InvalidOperationException("The current application was not found.");
        }

        private static void AnimateBackground(
            Border border,
            Color targetColor,
            string animationName,
            bool animate)
        {
            MaterialMotion.UpdateBackgroundColor(
                border,
                targetColor,
                GetMotionDuration(animate),
                animationName,
                animate);
        }

        private static void AnimateBorderStroke(
            Border border,
            Color targetColor,
            string animationName,
            bool animate)
        {
            Color startColor = GetCurrentStrokeColor(border, targetColor);
            MaterialMotion.UpdateColor(
                border,
                startColor,
                targetColor,
                GetMotionDuration(animate),
                animationName,
                animate,
                color => border.Stroke = new SolidColorBrush(color));
        }

        private static void AnimateIconColor(IconView icon, Color targetColor, bool animate)
        {
            MaterialMotion.UpdateColor(
                icon,
                icon.IconColor,
                targetColor,
                GetMotionDuration(animate),
                IconColorAnimationName,
                animate,
                color => icon.IconColor = color);
        }

        private static int GetMotionDuration(bool animate)
        {
            return animate ? MaterialResources.Get<int>(FocusMotionDurationResourceKey) : 0;
        }

        private static Color GetCurrentStrokeColor(Border border, Color fallbackColor)
        {
            if (border.Stroke is SolidColorBrush brush)
            {
                return brush.Color;
            }

            return fallbackColor;
        }

    }
}
