using Cotton.Mobile.ViewModels;

namespace Cotton.Mobile
{
    public partial class ImageViewerPage : ContentPage
    {
        private const double MinimumScale = 1d;
        private const double DoubleTapScale = 2d;
        private const double MaximumScale = 4d;

        private double _pinchStartScale = MinimumScale;
        private double _panStartX;
        private double _panStartY;

        public ImageViewerPage(ImageViewerViewModel viewModel)
        {
            ArgumentNullException.ThrowIfNull(viewModel);

            InitializeComponent();
            BindingContext = viewModel;
        }

        private void OnImagePinchUpdated(object? sender, PinchGestureUpdatedEventArgs e)
        {
            switch (e.Status)
            {
                case GestureStatus.Started:
                    _pinchStartScale = PreviewImage.Scale;
                    break;
                case GestureStatus.Running:
                    PreviewImage.Scale = Math.Clamp(_pinchStartScale * e.Scale, MinimumScale, MaximumScale);
                    if (PreviewImage.Scale <= MinimumScale)
                    {
                        ResetImageTransform();
                    }
                    else
                    {
                        ResetButton.IsVisible = true;
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
            if (PreviewImage.Scale <= MinimumScale)
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
            if (PreviewImage.Scale > MinimumScale)
            {
                ResetImageTransform();
                return;
            }

            PreviewImage.Scale = DoubleTapScale;
            ResetButton.IsVisible = true;
            ClampImageTranslation();
        }

        private void OnResetClicked(object? sender, EventArgs e)
        {
            ResetImageTransform();
        }

        private void ResetImageTransform()
        {
            PreviewImage.Scale = MinimumScale;
            PreviewImage.TranslationX = 0;
            PreviewImage.TranslationY = 0;
            ResetButton.IsVisible = false;
        }

        private void ClampImageTranslation()
        {
            if (PreviewImage.Width <= 0 || PreviewImage.Height <= 0)
            {
                return;
            }

            double maxX = Math.Max(0, (PreviewImage.Width * PreviewImage.Scale - ImageSurface.Width) / 2d);
            double maxY = Math.Max(0, (PreviewImage.Height * PreviewImage.Scale - ImageSurface.Height) / 2d);
            PreviewImage.TranslationX = Math.Clamp(PreviewImage.TranslationX, -maxX, maxX);
            PreviewImage.TranslationY = Math.Clamp(PreviewImage.TranslationY, -maxY, maxY);
        }
    }
}
