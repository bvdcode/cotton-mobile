// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID
using Android.Content;
using Android.Content.Res;
using Microsoft.Maui.Handlers;
#endif

namespace Cotton.Mobile
{
	public static class MauiBuilderDesignSystemExtensions
	{
		public static MauiAppBuilder UseCottonDesignSystem(this MauiAppBuilder builder)
		{
#if ANDROID
			EntryHandler.Mapper.AppendToMapping(
				nameof(UseCottonDesignSystem),
				static (handler, view) =>
				{
					Context context = handler.PlatformView.Context
						?? throw new InvalidOperationException("Entry context was not found.");
					Resources resources = context.Resources
						?? throw new InvalidOperationException("Entry resources were not found.");

					handler.PlatformView.BackgroundTintList = ColorStateList.ValueOf(
						resources.GetColor(Resource.Color.cotton_transparent, context.Theme));
					handler.PlatformView.SetHighlightColor(
						resources.GetColor(Resource.Color.cotton_text_highlight, context.Theme));

					if (OperatingSystem.IsAndroidVersionAtLeast(29))
					{
						handler.PlatformView.SetTextCursorDrawable(Resource.Drawable.cotton_text_cursor);
						handler.PlatformView.SetTextSelectHandle(Resource.Drawable.cotton_text_select_handle);
						handler.PlatformView.SetTextSelectHandleLeft(Resource.Drawable.cotton_text_select_handle);
						handler.PlatformView.SetTextSelectHandleRight(Resource.Drawable.cotton_text_select_handle);
					}
				});
#endif
			return builder;
		}
	}
}
