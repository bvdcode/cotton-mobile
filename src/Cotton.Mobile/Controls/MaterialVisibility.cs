// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Controls
{
    internal static class MaterialVisibility
    {
        public static void Update(
            VisualElement element,
            bool isVisible,
            string animationName,
            bool animate,
            Action? onStateChanged = null)
        {
            if (isVisible)
            {
                element.IsVisible = true;
            }

            onStateChanged?.Invoke();
            double targetOpacity = isVisible
                ? MaterialMotion.Value("M3MotionVisibleOpacity")
                : MaterialMotion.Value("M3MotionHiddenOpacity");
            int duration = MaterialResources.Get<int>("M3MotionStatusDuration");
            MaterialMotion.UpdateDouble(
                element,
                element.Opacity,
                targetOpacity,
                duration,
                animationName,
                animate,
                opacity => element.Opacity = opacity,
                () =>
                {
                    element.IsVisible = isVisible;
                    onStateChanged?.Invoke();
                });
        }
    }
}
