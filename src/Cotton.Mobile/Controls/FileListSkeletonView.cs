// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Controls
{
    public class FileListSkeletonView : VerticalStackLayout
    {
        public static readonly BindableProperty RowCountProperty = BindableProperty.Create(
            nameof(RowCount),
            typeof(int),
            typeof(FileListSkeletonView),
            3,
            propertyChanged: OnRowCountChanged);

        public FileListSkeletonView()
        {
            InputTransparent = true;
            RebuildRows();
        }

        public int RowCount
        {
            get => (int)GetValue(RowCountProperty);
            set => SetValue(RowCountProperty, value);
        }

        private static void OnRowCountChanged(BindableObject bindable, object oldValue, object newValue)
        {
            FileListSkeletonView skeletonView = (FileListSkeletonView)bindable;
            skeletonView.RebuildRows();
        }

        private void RebuildRows()
        {
            Children.Clear();

            int rowCount = Math.Max(0, RowCount);
            for (int index = 0; index < rowCount; index++)
            {
                Children.Add(CreateRow());
            }
        }

        private static Grid CreateRow()
        {
            Grid row = new();
            row.SetDynamicResource(StyleProperty, "M3FileSkeletonRowGrid");
            row.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = new GridLength(MaterialResources.Get<double>("M3FileListThumbnailColumnWidth")),
            });
            row.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = GridLength.Star,
            });
            row.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = new GridLength(MaterialResources.Get<double>("M3FileActionSize")),
            });

            row.Add(CreateSkeletonBlock("M3FileSkeletonThumbnailBlock"));

            VerticalStackLayout textStack = new()
            {
                Children =
                {
                    CreateSkeletonBlock("M3FileSkeletonPrimaryLineBlock"),
                    CreateSkeletonBlock("M3FileSkeletonSecondaryLineBlock"),
                },
            };
            textStack.SetDynamicResource(StyleProperty, "M3FileSkeletonTextStack");
            row.Add(textStack, 1);

            row.Add(CreateSkeletonBlock("M3FileSkeletonActionBlock"), 2);

            return row;
        }

        private static SkeletonBlock CreateSkeletonBlock(string styleKey)
        {
            SkeletonBlock skeletonBlock = new();
            skeletonBlock.SetDynamicResource(StyleProperty, styleKey);
            return skeletonBlock;
        }
    }
}
