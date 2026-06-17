using System.ComponentModel;
using Cotton.Mobile.ViewModels;

namespace Cotton.Mobile
{
	public partial class MainPage : ContentPage
	{
		private const double PageHorizontalPadding = 40;
		private const double ContentMaximumWidth = 520;
		private const double FileTileHorizontalMargin = 4;
		private const double FileTileMinimumWidth = 112;
		private const double FileTileMaximumWidth = 180;
		private const double FileTilePreviewRatio = 0.54;
		private const double FileTileVerticalChrome = 64;

		private readonly MainPageViewModel _viewModel;
		private double _fileTileHeight = 146;
		private double _fileTilePreviewHeight = 72;
		private double _fileTileWidth = 150;

		public MainPage(MainPageViewModel viewModel)
		{
			ArgumentNullException.ThrowIfNull(viewModel);

			_viewModel = viewModel;
			InitializeComponent();
			Loaded += MainPage_Loaded;
			SizeChanged += MainPage_SizeChanged;
			FileBrowserContent.SizeChanged += FileBrowserContent_SizeChanged;
			FileSearchBar.PropertyChanged += FileSearchBar_PropertyChanged;
			BindingContext = _viewModel;
		}

		public double FileTileWidth
		{
			get => _fileTileWidth;
			private set => SetPageProperty(ref _fileTileWidth, value, nameof(FileTileWidth));
		}

		public double FileTilePreviewHeight
		{
			get => _fileTilePreviewHeight;
			private set => SetPageProperty(ref _fileTilePreviewHeight, value, nameof(FileTilePreviewHeight));
		}

		public double FileTileHeight
		{
			get => _fileTileHeight;
			private set => SetPageProperty(ref _fileTileHeight, value, nameof(FileTileHeight));
		}

		protected override async void OnAppearing()
		{
			base.OnAppearing();

			await _viewModel.RestoreSessionOnceAsync();
		}

		private void MainPage_Loaded(object? sender, EventArgs e)
		{
			UpdateFileTileMetrics();
		}

		private void MainPage_SizeChanged(object? sender, EventArgs e)
		{
			UpdateFileTileMetrics();
		}

		private void FileBrowserContent_SizeChanged(object? sender, EventArgs e)
		{
			UpdateFileTileMetrics();
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

		private void UpdateFileTileMetrics()
		{
			double contentWidth = Width > PageHorizontalPadding
				? Width - PageHorizontalPadding
				: FileBrowserContent.Width;
			if (contentWidth <= 0)
			{
				return;
			}

			contentWidth = Math.Min(contentWidth, ContentMaximumWidth);
			double tileHorizontalChrome = FileTileHorizontalMargin * 2;
			int columnCount = Math.Max(
				2,
				(int)Math.Floor(contentWidth / (FileTileMinimumWidth + tileHorizontalChrome)));
			double totalTileHorizontalChrome = tileHorizontalChrome * columnCount;
			double tileWidth = Math.Floor((contentWidth - totalTileHorizontalChrome) / columnCount);

			while (tileWidth < FileTileMinimumWidth && columnCount > 2)
			{
				columnCount--;
				totalTileHorizontalChrome = tileHorizontalChrome * columnCount;
				tileWidth = Math.Floor((contentWidth - totalTileHorizontalChrome) / columnCount);
			}

			tileWidth = Math.Clamp(tileWidth, FileTileMinimumWidth, FileTileMaximumWidth);
			double previewHeight = Math.Round(tileWidth * FileTilePreviewRatio);

			FileTileWidth = tileWidth;
			FileTilePreviewHeight = previewHeight;
			FileTileHeight = previewHeight + FileTileVerticalChrome;
		}

		private void SetPageProperty(ref double field, double value, string propertyName)
		{
			if (Math.Abs(field - value) < 0.5)
			{
				return;
			}

			field = value;
			OnPropertyChanged(propertyName);
		}
	}
}
