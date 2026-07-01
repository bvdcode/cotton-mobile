// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Controls
{
    public class MetadataListSkeletonView : VerticalStackLayout
    {
        public static readonly BindableProperty RowCountProperty = BindableProperty.Create(
            nameof(RowCount),
            typeof(int),
            typeof(MetadataListSkeletonView),
            3,
            propertyChanged: OnRowCountChanged);

        public static readonly BindableProperty IsBodyLineVisibleProperty = BindableProperty.Create(
            nameof(IsBodyLineVisible),
            typeof(bool),
            typeof(MetadataListSkeletonView),
            true,
            propertyChanged: OnBodyLineVisibleChanged);

        public MetadataListSkeletonView()
        {
            InputTransparent = true;
            RebuildRows();
        }

        public int RowCount
        {
            get => (int)GetValue(RowCountProperty);
            set => SetValue(RowCountProperty, value);
        }

        public bool IsBodyLineVisible
        {
            get => (bool)GetValue(IsBodyLineVisibleProperty);
            set => SetValue(IsBodyLineVisibleProperty, value);
        }

        private static void OnRowCountChanged(BindableObject bindable, object oldValue, object newValue)
        {
            MetadataListSkeletonView skeletonView = (MetadataListSkeletonView)bindable;
            skeletonView.RebuildRows();
        }

        private static void OnBodyLineVisibleChanged(BindableObject bindable, object oldValue, object newValue)
        {
            MetadataListSkeletonView skeletonView = (MetadataListSkeletonView)bindable;
            skeletonView.RebuildRows();
        }

        private void RebuildRows()
        {
            Children.Clear();

            int rowCount = Math.Max(0, RowCount);
            for (int index = 0; index < rowCount; index++)
            {
                Children.Add(CreateCard(IsBodyLineVisible));
            }
        }

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
