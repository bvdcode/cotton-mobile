// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Android.Content.Res;
using AndroidX.AppCompat.Content.Res;
using AndroidX.AppCompat.Widget;
using Microsoft.Maui.Handlers;

namespace Cotton.Mobile.Platforms.Android
{
    public static class AndroidRadioButtonHandlerConfiguration
    {
        public static void Configure()
        {
            RadioButtonHandler.Mapper.AppendToMapping(
                nameof(AndroidRadioButtonHandlerConfiguration),
                static (handler, _) =>
                {
                    AppCompatRadioButton radioButton = handler.PlatformView as AppCompatRadioButton
                        ?? throw new InvalidOperationException("The Android radio-button handler is unavailable.");
                    global::Android.Content.Context context = radioButton.Context
                        ?? throw new InvalidOperationException("The Android radio-button context is unavailable.");
                    ColorStateList tint = AppCompatResources.GetColorStateList(
                        context,
                        Resource.Color.cotton_radio_button_tint)
                        ?? throw new InvalidOperationException("The radio-button color state list is unavailable.");
                    radioButton.ButtonTintList = tint;
                });
        }
    }
}
