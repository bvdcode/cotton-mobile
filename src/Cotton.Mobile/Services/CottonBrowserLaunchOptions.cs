using Microsoft.Maui.ApplicationModel;

namespace Cotton.Mobile.Services
{
	public static class CottonBrowserLaunchOptions
	{
		public static BrowserLaunchOptions External()
		{
			return new BrowserLaunchOptions
			{
				LaunchMode = BrowserLaunchMode.External,
				TitleMode = BrowserTitleMode.Show,
			};
		}
	}
}
