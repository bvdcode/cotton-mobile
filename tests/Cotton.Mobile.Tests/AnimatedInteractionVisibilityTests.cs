using Xunit;

namespace Cotton.Mobile.Tests
{
    public class AnimatedInteractionVisibilityTests
    {
        [Theory]
        [InlineData("src/Cotton.Mobile/Controls/SelectionBarView.cs", "InputTransparent = !IsVisible || !IsBarVisible;")]
        [InlineData("src/Cotton.Mobile/Controls/FileBrowserNavigationBarView.cs", "InputTransparent = !IsVisible || !IsNavigationVisible;")]
        [InlineData("src/Cotton.Mobile/Controls/NoticePanelView.cs", "InputTransparent = !IsVisible || !IsPanelVisible;")]
        [InlineData("src/Cotton.Mobile/Controls/AttentionStatusView.cs", "InputTransparent = !IsVisible || !IsStatusVisible;")]
        [InlineData("src/Cotton.Mobile/Controls/FileStatusActionView.cs", "InputTransparent = !IsVisible || !IsStatusVisible;")]
        public void Animated_interactive_containers_disable_input_while_hidden(
            string relativePath,
            string expectedGuard)
        {
            string content = RepositoryPath.ReadText(relativePath);

            Assert.Contains(expectedGuard, content);
        }

        [Theory]
        [InlineData("src/Cotton.Mobile/Controls/ActionClusterView.cs", "actionButton.InputTransparent = !isActionVisible;")]
        [InlineData("src/Cotton.Mobile/Controls/ScreenHeaderView.cs", "element.InputTransparent = !isElementVisible;")]
        [InlineData("src/Cotton.Mobile/Controls/NoticePanelView.cs", "element.InputTransparent = !isElementVisible;")]
        [InlineData("src/Cotton.Mobile/Controls/AttentionStatusView.cs", "_actionButton.InputTransparent = !IsActionVisible;")]
        [InlineData("src/Cotton.Mobile/Controls/FileEntryActionButtonView.cs", "_actionButton.InputTransparent = !IsActionVisible;")]
        public void Animated_child_actions_disable_input_before_fade_out(
            string relativePath,
            string expectedGuard)
        {
            string content = RepositoryPath.ReadText(relativePath);

            Assert.Contains(expectedGuard, content);
        }
    }
}
