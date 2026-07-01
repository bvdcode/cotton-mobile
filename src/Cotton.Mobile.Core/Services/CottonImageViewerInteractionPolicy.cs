// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public static class CottonImageViewerInteractionPolicy
    {
        public const double MinimumScale = 1d;
        public const double DoubleTapScale = 2d;
        public const double MaximumScale = 4d;

        public static CottonImageViewerTransform Reset()
        {
            return new CottonImageViewerTransform(
                MinimumScale,
                translationX: 0,
                translationY: 0);
        }

        public static double ClampScale(double scale)
        {
            return Math.Clamp(scale, MinimumScale, MaximumScale);
        }

        public static CottonImageViewerTransform CreateDoubleTapTransform(double currentScale)
        {
            if (currentScale > MinimumScale)
            {
                return Reset();
            }

            return new CottonImageViewerTransform(
                DoubleTapScale,
                translationX: 0,
                translationY: 0);
        }

        public static CottonImageViewerTransform CreateDoubleTapTransform(
            double currentScale,
            double imageWidth,
            double imageHeight,
            double surfaceWidth,
            double surfaceHeight,
            double tapX,
            double tapY)
        {
            if (currentScale > MinimumScale)
            {
                return Reset();
            }

            if (imageWidth <= 0 || imageHeight <= 0 || surfaceWidth <= 0 || surfaceHeight <= 0)
            {
                return CreateDoubleTapTransform(currentScale);
            }

            double targetScale = DoubleTapScale;
            double centerX = imageWidth / 2d;
            double centerY = imageHeight / 2d;
            double boundedTapX = Math.Clamp(tapX, 0, imageWidth);
            double boundedTapY = Math.Clamp(tapY, 0, imageHeight);
            double translationX = -(boundedTapX - centerX) * (targetScale - MinimumScale);
            double translationY = -(boundedTapY - centerY) * (targetScale - MinimumScale);

            return ClampTranslation(
                imageWidth,
                imageHeight,
                surfaceWidth,
                surfaceHeight,
                targetScale,
                translationX,
                translationY);
        }

        public static CottonImageViewerTransform ClampTranslation(
            double imageWidth,
            double imageHeight,
            double surfaceWidth,
            double surfaceHeight,
            double scale,
            double translationX,
            double translationY)
        {
            if (imageWidth <= 0 || imageHeight <= 0)
            {
                return new CottonImageViewerTransform(
                    scale,
                    translationX,
                    translationY);
            }

            double maxX = Math.Max(0, (imageWidth * scale - surfaceWidth) / 2d);
            double maxY = Math.Max(0, (imageHeight * scale - surfaceHeight) / 2d);

            return new CottonImageViewerTransform(
                scale,
                Math.Clamp(translationX, -maxX, maxX),
                Math.Clamp(translationY, -maxY, maxY));
        }
    }
}
