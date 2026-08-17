using System.Xml;
using System.Xml.Linq;
using Xunit;

namespace Cotton.Mobile.Tests
{
    /// <summary>
    /// Guards the styling rules that the colour palette alone cannot express:
    /// a glyph is only as readable as the tint it is drawn with, and a card is
    /// only visible if its boundary is, because card fill equals page background.
    /// </summary>
    public class AppViewStyleContractTests
    {
        private const string ViewDirectory = "src/Cotton.Mobile";
        private const string AppStylesPath = "src/Cotton.Mobile/Resources/Styles/AppStyles.xaml";
        private static readonly XNamespace XamlNamespace = "http://schemas.microsoft.com/winfx/2009/xaml";

        [Fact]
        public void EveryFontImageSourceDeclaresAnExplicitTint()
        {
            IReadOnlyList<string> offenders = FindUntintedFontImageSources();

            AssertNone(
                offenders,
                "A FontImageSource without Color renders white and disappears on the light surface.");
        }

        [Fact]
        public void AppCardUsesTheHighContrastOutline()
        {
            XDocument document = XDocument.Parse(RepositoryPath.ReadText(AppStylesPath));
            XElement appCardStyle = document
                .Descendants()
                .Single(element =>
                    element.Name.LocalName == "Style"
                    && string.Equals(
                        (string?)element.Attribute(XamlNamespace + "Key"),
                        "AppCard",
                        StringComparison.Ordinal));
            XElement strokeSetter = appCardStyle
                .Elements()
                .Single(element =>
                    element.Name.LocalName == "Setter"
                    && string.Equals(
                        (string?)element.Attribute("Property"),
                        "Stroke",
                        StringComparison.Ordinal));
            string value = (string?)strokeSetter.Attribute("Value")
                ?? throw new InvalidOperationException("AppCard Stroke value is missing.");

            Assert.Contains("StaticResource Outline", value, StringComparison.Ordinal);
            Assert.DoesNotContain("OutlineVariant", value, StringComparison.Ordinal);
        }

        private static List<string> FindUntintedFontImageSources()
        {
            List<string> offenders = [];

            foreach (string file in RepositoryPath.EnumerateFiles(ViewDirectory, "*.xaml"))
            {
                XDocument document = XDocument.Parse(RepositoryPath.ReadText(file), LoadOptions.SetLineInfo);
                IEnumerable<XObject> fontImageSources = document
                    .Descendants()
                    .Where(element =>
                        element.Name.LocalName == "FontImageSource"
                        && element.Attribute("Color") is null)
                    .Cast<XObject>()
                    .Concat(
                        document
                            .Descendants()
                            .Attributes()
                            .Where(attribute =>
                                attribute.Value.Contains("{FontImageSource", StringComparison.Ordinal)
                                && !attribute.Value.Contains("Color=", StringComparison.Ordinal)));

                foreach (XObject source in fontImageSources)
                {
                    IXmlLineInfo lineInfo = (IXmlLineInfo)source;
                    offenders.Add($"{file}:{lineInfo.LineNumber}");
                }
            }

            return offenders;
        }

        private static void AssertNone(IReadOnlyList<string> offenders, string reason)
        {
            if (offenders.Count > 0)
            {
                Assert.Fail($"{reason}{Environment.NewLine}{string.Join(Environment.NewLine, offenders)}");
            }
        }
    }
}
