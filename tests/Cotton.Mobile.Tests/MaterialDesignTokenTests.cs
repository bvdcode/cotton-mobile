using System.Globalization;
using System.Xml.Linq;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class MaterialDesignTokenTests
    {
        private const string SpacingResourcePath = "src/Cotton.Mobile/Resources/Styles/Theme/MSpacing.xaml";
        private const string InteractionResourcePath = "src/Cotton.Mobile/Resources/Styles/Theme/MInteraction.xaml";
        private const string StylesResourcePath = "src/Cotton.Mobile/Resources/Styles/Styles.xaml";
        private const string MainPagePath = "src/Cotton.Mobile/MainPage.xaml";
        private static readonly XNamespace XamlNamespace = "http://schemas.microsoft.com/winfx/2009/xaml";

        [Fact]
        public void Interactive_spacing_tokens_meet_android_touch_target()
        {
            XDocument spacing = LoadResourceDictionary(SpacingResourcePath);

            double touchTarget = GetDoubleResource(spacing, "TouchTarget");

            Assert.Equal(48, touchTarget);
            Assert.True(GetDoubleResource(spacing, "M3ButtonMinSize") >= touchTarget);
            Assert.True(GetDoubleResource(spacing, "M3ControlMinSize") >= touchTarget);
            Assert.True(GetDoubleResource(spacing, "M3FooterLinkMinHeight") >= touchTarget);
            Assert.True(GetDoubleResource(spacing, "M3FilledButtonHeight") >= touchTarget);
        }

        [Fact]
        public void Material_switch_style_uses_shared_touch_target()
        {
            XDocument styles = LoadResourceDictionary(StylesResourcePath);

            IReadOnlyDictionary<string, string> switchSetters = GetStyleSetters(styles, "M3Switch");

            Assert.Equal("{StaticResource TouchTarget}", switchSetters["TouchTargetSize"]);
        }

        [Fact]
        public void File_browser_loading_skeleton_uses_material_geometry_and_motion()
        {
            XDocument spacing = LoadResourceDictionary(SpacingResourcePath);
            XDocument interaction = LoadResourceDictionary(InteractionResourcePath);
            XDocument styles = LoadResourceDictionary(StylesResourcePath);

            Assert.True(GetDoubleResource(spacing, "M3FileSkeletonLineHeight") > 0);
            Assert.True(GetDoubleResource(spacing, "M3FileSkeletonSecondaryLineHeight") > 0);
            Assert.True(GetDoubleResource(spacing, "M3FileSkeletonPrimaryLineWidth") > 0);
            Assert.True(GetDoubleResource(spacing, "M3FileSkeletonSecondaryLineWidth") > 0);
            Assert.True(GetDoubleResource(interaction, "M3SkeletonIdleOpacity") < 1);
            Assert.True(GetDoubleResource(interaction, "M3SkeletonPulseOpacity") > GetDoubleResource(interaction, "M3SkeletonIdleOpacity"));
            Assert.True(GetIntResource(interaction, "M3MotionSkeletonPulseDuration") >= 1000);

            IReadOnlyDictionary<string, string> skeletonSetters = GetStyleSetters(styles, "M3SkeletonBlock");
            IReadOnlyDictionary<string, string> listSetters = GetStyleSetters(styles, "M3FileListSkeletonView");
            IReadOnlyDictionary<string, string> rowSetters = GetStyleSetters(styles, "M3FileSkeletonRowGrid");

            Assert.Equal("{AppThemeBinding Light={StaticResource M3LightSurfaceContainerHigh}, Dark={StaticResource M3DarkSurfaceContainerHigh}}", skeletonSetters["BackgroundColor"]);
            Assert.Equal("{StaticResource M3SkeletonIdleOpacity}", skeletonSetters["IdleOpacity"]);
            Assert.Equal("{StaticResource M3SkeletonPulseOpacity}", skeletonSetters["PulseOpacity"]);
            Assert.Equal("{StaticResource M3MotionSkeletonPulseDuration}", skeletonSetters["PulseDuration"]);
            Assert.Equal("{StaticResource SpaceNone}", listSetters["Spacing"]);
            Assert.Equal("True", listSetters["InputTransparent"]);
            Assert.Equal("{StaticResource M3FileRowPadding}", rowSetters["Padding"]);
            Assert.Equal("{StaticResource M3FileRowHeight}", rowSetters["HeightRequest"]);
            Assert.Equal("{StaticResource Space12}", rowSetters["ColumnSpacing"]);
        }

        [Fact]
        public void Main_file_browser_uses_reusable_loading_skeleton_view()
        {
            string mainPage = LoadText(MainPagePath);

            Assert.Contains("<controls:FileListSkeletonView", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("M3FileSkeletonRowGrid", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("M3FileSkeletonPrimaryLineBlock", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("M3FileSkeletonSecondaryLineBlock", mainPage, StringComparison.Ordinal);
        }

        private static XDocument LoadResourceDictionary(string relativePath)
        {
            string repositoryRoot = FindRepositoryRoot(relativePath);
            string resourcePath = Path.Combine(repositoryRoot, relativePath);
            return XDocument.Load(resourcePath);
        }

        private static string LoadText(string relativePath)
        {
            string repositoryRoot = FindRepositoryRoot(relativePath);
            string resourcePath = Path.Combine(repositoryRoot, relativePath);
            return File.ReadAllText(resourcePath);
        }

        private static string FindRepositoryRoot(string relativePath)
        {
            DirectoryInfo? directory = new(AppContext.BaseDirectory);
            while (directory is not null)
            {
                string candidate = Path.Combine(directory.FullName, relativePath);
                if (File.Exists(candidate))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            throw new DirectoryNotFoundException($"Could not find repository root containing {relativePath}.");
        }

        private static double GetDoubleResource(XDocument document, string key)
        {
            XElement element = document.Descendants()
                .Single(descendant => string.Equals(
                    (string?)descendant.Attribute(XamlNamespace + "Key"),
                    key,
                    StringComparison.Ordinal));
            return double.Parse(element.Value, CultureInfo.InvariantCulture);
        }

        private static int GetIntResource(XDocument document, string key)
        {
            XElement element = document.Descendants()
                .Single(descendant => string.Equals(
                    (string?)descendant.Attribute(XamlNamespace + "Key"),
                    key,
                    StringComparison.Ordinal));
            return int.Parse(element.Value, CultureInfo.InvariantCulture);
        }

        private static IReadOnlyDictionary<string, string> GetStyleSetters(XDocument document, string styleKey)
        {
            XElement style = document.Descendants()
                .Single(descendant => string.Equals(
                    descendant.Name.LocalName,
                    "Style",
                    StringComparison.Ordinal)
                    && string.Equals(
                        (string?)descendant.Attribute(XamlNamespace + "Key"),
                        styleKey,
                        StringComparison.Ordinal));

            return style.Elements()
                .Where(element => string.Equals(element.Name.LocalName, "Setter", StringComparison.Ordinal))
                .ToDictionary(
                    element => (string)element.Attribute("Property")!,
                    element => (string)element.Attribute("Value")!,
                    StringComparer.Ordinal);
        }
    }
}
