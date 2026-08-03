using Xunit;

namespace Cotton.Mobile.Tests
{
    public class AnimatedInteractionVisibilityTests
    {
        [Fact]
        public void Animated_action_cluster_disables_input_before_fade_out()
        {
            string content = RepositoryPath.ReadText(
                "src/Cotton.Mobile/Controls/ActionClusterView.cs");

            Assert.Contains("actionButton.InputTransparent = !isActionVisible;", content);
        }
    }
}
