using System.Globalization;
using System.Xml.Linq;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class MaterialDesignTokenTests
    {
        private const string SpacingResourcePath = "src/Cotton.Mobile/Resources/Styles/Theme/MSpacing.xaml";
        private const string StylesResourcePath = "src/Cotton.Mobile/Resources/Styles/Styles.xaml";
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

        private static XDocument LoadResourceDictionary(string relativePath)
        {
            string repositoryRoot = FindRepositoryRoot(relativePath);
            string resourcePath = Path.Combine(repositoryRoot, relativePath);
            return XDocument.Load(resourcePath);
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
