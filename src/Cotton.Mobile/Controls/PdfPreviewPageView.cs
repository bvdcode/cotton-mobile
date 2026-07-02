// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Controls
{
    public class PdfPreviewPageView : ContentView
    {
        private const string DefaultCardStyleResourceKey = "M3PdfPageSurface";
        private const string DefaultContainerStyleResourceKey = "M3PdfPageContainerGrid";
        private const string DefaultImageStyleResourceKey = "M3PdfPageImage";

        public static readonly BindableProperty ImageSourceProperty = BindableProperty.Create(
            nameof(ImageSource),
            typeof(ImageSource),
            typeof(PdfPreviewPageView),
            default(ImageSource),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty DisplayHeightProperty = BindableProperty.Create(
            nameof(DisplayHeight),
            typeof(double),
            typeof(PdfPreviewPageView),
            0d,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty ContainerStyleResourceKeyProperty = BindableProperty.Create(
            nameof(ContainerStyleResourceKey),
            typeof(string),
            typeof(PdfPreviewPageView),
            DefaultContainerStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty CardStyleResourceKeyProperty = BindableProperty.Create(
            nameof(CardStyleResourceKey),
            typeof(string),
            typeof(PdfPreviewPageView),
            DefaultCardStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty ImageStyleResourceKeyProperty = BindableProperty.Create(
            nameof(ImageStyleResourceKey),
            typeof(string),
            typeof(PdfPreviewPageView),
            DefaultImageStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        private readonly ContentCardView _card;
        private readonly Grid _container;
        private readonly Image _image;

        public PdfPreviewPageView()
        {
            _image = new Image();
            _card = new ContentCardView
            {
                BodyContent = _image,
            };
            _container = new Grid
            {
                Children =
                {
                    _card,
                },
            };

            Content = _container;
            UpdateVisualState();
        }

        public ImageSource? ImageSource
        {
            get => (ImageSource?)GetValue(ImageSourceProperty);
            set => SetValue(ImageSourceProperty, value);
        }

        public double DisplayHeight
        {
            get => (double)GetValue(DisplayHeightProperty);
            set => SetValue(DisplayHeightProperty, value);
        }

        public string ContainerStyleResourceKey
        {
            get => (string)GetValue(ContainerStyleResourceKeyProperty);
            set => SetValue(ContainerStyleResourceKeyProperty, value);
        }

        public string CardStyleResourceKey
        {
            get => (string)GetValue(CardStyleResourceKeyProperty);
            set => SetValue(CardStyleResourceKeyProperty, value);
        }

        public string ImageStyleResourceKey
        {
            get => (string)GetValue(ImageStyleResourceKeyProperty);
            set => SetValue(ImageStyleResourceKeyProperty, value);
        }

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            PdfPreviewPageView view = (PdfPreviewPageView)bindable;
            view.UpdateVisualState();
        }

        private void UpdateVisualState()
        {
            string containerStyleResourceKey =
                MaterialResources.ResolveStyleResourceKey(ContainerStyleResourceKey, DefaultContainerStyleResourceKey);
            string cardStyleResourceKey =
                MaterialResources.ResolveStyleResourceKey(CardStyleResourceKey, DefaultCardStyleResourceKey);
            string imageStyleResourceKey =
                MaterialResources.ResolveStyleResourceKey(ImageStyleResourceKey, DefaultImageStyleResourceKey);

            _container.SetDynamicResource(StyleProperty, containerStyleResourceKey);
            _card.CardStyleResourceKey = cardStyleResourceKey;
            _image.SetDynamicResource(StyleProperty, imageStyleResourceKey);
            _image.Source = ImageSource;
            _image.HeightRequest = DisplayHeight;
        }
    }
}
