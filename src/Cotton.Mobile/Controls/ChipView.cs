// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Controls
{
    public class ChipView : ContentView
    {
        private const string DefaultChipStyleResourceKey = "M3NeutralChip";
        private const string DefaultLabelStyleResourceKey = "M3ChipLabel";
        private const string ChipOpacityAnimationName = "M3ChipOpacity";

        public static readonly BindableProperty TextProperty = BindableProperty.Create(
            nameof(Text),
            typeof(string),
            typeof(ChipView),
            string.Empty,
            propertyChanged: OnTextPropertyChanged);

        public static readonly BindableProperty ChipStyleResourceKeyProperty = BindableProperty.Create(
            nameof(ChipStyleResourceKey),
            typeof(string),
            typeof(ChipView),
            DefaultChipStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty LabelStyleResourceKeyProperty = BindableProperty.Create(
            nameof(LabelStyleResourceKey),
            typeof(string),
            typeof(ChipView),
            DefaultLabelStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        private readonly Border _chip;
        private readonly Label _label;
        private string? _appliedChipStyleResourceKey;
        private string? _appliedLabelStyleResourceKey;
        private bool _hasAppliedTextVisibility;
        private bool _isCurrentTextVisible;
        private string _currentText = string.Empty;

        public ChipView()
        {
            InputTransparent = true;

            _label = new Label();
            _chip = new Border
            {
                Content = _label,
            };

            Content = _chip;
            UpdateVisualState(animateTextVisibility: false);
        }

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public string ChipStyleResourceKey
        {
            get => (string)GetValue(ChipStyleResourceKeyProperty);
            set => SetValue(ChipStyleResourceKeyProperty, value);
        }

        public string LabelStyleResourceKey
        {
            get => (string)GetValue(LabelStyleResourceKeyProperty);
            set => SetValue(LabelStyleResourceKeyProperty, value);
        }

        internal void ApplyChipState(
            string text,
            string chipStyleResourceKey,
            string labelStyleResourceKey,
            bool animateTextVisibility)
        {
            ApplyUnresolvedVisualState(
                text ?? string.Empty,
                chipStyleResourceKey,
                labelStyleResourceKey,
                animateTextVisibility);
        }

        private static void OnTextPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            ChipView view = (ChipView)bindable;
            view.UpdateVisualState(animateTextVisibility: true);
        }

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            ChipView view = (ChipView)bindable;
            view.UpdateVisualState(animateTextVisibility: false);
        }

        private void UpdateVisualState(bool animateTextVisibility)
        {
            ApplyUnresolvedVisualState(
                Text ?? string.Empty,
                ChipStyleResourceKey,
                LabelStyleResourceKey,
                animateTextVisibility);
        }

        private void ApplyUnresolvedVisualState(
            string text,
            string requestedChipStyleResourceKey,
            string requestedLabelStyleResourceKey,
            bool animateTextVisibility)
        {
            string chipStyleResourceKey = MaterialResources.ResolveStyleResourceKey(
                requestedChipStyleResourceKey,
                DefaultChipStyleResourceKey);
            string labelStyleResourceKey = MaterialResources.ResolveStyleResourceKey(
                requestedLabelStyleResourceKey,
                DefaultLabelStyleResourceKey);

            ApplyResolvedVisualState(
                text,
                chipStyleResourceKey,
                labelStyleResourceKey,
                animateTextVisibility);
        }

        private void ApplyResolvedVisualState(
            string text,
            string chipStyleResourceKey,
            string labelStyleResourceKey,
            bool animateTextVisibility)
        {
            ApplyStyleIfChanged(_chip, chipStyleResourceKey, ref _appliedChipStyleResourceKey);
            ApplyStyleIfChanged(_label, labelStyleResourceKey, ref _appliedLabelStyleResourceKey);
            if (!ShouldDeferHiddenTextUpdate(text, animateTextVisibility))
            {
                if (!string.Equals(_label.Text, text, StringComparison.Ordinal))
                {
                    _label.Text = text;
                }
            }

            bool isTextVisible = !string.IsNullOrWhiteSpace(text);
            _currentText = text;
            if (!_hasAppliedTextVisibility || _isCurrentTextVisible != isTextVisible)
            {
                _isCurrentTextVisible = isTextVisible;
                UpdateTextVisibility(text, animateTextVisibility);
            }
        }

        private void UpdateTextVisibility(string text, bool animateTextVisibility)
        {
            bool isTextVisible = !string.IsNullOrWhiteSpace(text);
            bool shouldAnimate = animateTextVisibility && _hasAppliedTextVisibility;
            double targetOpacity = isTextVisible
                ? MaterialMotion.Value("M3MotionVisibleOpacity")
                : MaterialMotion.Value("M3MotionHiddenOpacity");
            int duration = MaterialResources.Get<int>("M3MotionStatusDuration");

            if (isTextVisible)
            {
                _chip.IsVisible = true;
            }

            MaterialMotion.UpdateDouble(
                _chip,
                _chip.Opacity,
                targetOpacity,
                duration,
                ChipOpacityAnimationName,
                shouldAnimate,
                opacity => _chip.Opacity = opacity,
                CompleteTextVisibility);
            _hasAppliedTextVisibility = true;
        }

        private void CompleteTextVisibility()
        {
            if (_isCurrentTextVisible)
            {
                _chip.IsVisible = true;
                return;
            }

            _label.Text = _currentText;
            _chip.IsVisible = false;
        }

        private static void ApplyStyleIfChanged(
            Element target,
            string styleResourceKey,
            ref string? appliedStyleResourceKey)
        {
            if (string.Equals(appliedStyleResourceKey, styleResourceKey, StringComparison.Ordinal))
            {
                return;
            }

            target.SetDynamicResource(StyleProperty, styleResourceKey);
            appliedStyleResourceKey = styleResourceKey;
        }

        private bool ShouldDeferHiddenTextUpdate(string text, bool animateTextVisibility)
        {
            return string.IsNullOrWhiteSpace(text)
                && animateTextVisibility
                && _hasAppliedTextVisibility
                && _chip.IsVisible;
        }
    }
}
