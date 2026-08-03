// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Controls
{
    public abstract class MaterialAnimatedContentView : ContentView
    {
        public static readonly BindableProperty IsContentVisibleProperty = BindableProperty.Create(
            nameof(IsContentVisible),
            typeof(bool),
            typeof(MaterialAnimatedContentView),
            true,
            propertyChanged: OnContentVisiblePropertyChanged);

        protected MaterialAnimatedContentView()
        {
            ApplyContentVisibility();
        }

        public bool IsContentVisible
        {
            get => (bool)GetValue(IsContentVisibleProperty);
            set => SetValue(IsContentVisibleProperty, value);
        }

        protected virtual bool IsContentInteractiveWhenVisible => true;

        private static void OnContentVisiblePropertyChanged(
            BindableObject bindable,
            object oldValue,
            object newValue)
        {
            ((MaterialAnimatedContentView)bindable).ApplyContentVisibility();
        }

        private void ApplyContentVisibility()
        {
            IsVisible = IsContentVisible;
            Opacity = 1d;
            InputTransparent = !IsContentVisible || !IsContentInteractiveWhenVisible;
        }
    }
}
