using System.ComponentModel;
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
			FileSearchBar.PropertyChanged += FileSearchBar_PropertyChanged;
			BindingContext = _viewModel;
		}

		protected override async void OnAppearing()
		{
			base.OnAppearing();

			await _viewModel.RestoreSessionOnceAsync();
		}

		private void FileSearchBar_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (!string.Equals(e.PropertyName, nameof(VisualElement.IsVisible), StringComparison.Ordinal))
			{
				return;
			}

			if (FileSearchBar.IsVisible)
			{
				Dispatcher.DispatchDelayed(
					TimeSpan.FromMilliseconds(50),
					() => FileSearchBar.Focus());
				return;
			}

			FileSearchBar.Unfocus();
		}
	}
}
