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

        public static void SetColor(
            VisualElement element,
            Color targetColor,
            string animationName,
            Action<Color> applyColor)
        {
            element.AbortAnimation(animationName);
            applyColor(targetColor);
        }

        public static void SetDouble(
            VisualElement element,
            double targetValue,
            string animationName,
            Action<double> applyValue)
        {
            element.AbortAnimation(animationName);
            applyValue(targetValue);
        }

        public static void AnimateBackgroundColor(
            VisualElement element,
            Color targetColor,
            int duration,
            string animationName)
        {
            Color startColor = element.BackgroundColor ?? MaterialResources.Get<Color>("M3Transparent");
            AnimateColor(
                element,
                startColor,
                targetColor,
                duration,
                animationName,
                color => element.BackgroundColor = color);
        }

        public static void UpdateBackgroundColor(
            VisualElement element,
            Color targetColor,
            int duration,
            string animationName,
            bool animate)
        {
            if (animate)
            {
                AnimateBackgroundColor(element, targetColor, duration, animationName);
                return;
            }

            SetBackgroundColor(element, targetColor, animationName);
        }

        public static void AnimateColor(
            VisualElement element,
            Color startColor,
            Color targetColor,
            int duration,
            string animationName,
            Action<Color> applyColor)
        {
            if (duration <= 0 || AreClose(startColor, targetColor))
            {
                SetColor(element, targetColor, animationName, applyColor);
                return;
            }

            element.AbortAnimation(animationName);

            Animation animation = new(
                progress => applyColor(Interpolate(startColor, targetColor, progress)),
                0d,
                1d,
                Easing.CubicOut);
            animation.Commit(element, animationName, rate: FrameRate, length: Duration(duration));
        }

        public static void AnimateDouble(
            VisualElement element,
            double startValue,
            double targetValue,
            int duration,
            string animationName,
            Action<double> applyValue)
        {
            if (duration <= 0 || Math.Abs(startValue - targetValue) <= ColorTolerance)
            {
                SetDouble(element, targetValue, animationName, applyValue);
                return;
            }

            element.AbortAnimation(animationName);

            Animation animation = new(
                progress => applyValue(InterpolateDouble(startValue, targetValue, progress)),
                0d,
                1d,
                Easing.CubicOut);
            animation.Commit(element, animationName, rate: FrameRate, length: Duration(duration));
        }

        public static void UpdateColor(
            VisualElement element,
            Color startColor,
            Color targetColor,
            int duration,
            string animationName,
            bool animate,
            Action<Color> applyColor)
        {
            if (animate)
            {
                AnimateColor(element, startColor, targetColor, duration, animationName, applyColor);
                return;
            }

            SetColor(element, targetColor, animationName, applyColor);
        }

        public static void UpdateDouble(
            VisualElement element,
            double startValue,
            double targetValue,
            int duration,
            string animationName,
            bool animate,
            Action<double> applyValue)
        {
            if (animate)
            {
                AnimateDouble(element, startValue, targetValue, duration, animationName, applyValue);
                return;
            }

            SetDouble(element, targetValue, animationName, applyValue);
        }

        private static Color Interpolate(Color startColor, Color targetColor, double progress)
        {
            float red = InterpolateChannel(startColor.Red, targetColor.Red, progress);
            float green = InterpolateChannel(startColor.Green, targetColor.Green, progress);
            float blue = InterpolateChannel(startColor.Blue, targetColor.Blue, progress);
            float alpha = InterpolateChannel(startColor.Alpha, targetColor.Alpha, progress);

            return new Color(red, green, blue, alpha);
        }

        private static double InterpolateDouble(double startValue, double targetValue, double progress)
        {
            return startValue + ((targetValue - startValue) * progress);
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
