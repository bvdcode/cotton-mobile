// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using System.Globalization;

namespace Cotton.Mobile.Controls
{
    internal static class MaterialMotion
    {
        private const double ColorTolerance = 0.001d;
        private const uint FrameRate = 16;

        public static double Value(string resourceKey)
        {
            return MaterialResources.Get<double>(resourceKey);
        }

        public static uint Duration(string resourceKey)
        {
            return Duration(MaterialResources.Get<int>(resourceKey));
        }

        public static uint Duration(int duration)
        {
            return Convert.ToUInt32(duration, CultureInfo.InvariantCulture);
        }

        public static void SetBackgroundColor(VisualElement element, Color targetColor, string animationName)
        {
            element.AbortAnimation(animationName);
            element.BackgroundColor = targetColor;
        }

        public static void AnimateBackgroundColor(
            VisualElement element,
            Color targetColor,
            int duration,
            string animationName)
        {
            Color startColor = element.BackgroundColor ?? MaterialResources.Get<Color>("M3Transparent");
            if (duration <= 0 || AreClose(startColor, targetColor))
            {
                SetBackgroundColor(element, targetColor, animationName);
                return;
            }

            element.AbortAnimation(animationName);

            Animation animation = new(
                progress => element.BackgroundColor = Interpolate(startColor, targetColor, progress),
                0d,
                1d,
                Easing.CubicOut);
            animation.Commit(element, animationName, rate: FrameRate, length: Duration(duration));
        }

        private static Color Interpolate(Color startColor, Color targetColor, double progress)
        {
            float red = InterpolateChannel(startColor.Red, targetColor.Red, progress);
            float green = InterpolateChannel(startColor.Green, targetColor.Green, progress);
            float blue = InterpolateChannel(startColor.Blue, targetColor.Blue, progress);
            float alpha = InterpolateChannel(startColor.Alpha, targetColor.Alpha, progress);

            return new Color(red, green, blue, alpha);
        }

        private static float InterpolateChannel(float startValue, float targetValue, double progress)
        {
            return (float)(startValue + ((targetValue - startValue) * progress));
        }

        private static bool AreClose(Color firstColor, Color secondColor)
        {
            return Math.Abs(firstColor.Red - secondColor.Red) <= ColorTolerance
                && Math.Abs(firstColor.Green - secondColor.Green) <= ColorTolerance
                && Math.Abs(firstColor.Blue - secondColor.Blue) <= ColorTolerance
                && Math.Abs(firstColor.Alpha - secondColor.Alpha) <= ColorTolerance;
        }
    }
}
