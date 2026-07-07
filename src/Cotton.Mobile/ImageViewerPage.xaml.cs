// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using System.Windows.Input;
using Cotton.Mobile.Controls;
using Cotton.Mobile.Services;
using Cotton.Mobile.ViewModels;

namespace Cotton.Mobile
{
    public partial class ImageViewerPage : DarkViewerPage
    {
        private double _pinchStartScale = CottonImageViewerInteractionPolicy.MinimumScale;
        private double _panStartX;
        private double _panStartY;

        public ImageViewerPage(ImageViewerViewModel viewModel)
        {
            ArgumentNullException.ThrowIfNull(viewModel);

            InitializeComponent();
            ResetImageCommand = new Command(ResetImageTransform);
            BindingContext = viewModel;
            ImageSurface.SizeChanged += ImageSurface_SizeChanged;
            PreviewImage.SizeChanged += PreviewImage_SizeChanged;
        }

        public ICommand ResetImageCommand { get; }

        private void ImageSurface_SizeChanged(object? sender, EventArgs e)
        {
            ClampImageTranslation();
        }

        private void PreviewImage_SizeChanged(object? sender, EventArgs e)
        {
            ClampImageTranslation();
        }

        private void OnImagePinchUpdated(object? sender, PinchGestureUpdatedEventArgs e)
        {
            switch (e.Status)
            {
                case GestureStatus.Started:
                    _pinchStartScale = PreviewImage.Scale;
                    break;
                case GestureStatus.Running:
                    PreviewImage.Scale = CottonImageViewerInteractionPolicy.ClampScale(_pinchStartScale * e.Scale);
                    if (PreviewImage.Scale <= CottonImageViewerInteractionPolicy.MinimumScale)
                    {
                        ResetImageTransform();
                    }
                    else
                    {
                        ResetButton.IsActionVisible = true;
                        ClampImageTranslation();
                    }

                    break;
                case GestureStatus.Completed:
                case GestureStatus.Canceled:
                    ClampImageTranslation();
                    break;
            }
        }

        private void OnImagePanUpdated(object? sender, PanUpdatedEventArgs e)
        {
            if (PreviewImage.Scale <= CottonImageViewerInteractionPolicy.MinimumScale)
            {
                return;
            }

            switch (e.StatusType)
            {
                case GestureStatus.Started:
                    _panStartX = PreviewImage.TranslationX;
                    _panStartY = PreviewImage.TranslationY;
                    break;
                case GestureStatus.Running:
                    PreviewImage.TranslationX = _panStartX + e.TotalX;
                    PreviewImage.TranslationY = _panStartY + e.TotalY;
                    ClampImageTranslation();
                    break;
                case GestureStatus.Completed:
                case GestureStatus.Canceled:
                    ClampImageTranslation();
                    break;
            }
        }

        private void OnImageDoubleTapped(object? sender, TappedEventArgs e)
        {
            Point? tapPosition = e.GetPosition(PreviewImage);
            if (tapPosition is not Point point)
            {
                return;
            }

            ApplyImageTransform(
                CottonImageViewerInteractionPolicy.CreateDoubleTapTransform(
                    PreviewImage.Scale,
                    PreviewImage.Width,
                    PreviewImage.Height,
                    ImageSurface.Width,
                    ImageSurface.Height,
                    point.X,
                    point.Y));
        }

        private void ResetImageTransform()
        {
            ApplyImageTransform(CottonImageViewerInteractionPolicy.Reset());
        }

        private void ClampImageTranslation()
        {
            ApplyImageTransform(
                CottonImageViewerInteractionPolicy.ClampTranslation(
                    PreviewImage.Width,
                    PreviewImage.Height,
                    ImageSurface.Width,
                    ImageSurface.Height,
                    PreviewImage.Scale,
                    PreviewImage.TranslationX,
                    PreviewImage.TranslationY));
        }

        private void ApplyImageTransform(CottonImageViewerTransform transform)
        {
            PreviewImage.Scale = transform.Scale;
            PreviewImage.TranslationX = transform.TranslationX;
            PreviewImage.TranslationY = transform.TranslationY;
            ResetButton.IsActionVisible = transform.Scale > CottonImageViewerInteractionPolicy.MinimumScale;
        }
    }
}
