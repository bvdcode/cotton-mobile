using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class FileDownloadCachePrunePlannerTests
    {
        private static readonly DateTime Older = new(2026, 6, 19, 8, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime Newer = new(2026, 6, 19, 9, 0, 0, DateTimeKind.Utc);

        [Fact]
        public void Select_files_to_delete_keeps_cache_under_budget_by_oldest_unprotected_files()
        {
            string oldFile = CreatePath("cache", "old.bin");
            string newFile = CreatePath("cache", "new.bin");

            IReadOnlyList<string> deletePaths = CottonFileDownloadCachePrunePlanner.SelectFilesToDelete(
                [
                    new CottonFileDownloadCacheEntry(oldFile, 80, Older, requiresSensitiveEviction: false),
                    new CottonFileDownloadCacheEntry(newFile, 80, Newer, requiresSensitiveEviction: false),
                ],
                maxCacheBytes: 100,
                protectedPath: null,
                protectedDirectories: []);

            string deletePath = Assert.Single(deletePaths);
            Assert.Equal(oldFile, deletePath);
        }

        [Fact]
        public void Select_files_to_delete_preserves_exact_protected_path()
        {
            string protectedFile = CreatePath("cache", "fresh.bin");
            string oldFile = CreatePath("cache", "old.bin");

            IReadOnlyList<string> deletePaths = CottonFileDownloadCachePrunePlanner.SelectFilesToDelete(
                [
                    new CottonFileDownloadCacheEntry(protectedFile, 80, Older, requiresSensitiveEviction: false),
                    new CottonFileDownloadCacheEntry(oldFile, 80, Newer, requiresSensitiveEviction: false),
                ],
                maxCacheBytes: 100,
                protectedPath: protectedFile,
                protectedDirectories: []);

            string deletePath = Assert.Single(deletePaths);
            Assert.Equal(oldFile, deletePath);
        }

        [Fact]
        public void Select_files_to_delete_preserves_pinned_download_directory()
        {
            string pinnedDirectory = CreatePath("downloads", "instance", "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            string pinnedFile = Path.Combine(pinnedDirectory, "report.pdf");
            string evictableFile = CreatePath(
                "downloads",
                "instance",
                "bbbbbbbb-cccc-dddd-eeee-ffffffffffff",
                "open-cache.pdf");

            IReadOnlyList<string> deletePaths = CottonFileDownloadCachePrunePlanner.SelectFilesToDelete(
                [
                    new CottonFileDownloadCacheEntry(pinnedFile, 80, Older, requiresSensitiveEviction: false),
                    new CottonFileDownloadCacheEntry(evictableFile, 80, Newer, requiresSensitiveEviction: false),
                ],
                maxCacheBytes: 100,
                protectedPath: null,
                protectedDirectories: [pinnedDirectory]);

            string deletePath = Assert.Single(deletePaths);
            Assert.Equal(evictableFile, deletePath);
        }

        [Fact]
        public void Select_files_to_delete_does_not_protect_sibling_directory_prefixes()
        {
            string pinnedDirectory = CreatePath("downloads", "instance", "file");
            string siblingFile = CreatePath("downloads", "instance", "file-sibling", "cache.bin");
            string newerFile = CreatePath("downloads", "instance", "other", "cache.bin");

            IReadOnlyList<string> deletePaths = CottonFileDownloadCachePrunePlanner.SelectFilesToDelete(
                [
                    new CottonFileDownloadCacheEntry(siblingFile, 80, Older, requiresSensitiveEviction: false),
                    new CottonFileDownloadCacheEntry(newerFile, 80, Newer, requiresSensitiveEviction: false),
                ],
                maxCacheBytes: 100,
                protectedPath: null,
                protectedDirectories: [pinnedDirectory]);

            string deletePath = Assert.Single(deletePaths);
            Assert.Equal(siblingFile, deletePath);
        }

        [Fact]
        public void Select_files_to_delete_keeps_unavoidable_pinned_over_budget()
        {
            string pinnedDirectory = CreatePath("downloads", "instance", "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            string pinnedFile = Path.Combine(pinnedDirectory, "report.pdf");

            IReadOnlyList<string> deletePaths = CottonFileDownloadCachePrunePlanner.SelectFilesToDelete(
                [new CottonFileDownloadCacheEntry(pinnedFile, 200, Older, requiresSensitiveEviction: false)],
                maxCacheBytes: 100,
                protectedPath: null,
                protectedDirectories: [pinnedDirectory]);

            Assert.Empty(deletePaths);
        }

        [Fact]
        public void Select_files_to_delete_evicts_sensitive_unpinned_files_before_budget_pressure()
        {
            string sensitiveFile = CreatePath("downloads", "instance", "secret", "service-account.pem");
            string normalFile = CreatePath("downloads", "instance", "normal", "photo.jpg");

            IReadOnlyList<string> deletePaths = CottonFileDownloadCachePrunePlanner.SelectFilesToDelete(
                [
                    new CottonFileDownloadCacheEntry(sensitiveFile, 80, Older, requiresSensitiveEviction: true),
                    new CottonFileDownloadCacheEntry(normalFile, 80, Newer, requiresSensitiveEviction: false),
                ],
                maxCacheBytes: 1000,
                protectedPath: null,
                protectedDirectories: []);

            string deletePath = Assert.Single(deletePaths);
            Assert.Equal(sensitiveFile, deletePath);
        }

        [Fact]
        public void Select_files_to_delete_preserves_protected_sensitive_path()
        {
            string sensitiveFile = CreatePath("downloads", "instance", "secret", "service-account.pem");

            IReadOnlyList<string> deletePaths = CottonFileDownloadCachePrunePlanner.SelectFilesToDelete(
                [new CottonFileDownloadCacheEntry(sensitiveFile, 80, Older, requiresSensitiveEviction: true)],
                maxCacheBytes: 1000,
                protectedPath: sensitiveFile,
                protectedDirectories: []);

            Assert.Empty(deletePaths);
        }

        [Fact]
        public void Select_files_to_delete_preserves_sensitive_pinned_directory()
        {
            string pinnedDirectory = CreatePath("downloads", "instance", "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            string sensitiveFile = Path.Combine(pinnedDirectory, "service-account.pem");

            IReadOnlyList<string> deletePaths = CottonFileDownloadCachePrunePlanner.SelectFilesToDelete(
                [new CottonFileDownloadCacheEntry(sensitiveFile, 80, Older, requiresSensitiveEviction: true)],
                maxCacheBytes: 1000,
                protectedPath: null,
                protectedDirectories: [pinnedDirectory]);

            Assert.Empty(deletePaths);
        }

        private static string CreatePath(params string[] parts)
        {
            string root = OperatingSystem.IsWindows() ? "C:\\" : "/";
            return Path.Combine([root, .. parts]);
        }
    }
}
