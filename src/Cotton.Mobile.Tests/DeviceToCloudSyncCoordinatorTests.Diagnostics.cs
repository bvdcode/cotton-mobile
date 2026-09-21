using Cotton.Mobile.Services;
using Microsoft.Extensions.Logging;
using static Cotton.Mobile.Tests.DeviceToCloudSyncCoordinatorTestData;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public partial class DeviceToCloudSyncCoordinatorTests
    {
        [Fact]
        public async Task DailyConflictScansPreserveUploadAndFailureHistoryWithBoundedSamples()
        {
            const int conflictCount = 1200;
            const int dailyPasses = 96;
            CottonSyncRootSnapshot[] roots =
            [
                CreateRoot(SyncRootId, FolderId, "Photos"),
                CreateRoot(SecondSyncRootId, SecondFolderId, "Videos"),
            ];
            await _rootStore.SaveAsync(InstanceUri, roots, TestContext.Current.CancellationToken);
            foreach (CottonSyncRootSnapshot root in roots)
            {
                CottonDeviceToCloudLocalItemSnapshot[] local = [.. Enumerable.Range(0, conflictCount)
                    .Select(index => CreateLocalFile($"{index}.jpg", $"{index}.jpg", $"document:{index}"))];
                CottonFileBrowserEntry[] remote = [.. local.Select(item => CottonFileBrowserEntryFactory.CreateFile(
                    Guid.NewGuid(), item.DisplayName, UpdatedAt, 42, "image/jpeg", null,
                    "\"etag\"", contentHash: TestContentHashes.Second))];
                _localTreeReader.SetContent(root.Id, CreateLocalContent(local));
                _remoteFolderContentSource.SetContent(root.CloudFolder.FolderId, CreateContent(root, remote));
            }

            _journal.Write(LogLevel.Information, "upload", new EventId(2131), "First upload completed.", null);
            _journal.Write(LogLevel.Warning, "worker", new EventId(2205), "First worker stopped.", null);
            for (int pass = 0; pass < dailyPasses; pass++)
            {
                CottonDeviceToCloudSyncRunSummary result = await _coordinator.RunAsync(
                    InstanceUri, TestContext.Current.CancellationToken);
                Assert.Equal(0, result.UploadedCount);
                if (pass % 4 == 0)
                {
                    _journal.Write(LogLevel.Warning, "network", new EventId(1101), new string('x', 16000), null);
                }
            }

            IReadOnlyList<string> records = _journal.ReadAll();
            Assert.Contains(records, record => record.Contains("First upload completed.", StringComparison.Ordinal));
            Assert.Contains(records, record => record.Contains("First worker stopped.", StringComparison.Ordinal));
            Assert.Equal(roots.Length, records.Count(record => record.Contains("\t2127\t", StringComparison.Ordinal)));
            Assert.Equal(roots.Length * 3, records.Count(record => record.Contains("\t2126\t", StringComparison.Ordinal)));
            Assert.Equal((dailyPasses - 1) * roots.Length, records.Count(record => record.Contains("\t2142\t", StringComparison.Ordinal)));
            Assert.Equal(roots.Length, _remoteFolderContentSource.RequestedFolderIds.Count);
            Assert.Contains(records, record => record.Contains(TestContentHashes.First, StringComparison.Ordinal)
                && record.Contains(TestContentHashes.Second, StringComparison.Ordinal));
            Assert.Empty(_fileOperator.UploadedItems);
        }
    }
}
