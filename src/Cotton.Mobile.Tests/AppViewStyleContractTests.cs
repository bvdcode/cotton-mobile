using System.Xml;
using System.Xml.Linq;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class AppViewStyleContractTests
    {
        private const string ViewDirectory = "src/Cotton.Mobile";

        [Fact]
        public void SyncOperationFeedbackIsVisibleOutsideTheRootList()
        {
            XDocument document = XDocument.Parse(
                RepositoryPath.ReadText($"{ViewDirectory}/SyncDashboardView.xaml"));
            XElement status = document.Descendants().Single(element =>
                GetAttribute(element, "AutomationId")?.Value == "SyncStatus");

            Assert.Equal("{Binding Status}", GetAttribute(status, "Text")?.Value);
            Assert.Equal("{Binding IsStatusVisible}", GetAttribute(status, "IsVisible")?.Value);
            Assert.DoesNotContain(status.Ancestors(), element =>
                element.Name.LocalName is "DataTemplate" or "CollectionView");
        }

        [Fact]
        public void EveryFontImageSourceDeclaresAnExplicitTint()
        {
            IReadOnlyList<string> offenders = FindUntintedFontImageSources();

            AssertNone(
                offenders,
                "A FontImageSource without Color renders white and disappears on the light surface.");
        }

        [Fact]
        public void SyncNavigationAnnouncesTheActionItExecutes()
        {
            XDocument document = XDocument.Parse(RepositoryPath.ReadText($"{ViewDirectory}/MainPage.xaml"));
            XElement button = document.Descendants().Single(element =>
                element.Name.LocalName == "Button"
                && GetAttribute(element, "Command")?.Value.Contains("ShowSyncCommand", StringComparison.Ordinal) == true);

            Assert.Contains(
                "OpenSyncDescription",
                GetAttributeContaining(button, "Description")?.Value,
                StringComparison.Ordinal);
        }

        [Fact]
        public void SyncStatusActionUsesTheSameAccessibleTextLayoutAsProgress()
        {
            XDocument document = XDocument.Parse(
                RepositoryPath.ReadText($"{ViewDirectory}/SyncDashboardView.xaml"));
            XElement gesture = document.Descendants().Single(element =>
                element.Name.LocalName == "TapGestureRecognizer"
                && GetAttribute(element, "CommandParameter")?.Value.Contains("StatusAction", StringComparison.Ordinal)
                    == true);
            XElement container = gesture.Ancestors().First(element => element.Name.LocalName == "Grid");
            XElement status = container.Descendants().Single(element =>
                element.Name.LocalName == "Label"
                && GetAttribute(element, "Text")?.Value.Contains("StatusText", StringComparison.Ordinal) == true);

            Assert.Equal("{StaticResource AppTouchTargetSize}", GetAttribute(container, "MinimumHeightRequest")?.Value);
            Assert.Contains(
                "StatusActionText",
                GetAttributeContaining(container, "Description")?.Value,
                StringComparison.Ordinal);
            Assert.Equal("Label", status.Name.LocalName);
            Assert.DoesNotContain(container.Descendants(), element => element.Name.LocalName == "Button");
        }

        private static XAttribute? GetAttribute(XElement element, string localName)
        {
            return element.Attributes().SingleOrDefault(attribute => attribute.Name.LocalName == localName);
        }

        private static XAttribute? GetAttributeContaining(XElement element, string namePart)
        {
            return element.Attributes().SingleOrDefault(attribute =>
                attribute.Name.LocalName.Contains(namePart, StringComparison.Ordinal));
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
