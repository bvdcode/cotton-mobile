using System.Xml;
using System.Xml.Linq;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class AppViewStyleContractTests
    {
        private const string ViewDirectory = "src/Cotton.Mobile";
        [Fact]
        public void EveryFontImageSourceDeclaresAnExplicitTint()
        {
            IReadOnlyList<string> offenders = FindUntintedFontImageSources();

            AssertNone(
                offenders,
                "A FontImageSource without Color renders white and disappears on the light surface.");
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
