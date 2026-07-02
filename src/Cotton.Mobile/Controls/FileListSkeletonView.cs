// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Controls
{
    public class FileListSkeletonView : SkeletonListView
    {
        private const string DefaultStyleResourceKey = "M3FileListSkeletonView";

        public FileListSkeletonView()
        {
            SetDynamicResource(StyleProperty, DefaultStyleResourceKey);
            RebuildRows();
        }

        protected override View CreateRow()
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
