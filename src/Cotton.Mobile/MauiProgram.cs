using Cotton.Mobile.Services;
using Cotton.Sdk.Auth;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;

namespace Cotton.Mobile
{
	public static class MauiProgram
	{
		public static MauiApp CreateMauiApp()
		{
			var builder = MauiApp.CreateBuilder();
			builder
				.UseMauiApp<App>()
				.ConfigureFonts(fonts =>
				{
					fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
					fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
				});

			builder.Services.AddSingleton<ISecureStorage>(SecureStorage.Default);
			builder.Services.AddSingleton<IPreferences>(Preferences.Default);
			builder.Services.AddSingleton<IBrowser>(Browser.Default);
			builder.Services.AddSingleton(
				new CottonMobileOptions(
					"Cotton Cloud",
					new Uri("https://app.cottoncloud.dev"),
					new Uri("https://cottoncloud.dev/privacy-policy")));
			builder.Services.AddSingleton<IApplicationForegroundService, ApplicationForegroundService>();
			builder.Services.AddSingleton<ICottonMobileApplicationMetadata, CottonMobileApplicationMetadata>();
			builder.Services.AddSingleton<ICottonTokenStore, SecureStorageCottonTokenStore>();
			builder.Services.AddSingleton<ICottonInstanceStore, PreferencesCottonInstanceStore>();
			builder.Services.AddSingleton<ICottonClientFactory, CottonClientFactory>();
			builder.Services.AddSingleton<ICottonSessionService, CottonSessionService>();
			builder.Services.AddSingleton<AppShell>();
			builder.Services.AddTransient<MainPage>();

#if DEBUG
			builder.Logging.AddDebug();
#endif

			return builder.Build();
		}
	}
}
