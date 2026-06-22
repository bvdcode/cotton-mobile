// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Microsoft.Maui.ApplicationModel;

namespace Cotton.Mobile.Services
{
	public static class CottonBrowserLaunchOptions
	{
		public static BrowserLaunchOptions SystemPreferred()
		{
			return new BrowserLaunchOptions
			{
				LaunchMode = BrowserLaunchMode.SystemPreferred,
				TitleMode = BrowserTitleMode.Show,
			};
		}
	}
}
