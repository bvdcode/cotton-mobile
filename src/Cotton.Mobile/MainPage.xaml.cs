using System.ComponentModel;
using Cotton.Mobile.ViewModels;

namespace Cotton.Mobile
{
	public partial class MainPage : ContentPage
	{
		private const double FileTileSlotHorizontalPadding = 2;
		private const double FileTileSlotRoundingGuard = 1;
		private const int FileTilePreferredColumnCount = 2;
		private const double FileTileMinimumColumnWidth = 140;
		private const double FileTilePreviewRatio = 0.62;
		private const double FileTileFolderIconMinimumSize = 62;
		private const double FileTileFolderIconMaximumSize = 92;
		private const double FileTileFolderIconWidthRatio = 0.42;
		private const double FileTileVerticalChrome = 54;

		private readonly MainPageViewModel _viewModel;
		private double _fileTileHeight = 146;
		private double _fileTileFolderIconSize = FileTileFolderIconMinimumSize;
		private double _fileTilePreviewHeight = 72;
		private double _fileTileSlotWidth = 150;

		public MainPage(MainPageViewModel viewModel)
		{
			ArgumentNullException.ThrowIfNull(viewModel);

			_viewModel = viewModel;
			InitializeComponent();
			Loaded += MainPage_Loaded;
			SizeChanged += MainPage_SizeChanged;
			RootLayout.SizeChanged += RootLayout_SizeChanged;
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

		public double FileTileFolderIconSize
		{
			get => _fileTileFolderIconSize;
			private set => SetPageProperty(ref _fileTileFolderIconSize, value, nameof(FileTileFolderIconSize));
		}

		public double FileTileHeight
		{
			get => _fileTileHeight;
			private set => SetPageProperty(ref _fileTileHeight, value, nameof(FileTileHeight));
		}

		protected override async void OnAppearing()
		{
			base.OnAppearing();

			bool restoredSession = await _viewModel.RestoreSessionOnceAsync();
			if (!restoredSession)
			{
				await _viewModel.RefreshTransferActivityAsync();
			}
		}

		private void MainPage_Loaded(object? sender, EventArgs e)
		{
			UpdateFileTileMetrics();
		}

		private void MainPage_SizeChanged(object? sender, EventArgs e)
		{
			UpdateFileTileMetrics();
		}

		private void RootLayout_SizeChanged(object? sender, EventArgs e)
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
					() =>
					{
						if (FileSearchBar.IsVisible)
						{
							FileSearchBar.Focus();
						}
					});
				return;
			}

			FileSearchBar.Unfocus();
		}

		private void UpdateFileTileMetrics()
		{
			double measuredContentWidth = FileBrowserContent.Width > 0
				? FileBrowserContent.Width
				: 0;
			double rootWidth = RootLayout.Width > 0
				? RootLayout.Width
				: 0;
			double contentWidth = measuredContentWidth > 0
				? measuredContentWidth
				: rootWidth;
			if (contentWidth <= 0)
			{
				return;
			}

			double horizontalPadding = RootLayout.Padding.HorizontalThickness;
			if (measuredContentWidth <= 0 && rootWidth > horizontalPadding)
			{
				contentWidth = rootWidth - horizontalPadding;
			}

			if (contentWidth <= 0)
			{
				return;
			}

			double slotWidth = ResolveFileTileSlotWidth(contentWidth);
			double tileWidth = slotWidth - FileTileSlotHorizontalPadding;

			double previewHeight = Math.Round(tileWidth * FileTilePreviewRatio);

			FileTileSlotWidth = slotWidth;
			FileTilePreviewHeight = previewHeight;
			FileTileFolderIconSize = Math.Clamp(
				Math.Round(tileWidth * FileTileFolderIconWidthRatio),
				FileTileFolderIconMinimumSize,
				FileTileFolderIconMaximumSize);
			FileTileHeight = previewHeight + FileTileVerticalChrome;
		}

		private static double ResolveFileTileSlotWidth(double contentWidth)
		{
			int columnCount = contentWidth >= FileTileMinimumColumnWidth * FileTilePreferredColumnCount
				? FileTilePreferredColumnCount
				: 1;
			return Math.Max(
				1,
				Math.Floor(contentWidth / columnCount) - FileTileSlotRoundingGuard);
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
