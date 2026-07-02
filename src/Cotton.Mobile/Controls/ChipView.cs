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
        private bool _hasAppliedTextVisibility;

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
            string text = Text ?? string.Empty;
            string chipStyleResourceKey = MaterialResources.ResolveStyleResourceKey(
                ChipStyleResourceKey,
                DefaultChipStyleResourceKey);
            string labelStyleResourceKey = MaterialResources.ResolveStyleResourceKey(
                LabelStyleResourceKey,
                DefaultLabelStyleResourceKey);

            _chip.SetDynamicResource(StyleProperty, chipStyleResourceKey);
            _label.SetDynamicResource(StyleProperty, labelStyleResourceKey);
            if (!ShouldDeferHiddenTextUpdate(text, animateTextVisibility))
            {
                _label.Text = text;
            }

            UpdateTextVisibility(text, animateTextVisibility);
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
            if (!string.IsNullOrWhiteSpace(Text))
            {
                _chip.IsVisible = true;
                return;
            }

            _label.Text = Text ?? string.Empty;
            _chip.IsVisible = false;
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
