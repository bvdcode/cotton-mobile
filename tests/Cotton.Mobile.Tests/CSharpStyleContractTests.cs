using System.Text.RegularExpressions;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class CSharpStyleContractTests
    {
        [Fact]
        public void Production_csharp_sources_do_not_use_sealed_modifier()
        {
            IReadOnlyList<string> sourcePaths = RepositoryPath
                .EnumerateFiles("src", "*.cs")
                .Where(IsSourceFilePath)
                .ToList();
            List<string> matches = [];
            Regex sealedModifierPattern = new(@"\bsealed\s+(class|record)\b", RegexOptions.Compiled);

            foreach (string sourcePath in sourcePaths)
            {
                string content = RepositoryPath.ReadText(sourcePath);
                if (sealedModifierPattern.IsMatch(content))
                {
                    matches.Add(sourcePath);
                }
            }

            Assert.Empty(matches);
        }

        private static bool IsSourceFilePath(string sourcePath)
        {
            return !sourcePath.Contains("/bin/", StringComparison.Ordinal)
                && !sourcePath.Contains("/obj/", StringComparison.Ordinal);
        }
    }
}
