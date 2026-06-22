// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Cotton.Mobile
{
	public partial class App : Application
	{
		private readonly AppShell _appShell;

		public App(IServiceProvider serviceProvider)
		{
			ArgumentNullException.ThrowIfNull(serviceProvider);

			InitializeComponent();
			serviceProvider.GetRequiredService<ICottonNotificationChannelProvisioningService>()
				.EnsureChannels();
			serviceProvider.GetRequiredService<ICottonAppLockCoordinator>()
				.Start();
			_appShell = serviceProvider.GetRequiredService<AppShell>();
		}

		protected override Window CreateWindow(IActivationState? activationState)
		{
			return new Window(_appShell);
		}
	}
}
