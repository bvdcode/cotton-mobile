// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Controls
{
    public class TopAppBarTitleLabel : Label
    {
        private const string DarkTitleStyleResourceKey = "M3DarkAppBarTitleLine";
        private const string DefaultTitleStyleResourceKey = "M3AppBarTitleLine";

        public static readonly BindableProperty UseDarkThemeProperty = BindableProperty.Create(
            nameof(UseDarkTheme),
            typeof(bool),
            typeof(TopAppBarTitleLabel),
            false,
            propertyChanged: OnVisualPropertyChanged);

        public TopAppBarTitleLabel()
        {
            UpdateVisualState();
        }

        public bool UseDarkTheme
        {
            get => (bool)GetValue(UseDarkThemeProperty);
            set => SetValue(UseDarkThemeProperty, value);
        }

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            TopAppBarTitleLabel view = (TopAppBarTitleLabel)bindable;
            view.UpdateVisualState();
        }

        private void UpdateVisualState()
        {
            string titleStyleResourceKey = UseDarkTheme
                ? DarkTitleStyleResourceKey
                : DefaultTitleStyleResourceKey;

            SetDynamicResource(StyleProperty, titleStyleResourceKey);
        }
    }
}
