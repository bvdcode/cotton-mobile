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

        [Fact]
        public void CreateNavigationAfterOpenFolder_FromRoot_DoesNotPushRoot()
        {
            CottonFolderHandle root = CreateFolder("Files");

            IReadOnlyList<CottonFolderHandle> navigation =
                CottonFileNavigationPlanner.CreateNavigationAfterOpenFolder(
                    root,
                    Array.Empty<CottonFolderHandle>(),
                    isCurrentRoot: true);

            Assert.Empty(navigation);
        }

        [Fact]
        public void CreateNavigationAfterOpenFolder_FromFolder_PushesCurrentFolder()
        {
            CottonFolderHandle parent = CreateFolder("Parent");

            IReadOnlyList<CottonFolderHandle> navigation =
                CottonFileNavigationPlanner.CreateNavigationAfterOpenFolder(
                    parent,
                    Array.Empty<CottonFolderHandle>(),
                    isCurrentRoot: false);

            CottonFolderHandle pushed = Assert.Single(navigation);
            Assert.Same(parent, pushed);
        }

        [Fact]
        public void CreatePathSegments_PrependsRootWithoutStoringRootInNavigation()
        {
            CottonFolderHandle parent = CreateFolder("Parent");

            IReadOnlyList<string> segments =
                CottonFileNavigationPlanner.CreatePathSegments(
                    "Files",
                    new[] { parent },
                    "Child");

            Assert.Equal(["Files", "Parent", "Child"], segments);
        }

        private static CottonFolderHandle CreateFolder(string name)
        {
            return new CottonFolderHandle(Guid.NewGuid(), name);
        }
    }
}
