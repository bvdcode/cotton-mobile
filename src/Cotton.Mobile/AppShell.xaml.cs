// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile
{
	public partial class AppShell : Shell
	{
		public AppShell(MainPage mainPage)
		{
			ArgumentNullException.ThrowIfNull(mainPage);

			InitializeComponent();
			MainContent.Content = mainPage;
		}
	}
}
