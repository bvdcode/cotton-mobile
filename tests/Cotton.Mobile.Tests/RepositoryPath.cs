namespace Cotton.Mobile.Tests
{
    internal static class RepositoryPath
    {
        public static string ReadText(string relativePath)
        {
            return File.ReadAllText(Path.Combine(FindRoot(), relativePath));
        }

        public static IReadOnlyList<string> EnumerateFiles(string relativePath, string searchPattern)
        {
            string root = FindRoot();
            string directory = Path.Combine(root, relativePath);

            return Directory
                .EnumerateFiles(directory, searchPattern, SearchOption.AllDirectories)
                .Select(path => Path.GetRelativePath(root, path))
                .Order(StringComparer.Ordinal)
                .ToList();
        }

        private static string FindRoot()
        {
            DirectoryInfo? current = new(AppContext.BaseDirectory);
            while (current is not null)
            {
                if (File.Exists(Path.Combine(current.FullName, "README.md"))
                    && Directory.Exists(Path.Combine(current.FullName, "src")))
                {
                    return current.FullName;
                }

                current = current.Parent;
            }

            throw new InvalidOperationException("Repository root was not found.");
        }
    }
}
