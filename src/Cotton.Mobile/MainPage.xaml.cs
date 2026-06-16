using Cotton.Mobile.ViewModels;

namespace Cotton.Mobile
{
	public partial class MainPage : ContentPage
	{
		private readonly MainPageViewModel _viewModel;

		public MainPage(MainPageViewModel viewModel)
		{
			ArgumentNullException.ThrowIfNull(viewModel);

			_viewModel = viewModel;
			InitializeComponent();
			BindingContext = _viewModel;
		}

		protected override async void OnAppearing()
		{
			base.OnAppearing();

			await _viewModel.RestoreSessionOnceAsync();
		}
	}
}
