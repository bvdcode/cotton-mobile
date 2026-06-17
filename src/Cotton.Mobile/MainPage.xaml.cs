using System.ComponentModel;
using Cotton.Mobile.ViewModels;

namespace Cotton.Mobile
{
	public partial class MainPage : ContentPage
	{
		private const double PageHorizontalPadding = 32;
		private const double ContentMaximumWidth = 520;
		private const double FileTileSlotHorizontalPadding = 6;
		private const double FileTileMinimumSlotWidth = 90;
		private const double FileTileMinimumWidth = 84;
		private const double FileTilePreviewRatio = 0.62;
		private const double FileTileVerticalChrome = 62;
		private const int FileTileMaximumColumnCount = 5;

		private readonly MainPageViewModel _viewModel;
		private double _fileTileHeight = 146;
		private double _fileTilePreviewHeight = 72;
		private double _fileTileSlotWidth = 150;

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

		public double FileTileSlotWidth
		{
			get => _fileTileSlotWidth;
			private set => SetPageProperty(ref _fileTileSlotWidth, value, nameof(FileTileSlotWidth));
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
			double contentWidth = FileBrowserContent.Width > 0
				? FileBrowserContent.Width
				: Width - PageHorizontalPadding;
			if (contentWidth <= 0)
			{
				return;
			}

			contentWidth = Math.Min(contentWidth, ContentMaximumWidth);
			int columnCount = Math.Clamp(
				(int)Math.Floor(contentWidth / FileTileMinimumSlotWidth),
				2,
				FileTileMaximumColumnCount);
			double slotWidth = Math.Floor(contentWidth / columnCount);
			double tileWidth = slotWidth - FileTileSlotHorizontalPadding;

			while (tileWidth < FileTileMinimumWidth && columnCount > 2)
			{
				columnCount--;
				slotWidth = Math.Floor(contentWidth / columnCount);
				tileWidth = slotWidth - FileTileSlotHorizontalPadding;
			}

			tileWidth = Math.Max(tileWidth, FileTileMinimumWidth);
			double previewHeight = Math.Round(tileWidth * FileTilePreviewRatio);

			FileTileSlotWidth = slotWidth;
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
