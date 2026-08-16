using Cotton.Mobile.Services;
using static Cotton.Mobile.Tests.UploadOnlySyncPlanExecutorTestData;

namespace Cotton.Mobile.Tests
{
    internal class ExecutionHarness
    {
        public ExecutionHarness(
            CottonUploadOriginalRetention retention,
            IReadOnlyList<CottonUploadReceiptSnapshot>? initialReceipts = null)
        {
            Events = [];
            Root = CreateRoot(retention);
            ReceiptStore = new UploadOnlyReceiptStore(Events, initialReceipts ?? []);
            FileOperator = new UploadOnlySyncFileOperator(Events);
            LocalFileOperator = new UploadOnlyLocalFileOperator(Events);
            Executor = new CottonUploadOnlySyncPlanExecutor(
                FileOperator,
                LocalFileOperator,
                ReceiptStore,
                new CottonSyncProgressHub(),
                new FixedTimeProvider(RecordedAt));
        }

        public List<string> Events { get; }

        public CottonSyncRootSnapshot Root { get; }

        public UploadOnlyReceiptStore ReceiptStore { get; }

        public UploadOnlySyncFileOperator FileOperator { get; }

        public UploadOnlyLocalFileOperator LocalFileOperator { get; }

        public CottonUploadOnlySyncPlanExecutor Executor { get; }
    }
}
