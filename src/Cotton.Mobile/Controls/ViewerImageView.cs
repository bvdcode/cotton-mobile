// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Controls
{
    public class ViewerImageView : Image
    {
        private const string DefaultImageStyleResourceKey = "M3ViewerImage";

        public ViewerImageView()
        {
            Aspect = Aspect.AspectFit;
            SetDynamicResource(StyleProperty, DefaultImageStyleResourceKey);
        }
    }
}
