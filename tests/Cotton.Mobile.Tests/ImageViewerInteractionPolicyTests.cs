using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class ImageViewerInteractionPolicyTests
    {
        [Theory]
        [InlineData(0.25, 1)]
        [InlineData(2.5, 2.5)]
        [InlineData(8, 4)]
        public void ClampScale_keeps_zoom_inside_viewer_bounds(double scale, double expectedScale)
        {
            Assert.Equal(expectedScale, CottonImageViewerInteractionPolicy.ClampScale(scale));
        }

        [Fact]
        public void Double_tap_zooms_from_resting_scale()
        {
            CottonImageViewerTransform transform =
                CottonImageViewerInteractionPolicy.CreateDoubleTapTransform(1);

            Assert.Equal(2, transform.Scale);
            Assert.Equal(0, transform.TranslationX);
            Assert.Equal(0, transform.TranslationY);
        }

        [Fact]
        public void Double_tap_resets_from_zoomed_scale()
        {
            CottonImageViewerTransform transform =
                CottonImageViewerInteractionPolicy.CreateDoubleTapTransform(2);

            Assert.Equal(1, transform.Scale);
            Assert.Equal(0, transform.TranslationX);
            Assert.Equal(0, transform.TranslationY);
        }

        [Fact]
        public void ClampTranslation_keeps_zoomed_image_inside_surface_bounds()
        {
            CottonImageViewerTransform transform =
                CottonImageViewerInteractionPolicy.ClampTranslation(
                    imageWidth: 400,
                    imageHeight: 300,
                    surfaceWidth: 240,
                    surfaceHeight: 180,
                    scale: 2,
                    translationX: 500,
                    translationY: -500);

            Assert.Equal(2, transform.Scale);
            Assert.Equal(280, transform.TranslationX);
            Assert.Equal(-210, transform.TranslationY);
        }

        [Fact]
        public void ClampTranslation_preserves_transform_until_image_is_measured()
        {
            CottonImageViewerTransform transform =
                CottonImageViewerInteractionPolicy.ClampTranslation(
                    imageWidth: 0,
                    imageHeight: 300,
                    surfaceWidth: 240,
                    surfaceHeight: 180,
                    scale: 2,
                    translationX: 50,
                    translationY: -60);

            Assert.Equal(2, transform.Scale);
            Assert.Equal(50, transform.TranslationX);
            Assert.Equal(-60, transform.TranslationY);
        }
    }
}
