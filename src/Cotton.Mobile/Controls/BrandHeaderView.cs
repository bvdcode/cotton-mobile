// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Controls
{
    public class BrandHeaderView : ContentView
    {
        private const string DefaultGridStyleResourceKey = "M3AuthBrandGrid";
        private const string DefaultImageStyleResourceKey = "M3AuthBrandMarkImage";
        private const string DefaultMarkFrameStyleResourceKey = "M3AuthBrandMarkFrame";
        private const string DefaultTitleStyleResourceKey = "M3AuthTitle";

        public static readonly BindableProperty TitleProperty = BindableProperty.Create(
            nameof(Title),
            typeof(string),
            typeof(BrandHeaderView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty SourceProperty = BindableProperty.Create(
            nameof(Source),
            typeof(ImageSource),
            typeof(BrandHeaderView),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty SemanticDescriptionProperty = BindableProperty.Create(
            nameof(SemanticDescription),
            typeof(string),
            typeof(BrandHeaderView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty GridStyleResourceKeyProperty = BindableProperty.Create(
            nameof(GridStyleResourceKey),
            typeof(string),
            typeof(BrandHeaderView),
            DefaultGridStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty TitleStyleResourceKeyProperty = BindableProperty.Create(
            nameof(TitleStyleResourceKey),
            typeof(string),
            typeof(BrandHeaderView),
            DefaultTitleStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty MarkFrameStyleResourceKeyProperty = BindableProperty.Create(
            nameof(MarkFrameStyleResourceKey),
            typeof(string),
            typeof(BrandHeaderView),
            DefaultMarkFrameStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty ImageStyleResourceKeyProperty = BindableProperty.Create(
            nameof(ImageStyleResourceKey),
            typeof(string),
            typeof(BrandHeaderView),
            DefaultImageStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        private readonly BrandMarkView _brandMark;
        private readonly Grid _grid;
        private readonly Label _titleLabel;

        public BrandHeaderView()
        {
            InputTransparent = true;

            _brandMark = new BrandMarkView();
            _titleLabel = new Label();

            Grid.SetColumn(_titleLabel, 1);

            _grid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Star },
                },
                Children =
                {
                    _brandMark,
                    _titleLabel,
                },
            };

            Content = _grid;
            UpdateVisualState();
        }

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public ImageSource? Source
        {
            get => (ImageSource?)GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        public string SemanticDescription
        {
            get => (string)GetValue(SemanticDescriptionProperty);
            set => SetValue(SemanticDescriptionProperty, value);
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

        public string MarkFrameStyleResourceKey
        {
            get => (string)GetValue(MarkFrameStyleResourceKeyProperty);
            set => SetValue(MarkFrameStyleResourceKeyProperty, value);
        }

        public string ImageStyleResourceKey
        {
            get => (string)GetValue(ImageStyleResourceKeyProperty);
            set => SetValue(ImageStyleResourceKeyProperty, value);
        }

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            BrandHeaderView view = (BrandHeaderView)bindable;
            view.UpdateVisualState();
        }

        private void UpdateVisualState()
        {
            string gridStyleResourceKey =
                MaterialResources.ResolveStyleResourceKey(GridStyleResourceKey, DefaultGridStyleResourceKey);
            string titleStyleResourceKey =
                MaterialResources.ResolveStyleResourceKey(TitleStyleResourceKey, DefaultTitleStyleResourceKey);
            string markFrameStyleResourceKey =
                MaterialResources.ResolveStyleResourceKey(MarkFrameStyleResourceKey, DefaultMarkFrameStyleResourceKey);
            string imageStyleResourceKey =
                MaterialResources.ResolveStyleResourceKey(ImageStyleResourceKey, DefaultImageStyleResourceKey);
            string title = Title ?? string.Empty;
            string semanticDescription = string.IsNullOrWhiteSpace(SemanticDescription)
                ? title
                : SemanticDescription;

            _grid.SetDynamicResource(StyleProperty, gridStyleResourceKey);
            _titleLabel.SetDynamicResource(StyleProperty, titleStyleResourceKey);
            _titleLabel.Text = title;
            SemanticProperties.SetHeadingLevel(_titleLabel, SemanticHeadingLevel.Level1);

            _brandMark.Source = Source;
            _brandMark.SemanticDescription = semanticDescription;
            _brandMark.FrameStyleResourceKey = markFrameStyleResourceKey;
            _brandMark.ImageStyleResourceKey = imageStyleResourceKey;
        }
    }
}
