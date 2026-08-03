using Xunit;

namespace Cotton.Mobile.Tests
{
    public class AnimatedInteractionVisibilityTests
    {
        [Theory]
        [InlineData("src/Cotton.Mobile/Controls/ActionClusterView.cs", "actionButton.InputTransparent = !isActionVisible;")]
        [InlineData("src/Cotton.Mobile/Controls/ScreenHeaderView.cs", "element.InputTransparent = !isElementVisible;")]
        public void Animated_actions_disable_input_before_fade_out(
            string relativePath,
            string expectedGuard)
        {
            string content = RepositoryPath.ReadText(relativePath);

            Assert.Contains(expectedGuard, content);
        }
    }
}
