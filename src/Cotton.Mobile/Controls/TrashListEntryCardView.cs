// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Controls
{
    public class TrashListEntryCardView : TrashEntryCardViewBase
    {
        private readonly ActionClusterView _actions;
        private readonly ContentCardView _card;
        private readonly Grid _grid;
        private readonly FileListMetadataView _metadata;
        private readonly FileThumbnailView _thumbnail;
        private readonly TouchSurfaceView _touchSurface;

        public TrashListEntryCardView()
        {
            _thumbnail = new FileThumbnailView();
            _metadata = new FileListMetadataView
            {
                IsTrailingTextVisible = true,
            };
            _touchSurface = new TouchSurfaceView();
            _actions = new ActionClusterView();

            Grid.SetRowSpan(_thumbnail, 2);
            Grid.SetColumn(_metadata, 1);
            Grid.SetColumnSpan(_touchSurface, 2);
            Grid.SetRow(_actions, 1);
            Grid.SetColumn(_actions, 1);

            _grid = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = GridLength.Auto },
                },
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Star },
                },
                Children =
                {
                    _thumbnail,
                    _metadata,
                    _touchSurface,
                    _actions,
                },
            };

            _card = new ContentCardView
            {
                CardStyleResourceKey = "M3SelectableContentCard",
                BodyContent = _grid,
            };

            Content = _card;
            UpdateVisualState();
        }

        protected override void UpdateVisualState()
        {
            _grid.SetDynamicResource(StyleProperty, "M3MetadataCardGrid");
            _thumbnail.SurfaceStyleResourceKey = "M3MetadataFileThumbnailSurface";
            _metadata.Title = Title ?? string.Empty;
            _metadata.Detail = Detail ?? string.Empty;
            _metadata.TrailingText = BadgeText ?? string.Empty;

            UpdateThumbnail(_thumbnail);
            UpdateTouchSurface(_touchSurface);
            UpdateEntryActions(_actions);
        }
    }
}
