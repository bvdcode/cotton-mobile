// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Controls
{
    public class BrandMarkView : ContentView
    {
        private const string DefaultFrameStyleResourceKey = "M3AuthBrandMarkFrame";
        private const string DefaultImageStyleResourceKey = "M3AuthBrandMarkImage";

        public static readonly BindableProperty SourceProperty = BindableProperty.Create(
            nameof(Source),
            typeof(ImageSource),
            typeof(BrandMarkView),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty SemanticDescriptionProperty = BindableProperty.Create(
            nameof(SemanticDescription),
            typeof(string),
            typeof(BrandMarkView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty FrameStyleResourceKeyProperty = BindableProperty.Create(
            nameof(FrameStyleResourceKey),
            typeof(string),
            typeof(BrandMarkView),
            DefaultFrameStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty ImageStyleResourceKeyProperty = BindableProperty.Create(
            nameof(ImageStyleResourceKey),
            typeof(string),
            typeof(BrandMarkView),
            DefaultImageStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        private readonly Border _frame;
        private readonly Image _image;

        public BrandMarkView()
        {
            _image = new Image();
            _frame = new Border
            {
                Content = _image,
            };

            Content = _frame;
            UpdateVisualState();
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

        public string FrameStyleResourceKey
        {
            get => (string)GetValue(FrameStyleResourceKeyProperty);
            set => SetValue(FrameStyleResourceKeyProperty, value);
        }

        public string ImageStyleResourceKey
        {
            get => (string)GetValue(ImageStyleResourceKeyProperty);
            set => SetValue(ImageStyleResourceKeyProperty, value);
        }

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            BrandMarkView view = (BrandMarkView)bindable;
            view.UpdateVisualState();
        }

        private void UpdateVisualState()
        {
            string frameStyleResourceKey =
                MaterialResources.ResolveStyleResourceKey(FrameStyleResourceKey, DefaultFrameStyleResourceKey);
            string imageStyleResourceKey =
                MaterialResources.ResolveStyleResourceKey(ImageStyleResourceKey, DefaultImageStyleResourceKey);

            _frame.SetDynamicResource(StyleProperty, frameStyleResourceKey);
            _image.SetDynamicResource(StyleProperty, imageStyleResourceKey);
            _image.Source = Source;
            SemanticProperties.SetDescription(this, SemanticDescription ?? string.Empty);
        }
    }
}
