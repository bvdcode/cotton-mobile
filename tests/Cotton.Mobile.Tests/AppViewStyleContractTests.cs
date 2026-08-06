using System.Text.RegularExpressions;
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

        private static readonly Regex FontImageSourcePattern = new(
            "<FontImageSource\\b[^>]*?/>|\"\\{FontImageSource[^\"]*\"",
            RegexOptions.Singleline | RegexOptions.CultureInvariant);

        [Fact]
        public void Every_font_image_source_declares_an_explicit_tint()
        {
            IReadOnlyList<string> offenders = FindOffenders(
                FontImageSourcePattern,
                match => !match.Value.Contains("Color=", StringComparison.Ordinal));

            AssertNone(
                offenders,
                "A FontImageSource without Color renders white and disappears on the light surface.");
        }

        [Fact]
        public void App_card_uses_the_high_contrast_outline()
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

        [Fact]
        public void Card_actions_can_grow_with_accessible_font_scaling()
        {
            string markup = RepositoryPath.ReadText(AppStylesPath);
            Match cardActionStyle = Regex.Match(
                markup,
                "<Style[^>]*Class=\"CardAction\"[\\s\\S]*?</Style>",
                RegexOptions.CultureInvariant);

            Assert.True(cardActionStyle.Success, "CardAction style is missing.");
            Assert.DoesNotContain("HeightRequest", cardActionStyle.Value, StringComparison.Ordinal);
            Assert.Contains("Property=\"Padding\"", cardActionStyle.Value, StringComparison.Ordinal);
        }

        private static IReadOnlyList<string> FindOffenders(Regex pattern, Func<Match, bool> isOffending)
        {
            List<string> offenders = [];

            foreach (string file in RepositoryPath.EnumerateFiles(ViewDirectory, "*.xaml"))
            {
                string markup = RepositoryPath.ReadText(file);
                foreach (Match match in pattern.Matches(markup))
                {
                    if (isOffending(match))
                    {
                        int line = markup.Take(match.Index).Count(character => character == '\n') + 1;
                        offenders.Add($"{file}:{line}");
                    }
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
