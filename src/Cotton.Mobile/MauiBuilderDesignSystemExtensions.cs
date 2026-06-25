// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID
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
					handler.PlatformView.BackgroundTintList = ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
				});
#endif
			return builder;
		}
	}
}
