namespace Cotton.Mobile.Tests
{
    internal static class RepositoryPath
    {
        public static string ReadText(string relativePath)
        {
            return File.ReadAllText(Path.Combine(FindRoot(), relativePath));
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
