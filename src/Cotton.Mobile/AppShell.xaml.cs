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
