namespace Cotton.Mobile.Services
{
    public static class CottonFileNavigationPlanner
    {
        public static CottonFileNavigationUpPlan CreateNavigateUpPlan(
            CottonFolderHandle? currentFolder,
            IReadOnlyList<CottonFolderHandle> navigation)
        {
            ArgumentNullException.ThrowIfNull(navigation);

            if (navigation.Count == 0)
            {
                return currentFolder is null
                    ? CottonFileNavigationUpPlan.None
                    : CottonFileNavigationUpPlan.Root();
            }

            var remainingNavigation = navigation.Take(navigation.Count - 1).ToArray();
            return CottonFileNavigationUpPlan.Folder(
                navigation[navigation.Count - 1],
                remainingNavigation);
        }
    }
}
