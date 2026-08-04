// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Controls
{
    public class ScreenHeaderView : ContentView
    {
        public static readonly BindableProperty TitleProperty = CreateTextProperty(nameof(Title));
        public static readonly BindableProperty SupportingTextProperty = CreateTextProperty(nameof(SupportingText));

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

        public static readonly BindableProperty IsTitleMultilineProperty = BindableProperty.Create(
            nameof(IsTitleMultiline),
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
        private readonly Border _busyIndicatorFrame;
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
            _busyIndicatorFrame = new Border { Content = _busyIndicator };
            _busyIndicatorFrame.SetDynamicResource(StyleProperty, "M3ScreenHeaderBusyFrame");

            _container = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Auto),
                },
                Children =
                {
                    textStack,
                    _busyIndicatorFrame,
                },
            };
            _container.SetDynamicResource(StyleProperty, "M3ScreenHeaderGrid");
            Grid.SetColumn(_busyIndicatorFrame, 1);

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

        public bool IsTitleMultiline
        {
            get => (bool)GetValue(IsTitleMultilineProperty);
            set => SetValue(IsTitleMultilineProperty, value);
        }

        public bool IsBusy
        {
            get => (bool)GetValue(IsBusyProperty);
            set => SetValue(IsBusyProperty, value);
        }

        private static BindableProperty CreateTextProperty(string name)
        {
            return BindableProperty.Create(
                name,
                typeof(string),
                typeof(ScreenHeaderView),
                string.Empty,
                propertyChanged: OnVisualPropertyChanged);
        }

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            ((ScreenHeaderView)bindable).UpdateVisualState();
        }

        private void UpdateVisualState()
        {
            if (_container is null)
            {
                return;
            }

            string title = Title ?? string.Empty;
            string supportingText = SupportingText ?? string.Empty;

            _title.Text = title;
            _title.MaxLines = IsTitleMultiline ? 2 : 1;
            _title.LineBreakMode = IsTitleMultiline
                ? LineBreakMode.WordWrap
                : LineBreakMode.TailTruncation;
            _supportingText.Text = supportingText;
            _supportingText.IsVisible = IsSupportingTextVisible && !string.IsNullOrWhiteSpace(supportingText);
            _supportingText.SetDynamicResource(
                StyleProperty,
                IsSupportingTextMultiline
                    ? "M3ScreenHeaderSupportingMultiline"
                    : "M3ScreenHeaderSupporting");

            _busyIndicator.IsRunning = IsBusy;
            _busyIndicatorFrame.IsVisible = IsBusy;
            SemanticProperties.SetDescription(
                this,
                string.Join(", ", new[] { title, supportingText }
                    .Where(value => !string.IsNullOrWhiteSpace(value))));
        }
    }
}
