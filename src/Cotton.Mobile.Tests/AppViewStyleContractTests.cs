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
        public void AuthenticatedNavigationUsesTheMaterialTabView()
        {
            XDocument document = XDocument.Parse(RepositoryPath.ReadText($"{ViewDirectory}/MainPage.xaml"));
            XElement tabView = document.Descendants().Single(element =>
                element.Name.LocalName == "TabView");
            List<XElement> tabs = [.. tabView.Elements().Where(element =>
                element.Name.LocalName == "TabItem")];

            Assert.Equal("Bottom", GetAttribute(tabView, "TabPlacement")?.Value);
            Assert.Equal(
                "{Binding Display.SelectedDestination, Mode=TwoWay}",
                GetAttribute(tabView, "CurrentItem")?.Value);
            Assert.Equal(2, tabs.Count);
            Assert.Contains(tabs, tab =>
                GetAttribute(tab, "Title")?.Value.Contains("SyncTitle", StringComparison.Ordinal) == true);
            Assert.Contains(tabs, tab =>
                GetAttribute(tab, "Title")?.Value.Contains("ProfileTitle", StringComparison.Ordinal) == true);
        }

        [Fact]
        public void MainPageProvidesItsBindingContextBeforeTabViewInitialization()
        {
            string source = RepositoryPath.ReadText($"{ViewDirectory}/MainPage.xaml.cs");

            int bindingContext = source.IndexOf("BindingContext = viewModel;", StringComparison.Ordinal);
            int initialization = source.IndexOf("InitializeComponent();", StringComparison.Ordinal);

            Assert.True(bindingContext >= 0);
            Assert.True(initialization > bindingContext);
        }

        [Fact]
        public void SyncDashboardSeparatesPhotoAndFolderBackupActions()
        {
            XDocument document = XDocument.Parse(
                RepositoryPath.ReadText($"{ViewDirectory}/SyncDashboardView.xaml"));
            List<XElement> photoActions = [.. document.Descendants().Where(element =>
                GetAttribute(element, "Command")?.Value == "{Binding EnablePhotoBackupCommand}")];
            List<XElement> folderActions = [.. document.Descendants().Where(element =>
                GetAttribute(element, "Command")?.Value == "{Binding AddFolderCommand}")];

            Assert.Equal(2, photoActions.Count);
            Assert.Equal(2, folderActions.Count);
            Assert.DoesNotContain(document.Descendants(), element =>
                element.Name.LocalName is "RadioButton" or "Switch");
        }

        [Fact]
        public void SyncStatusActionUsesAnAccessibleButton()
        {
            XDocument document = XDocument.Parse(
                RepositoryPath.ReadText($"{ViewDirectory}/SyncDashboardView.xaml"));
            XElement button = document.Descendants().Single(element =>
                element.Name.LocalName == "Button"
                && GetAttribute(element, "CommandParameter")?.Value.Contains("StatusAction", StringComparison.Ordinal)
                    == true);

            Assert.Equal("{Binding CanUseStatusAction}", GetAttribute(button, "IsVisible")?.Value);
            Assert.Equal("TextButton, TouchTarget", GetAttribute(button, "StyleClass")?.Value);
            Assert.Contains(
                "StatusActionText",
                GetAttributeContaining(button, "Description")?.Value,
                StringComparison.Ordinal);
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
