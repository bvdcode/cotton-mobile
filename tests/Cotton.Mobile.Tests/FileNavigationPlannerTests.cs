using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class FileNavigationPlannerTests
    {
        [Fact]
        public void CreateNavigateUpPlan_WhenAlreadyAtRoot_DoesNotNavigate()
        {
            CottonFileNavigationUpPlan plan =
                CottonFileNavigationPlanner.CreateNavigateUpPlan(null, Array.Empty<CottonFolderHandle>());

            Assert.False(plan.CanNavigate);
            Assert.False(plan.IsRootTarget);
            Assert.Null(plan.TargetFolder);
            Assert.Empty(plan.NavigationAfterNavigate);
        }

        [Fact]
        public void CreateNavigateUpPlan_ForRootChildFolder_TargetsRoot()
        {
            CottonFolderHandle current = CreateFolder("Mobile smoke folder");

            CottonFileNavigationUpPlan plan =
                CottonFileNavigationPlanner.CreateNavigateUpPlan(current, Array.Empty<CottonFolderHandle>());

            Assert.True(plan.CanNavigate);
            Assert.True(plan.IsRootTarget);
            Assert.Null(plan.TargetFolder);
            Assert.Empty(plan.NavigationAfterNavigate);
        }

        [Fact]
        public void CreateNavigateUpPlan_ForNestedFolder_TargetsParentAndPopsNavigation()
        {
            CottonFolderHandle parent = CreateFolder("Parent");
            CottonFolderHandle current = CreateFolder("Child");

            CottonFileNavigationUpPlan plan =
                CottonFileNavigationPlanner.CreateNavigateUpPlan(current, new[] { parent });

            Assert.True(plan.CanNavigate);
            Assert.False(plan.IsRootTarget);
            Assert.Same(parent, plan.TargetFolder);
            Assert.Empty(plan.NavigationAfterNavigate);
        }

        [Fact]
        public void CreateNavigateUpPlan_ForDeepFolder_PreservesRemainingAncestors()
        {
            CottonFolderHandle ancestor = CreateFolder("Ancestor");
            CottonFolderHandle parent = CreateFolder("Parent");
            CottonFolderHandle current = CreateFolder("Child");

            CottonFileNavigationUpPlan plan =
                CottonFileNavigationPlanner.CreateNavigateUpPlan(current, new[] { ancestor, parent });

            Assert.True(plan.CanNavigate);
            Assert.False(plan.IsRootTarget);
            Assert.Same(parent, plan.TargetFolder);
            CottonFolderHandle remaining = Assert.Single(plan.NavigationAfterNavigate);
            Assert.Same(ancestor, remaining);
        }

        private static CottonFolderHandle CreateFolder(string name)
        {
            return new CottonFolderHandle(Guid.NewGuid(), name);
        }
    }
}
