// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Controls
{
    public class MetadataListSkeletonView : SkeletonListView
    {
        public static readonly BindableProperty IsBodyLineVisibleProperty = BindableProperty.Create(
            nameof(IsBodyLineVisible),
            typeof(bool),
            typeof(MetadataListSkeletonView),
            true,
            propertyChanged: OnBodyLineVisibleChanged);

        public MetadataListSkeletonView()
        {
            RebuildRows();
        }

        public bool IsBodyLineVisible
        {
            get => (bool)GetValue(IsBodyLineVisibleProperty);
            set => SetValue(IsBodyLineVisibleProperty, value);
        }

        private static void OnBodyLineVisibleChanged(BindableObject bindable, object oldValue, object newValue)
        {
            MetadataListSkeletonView skeletonView = (MetadataListSkeletonView)bindable;
            skeletonView.RebuildRows();
        }

        protected override View CreateRow() => CreateCard(IsBodyLineVisible);

        private static Border CreateCard(bool isBodyLineVisible)
        {
            Grid grid = new();
            grid.SetDynamicResource(StyleProperty, "M3MetadataSkeletonGrid");
            grid.RowDefinitions.Add(new RowDefinition
            {
                Height = GridLength.Auto,
            });
            if (isBodyLineVisible)
            {
                grid.RowDefinitions.Add(new RowDefinition
                {
                    Height = GridLength.Auto,
                });
            }

            grid.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = GridLength.Auto,
            });
            grid.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = GridLength.Star,
            });
            grid.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = GridLength.Auto,
            });

            SkeletonBlock thumbnail = CreateSkeletonBlock("M3MetadataSkeletonThumbnailBlock");
            Grid.SetRowSpan(thumbnail, isBodyLineVisible ? 2 : 1);
            grid.Add(thumbnail);

            VerticalStackLayout textStack = new()
            {
                Children =
                {
                    CreateSkeletonBlock("M3MetadataSkeletonPrimaryLineBlock"),
                    CreateSkeletonBlock("M3MetadataSkeletonSecondaryLineBlock"),
                },
            };
            textStack.SetDynamicResource(StyleProperty, "M3MetadataSkeletonTextStack");
            grid.Add(textStack, 1);

            grid.Add(CreateSkeletonBlock("M3MetadataSkeletonChipBlock"), 2);

            if (isBodyLineVisible)
            {
                SkeletonBlock bodyLine = CreateSkeletonBlock("M3MetadataSkeletonBodyLineBlock");
                Grid.SetRow(bodyLine, 1);
                Grid.SetColumn(bodyLine, 1);
                Grid.SetColumnSpan(bodyLine, 2);
                grid.Add(bodyLine);
            }

            Border card = new()
            {
                Content = grid,
            };
            card.SetDynamicResource(StyleProperty, "M3MetadataSkeletonCard");
            return card;
        }

        private static SkeletonBlock CreateSkeletonBlock(string styleKey)
        {
            SkeletonBlock skeletonBlock = new();
            skeletonBlock.SetDynamicResource(StyleProperty, styleKey);
            return skeletonBlock;
        }
    }
}
