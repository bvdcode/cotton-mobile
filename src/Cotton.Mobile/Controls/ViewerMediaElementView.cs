// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;

namespace Cotton.Mobile.Controls
{
    public class ViewerMediaElementView : MediaElement
    {
        private const string DefaultMediaStyleResourceKey = "M3ViewerMediaElement";

        public ViewerMediaElementView()
        {
            AndroidViewType = AndroidViewType.TextureView;
            Aspect = Aspect.AspectFit;
            ShouldAutoPlay = false;
            ShouldLoopPlayback = false;
            ShouldShowPlaybackControls = true;
            SetDynamicResource(StyleProperty, DefaultMediaStyleResourceKey);
        }
    }
}
