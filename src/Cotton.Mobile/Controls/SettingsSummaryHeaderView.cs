// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Controls
{
    public class SettingsSummaryHeaderView : ContentView
    {
        private const string DefaultDetailStyleResourceKey = "M3CardSupportingBlock";
        private const string DefaultGridStyleResourceKey = "M3SettingsSummaryGrid";
        private const string DefaultStatusStyleResourceKey = "M3CardSupportingLine";
        private const string DefaultTitleStyleResourceKey = "M3CardTitle";

        public static readonly BindableProperty TitleProperty = BindableProperty.Create(
            nameof(Title),
            typeof(string),
            typeof(SettingsSummaryHeaderView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty StatusTextProperty = BindableProperty.Create(
            nameof(StatusText),
            typeof(string),
            typeof(SettingsSummaryHeaderView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty DetailTextProperty = BindableProperty.Create(
            nameof(DetailText),
            typeof(string),
            typeof(SettingsSummaryHeaderView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsStatusVisibleProperty = BindableProperty.Create(
            nameof(IsStatusVisible),
            typeof(bool),
            typeof(SettingsSummaryHeaderView),
            true,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsDetailVisibleProperty = BindableProperty.Create(
            nameof(IsDetailVisible),
            typeof(bool),
            typeof(SettingsSummaryHeaderView),
            true,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty GridStyleResourceKeyProperty = BindableProperty.Create(
            nameof(GridStyleResourceKey),
            typeof(string),
            typeof(SettingsSummaryHeaderView),
            DefaultGridStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty TitleStyleResourceKeyProperty = BindableProperty.Create(
            nameof(TitleStyleResourceKey),
            typeof(string),
            typeof(SettingsSummaryHeaderView),
            DefaultTitleStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty StatusStyleResourceKeyProperty = BindableProperty.Create(
            nameof(StatusStyleResourceKey),
            typeof(string),
            typeof(SettingsSummaryHeaderView),
            DefaultStatusStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty DetailStyleResourceKeyProperty = BindableProperty.Create(
            nameof(DetailStyleResourceKey),
            typeof(string),
            typeof(SettingsSummaryHeaderView),
            DefaultDetailStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        private readonly Label _detailLabel;
        private readonly Grid _grid;
        private readonly Label _statusLabel;
        private readonly Label _titleLabel;

        public SettingsSummaryHeaderView()
        {
            InputTransparent = true;

            _titleLabel = new Label();
            _statusLabel = new Label();
            _detailLabel = new Label();
            _grid = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = GridLength.Auto },
                },
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto },
                },
                Children =
                {
                    _titleLabel,
                    _statusLabel,
                    _detailLabel,
                },
            };

            Grid.SetColumn(_statusLabel, 1);
            Grid.SetRow(_detailLabel, 1);
            Grid.SetColumnSpan(_detailLabel, 2);

            Content = _grid;
            UpdateVisualState();
        }

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public string StatusText
        {
            get => (string)GetValue(StatusTextProperty);
            set => SetValue(StatusTextProperty, value);
        }

        public string DetailText
        {
            get => (string)GetValue(DetailTextProperty);
            set => SetValue(DetailTextProperty, value);
        }

        public bool IsStatusVisible
        {
            get => (bool)GetValue(IsStatusVisibleProperty);
            set => SetValue(IsStatusVisibleProperty, value);
        }

        public bool IsDetailVisible
        {
            get => (bool)GetValue(IsDetailVisibleProperty);
            set => SetValue(IsDetailVisibleProperty, value);
        }

        public string GridStyleResourceKey
        {
            get => (string)GetValue(GridStyleResourceKeyProperty);
            set => SetValue(GridStyleResourceKeyProperty, value);
        }

        public string TitleStyleResourceKey
        {
            get => (string)GetValue(TitleStyleResourceKeyProperty);
            set => SetValue(TitleStyleResourceKeyProperty, value);
        }

        public string StatusStyleResourceKey
        {
            get => (string)GetValue(StatusStyleResourceKeyProperty);
            set => SetValue(StatusStyleResourceKeyProperty, value);
        }

        public string DetailStyleResourceKey
        {
            get => (string)GetValue(DetailStyleResourceKeyProperty);
            set => SetValue(DetailStyleResourceKeyProperty, value);
        }

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            SettingsSummaryHeaderView view = (SettingsSummaryHeaderView)bindable;
            view.UpdateVisualState();
        }

        private void UpdateVisualState()
        {
            string gridStyleResourceKey = string.IsNullOrWhiteSpace(GridStyleResourceKey)
                ? DefaultGridStyleResourceKey
                : GridStyleResourceKey;
            string titleStyleResourceKey = string.IsNullOrWhiteSpace(TitleStyleResourceKey)
                ? DefaultTitleStyleResourceKey
                : TitleStyleResourceKey;
            string statusStyleResourceKey = string.IsNullOrWhiteSpace(StatusStyleResourceKey)
                ? DefaultStatusStyleResourceKey
                : StatusStyleResourceKey;
            string detailStyleResourceKey = string.IsNullOrWhiteSpace(DetailStyleResourceKey)
                ? DefaultDetailStyleResourceKey
                : DetailStyleResourceKey;

            _grid.SetDynamicResource(StyleProperty, gridStyleResourceKey);
            _titleLabel.SetDynamicResource(StyleProperty, titleStyleResourceKey);
            _statusLabel.SetDynamicResource(StyleProperty, statusStyleResourceKey);
            _detailLabel.SetDynamicResource(StyleProperty, detailStyleResourceKey);

            _titleLabel.Text = Title ?? string.Empty;
            _statusLabel.Text = StatusText ?? string.Empty;
            _statusLabel.IsVisible = IsStatusVisible;
            _detailLabel.Text = DetailText ?? string.Empty;
            _detailLabel.IsVisible = IsDetailVisible;
        }
    }
}
