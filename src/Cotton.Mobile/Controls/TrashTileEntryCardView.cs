// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Controls
{
    public class TrashTileEntryCardView : TrashEntryCardViewBase
    {
        private readonly ActionClusterView _actions;
        private readonly ContentCardView _card;
        private readonly Grid _grid;
        private readonly FileTileMetadataView _metadata;
        private readonly FileThumbnailView _thumbnail;
        private readonly TouchSurfaceView _touchSurface;

        public TrashTileEntryCardView()
        {
            _thumbnail = new FileThumbnailView
            {
                SurfaceStyleResourceKey = "M3TrashTilePreviewSurface",
                SelectionMarkStyleResourceKey = "M3FileTileSelectionMark",
                IsBadgeVisible = true,
            };
            _metadata = new FileTileMetadataView();
            _touchSurface = new TouchSurfaceView();
            _actions = new ActionClusterView();

            Grid.SetRow(_metadata, 1);
            Grid.SetRowSpan(_touchSurface, 2);
            Grid.SetRow(_actions, 2);

            _grid = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = GridLength.Auto },
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
                CardStyleResourceKey = "M3SelectableTrashTileCard",
                BodyContent = _grid,
            };

            Content = _card;
            UpdateVisualState();
        }

        protected override void UpdateVisualState()
        {
            _grid.SetDynamicResource(StyleProperty, "M3FileTileContentGrid");
            _metadata.Title = Title ?? string.Empty;
            _metadata.Detail = Detail ?? string.Empty;

            UpdateThumbnail(_thumbnail, BadgeText ?? string.Empty, isBadgeVisible: true);
            UpdateTouchSurface(_touchSurface);
            UpdateEntryActions(_actions);
        }
    }
}
