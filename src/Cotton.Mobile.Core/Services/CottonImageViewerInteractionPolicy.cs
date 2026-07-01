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
