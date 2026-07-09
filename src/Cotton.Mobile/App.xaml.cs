// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cotton.Mobile
{
	public partial class App : Application
	{
		private readonly AppShell _appShell;
		private readonly IServiceProvider _serviceProvider;
		private bool _didProvisionStartupNotificationChannels;

		public App(IServiceProvider serviceProvider)
		{
			ArgumentNullException.ThrowIfNull(serviceProvider);

			_serviceProvider = serviceProvider;
			InitializeComponent();
			serviceProvider.GetRequiredService<ICottonAppLockCoordinator>()
				.Start();
			_appShell = serviceProvider.GetRequiredService<AppShell>();
		}

		protected override Window CreateWindow(IActivationState? activationState)
		{
			var window = new Window(_appShell);
			window.Created += Window_Created;
			return window;
		}

		private void Window_Created(object? sender, EventArgs e)
		{
			if (sender is Window window)
			{
				window.Created -= Window_Created;
			}

			Dispatcher.Dispatch(ProvisionStartupNotificationChannels);
		}

		private void ProvisionStartupNotificationChannels()
		{
			if (_didProvisionStartupNotificationChannels)
			{
				return;
			}

			_didProvisionStartupNotificationChannels = true;
			try
			{
				_serviceProvider.GetRequiredService<ICottonNotificationChannelProvisioningService>()
					.EnsureChannels();
			}
			catch (Exception exception)
			{
				_serviceProvider.GetService<ILogger<App>>()
					?.LogWarning(exception, "Failed to provision Cotton mobile notification channels after startup.");
			}
		}
	}
}
