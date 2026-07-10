using Xunit;

namespace Cotton.Mobile.Tests
{
    public class SecuritySettingsPresentationTests
    {
        [Fact]
        public void Security_page_keeps_baseline_capability_state_in_its_cards()
        {
            string page = RepositoryPath.ReadText("src/Cotton.Mobile/SecuritySettingsPage.xaml");
            string viewModel = RepositoryPath.ReadText(
                "src/Cotton.Mobile/ViewModels/SecuritySettingsViewModel.cs");
            string loadMethod = ExtractMethod(
                viewModel,
                "private async Task LoadAsync()",
                "catch (Exception exception)");

            Assert.Contains("<controls:ScreenHeaderView Title=\"Security\"", page, StringComparison.Ordinal);
            Assert.DoesNotContain("SupportingText=\"{Binding SummaryText}\"", page, StringComparison.Ordinal);
            Assert.DoesNotContain("public string SummaryText", viewModel, StringComparison.Ordinal);
            Assert.DoesNotContain("\"App lock is unavailable.\"", loadMethod, StringComparison.Ordinal);
            Assert.DoesNotContain("Could not inspect device access.", loadMethod, StringComparison.Ordinal);
            Assert.DoesNotContain("Could not load account sessions.", loadMethod, StringComparison.Ordinal);
            Assert.Contains(
                "<controls:SettingsCardView>\n                <controls:SettingsToggleItemView Text=\"{Binding AppLockTitle}\"",
                page,
                StringComparison.Ordinal);
            Assert.Contains(
                "<controls:SettingsCardView IsCardVisible=\"{Binding IsDeviceUnlockActionVisible}\">\n                <controls:SettingsSummaryHeaderView Title=\"{Binding DeviceUnlockTitle}\"",
                page,
                StringComparison.Ordinal);
        }

        private static string ExtractMethod(string source, string startMarker, string endMarker)
        {
            int start = source.IndexOf(startMarker, StringComparison.Ordinal);
            int end = source.IndexOf(endMarker, start, StringComparison.Ordinal);

            Assert.True(start >= 0, $"Could not find method marker '{startMarker}'.");
            Assert.True(end > start, $"Could not find method marker '{endMarker}'.");
            return source[start..end];
        }
    }
}
