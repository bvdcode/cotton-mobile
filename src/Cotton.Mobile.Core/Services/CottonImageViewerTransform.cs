// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonImageViewerTransform
    {
        public CottonImageViewerTransform(
            double scale,
            double translationX,
            double translationY)
        {
            Scale = scale;
            TranslationX = translationX;
            TranslationY = translationY;
        }

        public double Scale { get; }

        public double TranslationX { get; }

        public double TranslationY { get; }
    }
}
