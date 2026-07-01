// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Controls;

namespace Cotton.Mobile.Behaviors
{
    public class FocusedInputChromeBehavior : Behavior<Entry>
    {
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

        private const string AccentResourceKey = "M3Accent";
        private const string OnAccentResourceKey = "M3OnAccent";
        private const string FocusStrokeResourceKey = "M3StrokeFocus";
        private const string FocusedLightFieldBackgroundResourceKey = "M3LightSurfaceContainerLowest";
        private const string FocusedDarkFieldBackgroundResourceKey = "M3DarkSurfaceContainerHigh";

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
            ApplyCurrentState();
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
            behavior.ApplyCurrentState();
        }

        private void OnFocusChanged(object? sender, FocusEventArgs e)
        {
            ApplyCurrentState();
        }

        private void OnRequestedThemeChanged(object? sender, AppThemeChangedEventArgs e)
        {
            ApplyCurrentState();
        }

        private void ApplyCurrentState()
        {
            if (_entry is null || !_entry.IsFocused)
            {
                ClearFocusedState();
                return;
            }

            Color accentColor = GetRequiredColor(AccentResourceKey);
            Color onAccentColor = GetRequiredColor(OnAccentResourceKey);
            Color fieldBackgroundColor = GetRequiredColor(GetFocusedFieldBackgroundResourceKey());
            double focusStroke = GetRequiredDouble(FocusStrokeResourceKey);

            if (Field is not null)
            {
                Field.Stroke = new SolidColorBrush(accentColor);
                Field.StrokeThickness = focusStroke;
                Field.BackgroundColor = fieldBackgroundColor;
            }

            if (LeadingIconFrame is not null)
            {
                LeadingIconFrame.Stroke = new SolidColorBrush(accentColor);
                LeadingIconFrame.BackgroundColor = accentColor;
            }

            if (LeadingIcon is not null)
            {
                LeadingIcon.IconColor = onAccentColor;
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

        private static string GetFocusedFieldBackgroundResourceKey()
        {
            Application application = GetApplication();
            if (application.RequestedTheme == AppTheme.Light)
            {
                return FocusedLightFieldBackgroundResourceKey;
            }

            return FocusedDarkFieldBackgroundResourceKey;
        }

        private static Color GetRequiredColor(string key)
        {
            object resource = GetRequiredResource(key);
            if (resource is Color color)
            {
                return color;
            }

            throw new InvalidOperationException($"Resource '{key}' must be a Color.");
        }

        private static double GetRequiredDouble(string key)
        {
            object resource = GetRequiredResource(key);
            if (resource is double value)
            {
                return value;
            }

            throw new InvalidOperationException($"Resource '{key}' must be a Double.");
        }

        private static object GetRequiredResource(string key)
        {
            Application application = GetApplication();
            if (application.Resources.TryGetValue(key, out object resource))
            {
                return resource;
            }

            throw new InvalidOperationException($"Resource '{key}' was not found.");
        }

        private static Application GetApplication()
        {
            return Application.Current
                ?? throw new InvalidOperationException("The current application was not found.");
        }
    }
}
