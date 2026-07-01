// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using System.ComponentModel;
using Cotton.Mobile.Services;
using Cotton.Mobile.ViewModels;

namespace Cotton.Mobile
{
	public partial class MainPage : ContentPage
	{
		private readonly MainPageViewModel _viewModel;
		private double _fileTileHeight = CottonFileTileLayoutPlanner.InitialMetrics.TileHeight;
		private double _fileTileFolderIconSize = CottonFileTileLayoutPlanner.InitialMetrics.FolderIconSize;
		private double _fileTilePreviewHeight = CottonFileTileLayoutPlanner.InitialMetrics.PreviewHeight;
		private double _fileTileSlotWidth = CottonFileTileLayoutPlanner.InitialMetrics.SlotWidth;

		public MainPage(MainPageViewModel viewModel)
		{
			ArgumentNullException.ThrowIfNull(viewModel);

			_viewModel = viewModel;
			InitializeComponent();
			Loaded += MainPage_Loaded;
			SizeChanged += MainPage_SizeChanged;
			RootLayout.SizeChanged += RootLayout_SizeChanged;
			FileBrowserContent.SizeChanged += FileBrowserContent_SizeChanged;
			FileSearchField.PropertyChanged += FileSearchField_PropertyChanged;
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

		private void FileSearchField_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (!string.Equals(e.PropertyName, nameof(VisualElement.IsVisible), StringComparison.Ordinal))
			{
				return;
			}

			if (FileSearchField.IsVisible)
			{
				Dispatcher.DispatchDelayed(
					TimeSpan.FromMilliseconds(50),
					() =>
					{
						if (FileSearchField.IsVisible)
						{
							FileSearchField.FocusInput();
						}
					});
				return;
			}

			FileSearchField.UnfocusInput();
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

			CottonFileTileLayoutMetrics metrics = CottonFileTileLayoutPlanner.Calculate(contentWidth);

			FileTileSlotWidth = metrics.SlotWidth;
			FileTilePreviewHeight = metrics.PreviewHeight;
			FileTileFolderIconSize = metrics.FolderIconSize;
			FileTileHeight = metrics.TileHeight;
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
