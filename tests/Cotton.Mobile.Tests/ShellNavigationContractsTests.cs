using Xunit;

namespace Cotton.Mobile.Tests
{
    public class ShellNavigationContractsTests
    {
        [Fact]
        public void Shell_navigation_pushes_are_serialized_and_duplicate_guarded()
        {
            string source = RepositoryPath.ReadText("src/Cotton.Mobile/Services/CottonShellNavigation.cs");

            Assert.Contains("private static readonly SemaphoreSlim PushGate = new(1, 1);", source, StringComparison.Ordinal);
            Assert.Contains("PushGate.WaitAsync(cancellationToken)", source, StringComparison.Ordinal);
            Assert.Contains("Shell.Current?.Navigation", source, StringComparison.Ordinal);
            Assert.Contains("NavigationStack.LastOrDefault()", source, StringComparison.Ordinal);
            Assert.Contains("currentPage.GetType() == page.GetType()", source, StringComparison.Ordinal);
            Assert.Contains("return false;", source, StringComparison.Ordinal);
            Assert.Contains("await navigation.PushAsync(page);", source, StringComparison.Ordinal);
            Assert.Contains("PushGate.Release();", source, StringComparison.Ordinal);
            Assert.Contains("MainThread.IsMainThread", source, StringComparison.Ordinal);
        }

        [Fact]
        public void Screen_services_use_shared_shell_navigation_push_gate()
        {
            IReadOnlyList<string> sourcePaths = RepositoryPath
                .EnumerateFiles("src/Cotton.Mobile/Services", "*.cs")
                .Where(path => !path.EndsWith("CottonShellNavigation.cs", StringComparison.Ordinal))
                .ToList();
            List<string> directNavigationPushes = [];
            int guardedPushCount = 0;

            foreach (string sourcePath in sourcePaths)
            {
                string source = RepositoryPath.ReadText(sourcePath);
                if (source.Contains("CottonShellNavigation.PushAsync(page, cancellationToken)", StringComparison.Ordinal))
                {
                    guardedPushCount++;
                }

                if (source.Contains("Shell.Current.Navigation.PushAsync(page)", StringComparison.Ordinal)
                    || source.Contains("await navigation.PushAsync(page);", StringComparison.Ordinal))
                {
                    directNavigationPushes.Add(sourcePath);
                }
            }

            Assert.True(guardedPushCount >= 10);
            Assert.Empty(directNavigationPushes);
        }

        [Fact]
        public void Destination_picker_completes_when_duplicate_push_is_skipped()
        {
            string source = RepositoryPath.ReadText(
                "src/Cotton.Mobile/Services/UploadDestinationPickerPageService.cs");

            Assert.Contains("bool pushed = await CottonShellNavigation.PushAsync(page, cancellationToken);", source, StringComparison.Ordinal);
            Assert.Contains("if (!pushed)", source, StringComparison.Ordinal);
            Assert.Contains("completion.TrySetResult(null);", source, StringComparison.Ordinal);
        }
    }
}
