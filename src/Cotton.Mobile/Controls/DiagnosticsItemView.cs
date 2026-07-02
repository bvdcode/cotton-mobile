// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Controls
{
    public class DiagnosticsItemView : ContentView
    {
        private const string DefaultGridStyleResourceKey = "M3DiagnosticsItemGrid";
        private const string DefaultLabelTextStyleResourceKey = "M3CardSupporting";
        private const string DefaultValueTextStyleResourceKey = "M3CardSupportingPrimaryBlock";

        public static readonly BindableProperty LabelTextProperty = BindableProperty.Create(
            nameof(LabelText),
            typeof(string),
            typeof(DiagnosticsItemView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty ValueTextProperty = BindableProperty.Create(
            nameof(ValueText),
            typeof(string),
            typeof(DiagnosticsItemView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty GridStyleResourceKeyProperty = BindableProperty.Create(
            nameof(GridStyleResourceKey),
            typeof(string),
            typeof(DiagnosticsItemView),
            DefaultGridStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty LabelTextStyleResourceKeyProperty = BindableProperty.Create(
            nameof(LabelTextStyleResourceKey),
            typeof(string),
            typeof(DiagnosticsItemView),
            DefaultLabelTextStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty ValueTextStyleResourceKeyProperty = BindableProperty.Create(
            nameof(ValueTextStyleResourceKey),
            typeof(string),
            typeof(DiagnosticsItemView),
            DefaultValueTextStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty LabelColumnWidthProperty = BindableProperty.Create(
            nameof(LabelColumnWidth),
            typeof(double),
            typeof(DiagnosticsItemView),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<double>("M3DiagnosticsLabelColumnWidth"));

        private readonly Grid _grid;
        private readonly Label _label;
        private readonly Label _value;

        public DiagnosticsItemView()
        {
            InputTransparent = true;

            _label = new Label();
            _value = new Label();

            Grid.SetColumn(_value, 1);

            _grid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Star },
                },
                Children =
                {
                    _label,
                    _value,
                },
            };

            Content = _grid;
            UpdateVisualState();
        }

        public string LabelText
        {
            get => (string)GetValue(LabelTextProperty);
            set => SetValue(LabelTextProperty, value);
        }

        public string ValueText
        {
            get => (string)GetValue(ValueTextProperty);
            set => SetValue(ValueTextProperty, value);
        }

        public string GridStyleResourceKey
        {
            get => (string)GetValue(GridStyleResourceKeyProperty);
            set => SetValue(GridStyleResourceKeyProperty, value);
        }

        public string LabelTextStyleResourceKey
        {
            get => (string)GetValue(LabelTextStyleResourceKeyProperty);
            set => SetValue(LabelTextStyleResourceKeyProperty, value);
        }

        public string ValueTextStyleResourceKey
        {
            get => (string)GetValue(ValueTextStyleResourceKeyProperty);
            set => SetValue(ValueTextStyleResourceKeyProperty, value);
        }

        public double LabelColumnWidth
        {
            get => (double)GetValue(LabelColumnWidthProperty);
            set => SetValue(LabelColumnWidthProperty, value);
        }

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            DiagnosticsItemView view = (DiagnosticsItemView)bindable;
            view.UpdateVisualState();
        }

        private void UpdateVisualState()
        {
            string gridStyleResourceKey = ResolveStyleResourceKey(GridStyleResourceKey, DefaultGridStyleResourceKey);
            string labelTextStyleResourceKey =
                ResolveStyleResourceKey(LabelTextStyleResourceKey, DefaultLabelTextStyleResourceKey);
            string valueTextStyleResourceKey =
                ResolveStyleResourceKey(ValueTextStyleResourceKey, DefaultValueTextStyleResourceKey);

            _grid.SetDynamicResource(StyleProperty, gridStyleResourceKey);
            _grid.ColumnDefinitions[0].Width = new GridLength(LabelColumnWidth);
            _label.SetDynamicResource(StyleProperty, labelTextStyleResourceKey);
            _value.SetDynamicResource(StyleProperty, valueTextStyleResourceKey);
            _label.Text = LabelText ?? string.Empty;
            _value.Text = ValueText ?? string.Empty;
        }

        private static string ResolveStyleResourceKey(string resourceKey, string defaultResourceKey)
        {
            return string.IsNullOrWhiteSpace(resourceKey)
                ? defaultResourceKey
                : resourceKey;
        }
    }
}
