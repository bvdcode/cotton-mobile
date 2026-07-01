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

        public static readonly BindableProperty SupportingTextProperty = BindableProperty.Create(
            nameof(SupportingText),
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

        public static readonly BindableProperty IsBusyProperty = BindableProperty.Create(
            nameof(IsBusy),
            typeof(bool),
            typeof(ScreenHeaderView),
            false,
            propertyChanged: OnVisualPropertyChanged);

        private readonly ActivityIndicator _busyIndicator;
        private readonly Grid _container;
        private readonly Label _supportingText;
        private readonly Label _title;

        public ScreenHeaderView()
        {
            _title = new Label();
            _title.SetDynamicResource(StyleProperty, "M3ScreenTitle");

            _supportingText = new Label();

            VerticalStackLayout textStack = new()
            {
                Children =
                {
                    _title,
                    _supportingText,
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

        public string SupportingText
        {
            get => (string)GetValue(SupportingTextProperty);
            set => SetValue(SupportingTextProperty, value);
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

            _title.Text = title;
            _supportingText.Text = supportingText;
            _supportingText.IsVisible = IsSupportingTextVisible && !string.IsNullOrWhiteSpace(supportingText);
            _supportingText.SetDynamicResource(
                StyleProperty,
                IsSupportingTextMultiline ? "M3ScreenHeaderSupportingMultiline" : "M3ScreenHeaderSupporting");

            _busyIndicator.IsRunning = IsBusy;
            _busyIndicator.IsVisible = IsBusy;

            string description = _supportingText.IsVisible
                ? $"{title}. {supportingText}"
                : title;
            SemanticProperties.SetDescription(_container, description);
        }
    }
}
