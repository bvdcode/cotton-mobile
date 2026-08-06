// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Microsoft.Maui;
using Microsoft.Maui.Controls;

namespace Cotton.Mobile.Controls
{
    public class ProfileAvatarImage : Image, IImageSourcePartEvents
    {
        private static readonly BindablePropertyKey IsLoadedSuccessfullyPropertyKey = BindableProperty.CreateReadOnly(
            nameof(IsLoadedSuccessfully),
            typeof(bool),
            typeof(ProfileAvatarImage),
            false);

        public static readonly BindableProperty IsLoadedSuccessfullyProperty =
            IsLoadedSuccessfullyPropertyKey.BindableProperty;

        public bool IsLoadedSuccessfully => (bool)GetValue(IsLoadedSuccessfullyProperty);

        protected override void OnPropertyChanged(string? propertyName = null)
        {
            base.OnPropertyChanged(propertyName);
            if (string.Equals(propertyName, SourceProperty.PropertyName, StringComparison.Ordinal))
            {
                SetValue(IsLoadedSuccessfullyPropertyKey, false);
            }
        }

        void IImageSourcePartEvents.LoadingStarted()
        {
            SetValue(IsLoadedSuccessfullyPropertyKey, false);
        }

        void IImageSourcePartEvents.LoadingCompleted(bool successful)
        {
            SetValue(IsLoadedSuccessfullyPropertyKey, successful);
        }

        void IImageSourcePartEvents.LoadingFailed(Exception exception)
        {
            SetValue(IsLoadedSuccessfullyPropertyKey, false);
        }
    }
}
