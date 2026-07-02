// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Controls
{
    public class ChipView : ContentView
    {
        private const string DefaultChipStyleResourceKey = "M3NeutralChip";
        private const string DefaultLabelStyleResourceKey = "M3ChipLabel";

        public static readonly BindableProperty TextProperty = BindableProperty.Create(
            nameof(Text),
            typeof(string),
            typeof(ChipView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

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

        public ChipView()
        {
            InputTransparent = true;

            _label = new Label();
            _chip = new Border
            {
                Content = _label,
            };

            Content = _chip;
            UpdateVisualState();
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

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            ChipView view = (ChipView)bindable;
            view.UpdateVisualState();
        }

        private void UpdateVisualState()
        {
            string chipStyleResourceKey = string.IsNullOrWhiteSpace(ChipStyleResourceKey)
                ? DefaultChipStyleResourceKey
                : ChipStyleResourceKey;
            string labelStyleResourceKey = string.IsNullOrWhiteSpace(LabelStyleResourceKey)
                ? DefaultLabelStyleResourceKey
                : LabelStyleResourceKey;

            _chip.SetDynamicResource(StyleProperty, chipStyleResourceKey);
            _label.SetDynamicResource(StyleProperty, labelStyleResourceKey);
            _label.Text = Text ?? string.Empty;
        }
    }
}
