// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Controls
{
    public class ScreenHeaderView : ContentView
    {
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
            propertyChanged: OnVisualPropertyChanged);

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
            propertyChanged: OnVisualPropertyChanged);

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
            propertyChanged: OnVisualPropertyChanged);

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
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsBusyProperty = BindableProperty.Create(
            nameof(IsBusy),
            typeof(bool),
            typeof(ScreenHeaderView),
            false,
            propertyChanged: OnVisualPropertyChanged);

        private readonly ActivityIndicator _busyIndicator;
        private readonly Grid _container;
        private readonly Label _detailText;
        private readonly Label _supportingText;
        private readonly Label _title;

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
                    _busyIndicator,
                },
            };
            _container.SetDynamicResource(StyleProperty, "M3ScreenHeaderGrid");
            Grid.SetColumn(_busyIndicator, 1);

            Content = _container;
            UpdateVisualState();
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

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            ScreenHeaderView screenHeaderView = (ScreenHeaderView)bindable;
            screenHeaderView.UpdateVisualState();
        }

        private void UpdateVisualState()
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
            _supportingText.IsVisible = IsSupportingTextVisible && !string.IsNullOrWhiteSpace(supportingText);
            _supportingText.SetDynamicResource(StyleProperty, supportingTextStyleResourceKey);
            _detailText.Text = detailText;
            _detailText.IsVisible = IsDetailTextVisible && !string.IsNullOrWhiteSpace(detailText);
            _detailText.SetDynamicResource(StyleProperty, detailTextStyleResourceKey);

            _busyIndicator.IsRunning = IsBusy;
            _busyIndicator.IsVisible = IsBusy;

            string description = CreateDescription(title, supportingText, detailText);
            SemanticProperties.SetDescription(_container, description);
        }

        private string CreateDescription(string title, string supportingText, string detailText)
        {
            List<string> parts = [title];

            if (_supportingText.IsVisible)
            {
                parts.Add(supportingText);
            }

            if (_detailText.IsVisible)
            {
                parts.Add(detailText);
            }

            return string.Join(". ", parts);
        }
    }
}
