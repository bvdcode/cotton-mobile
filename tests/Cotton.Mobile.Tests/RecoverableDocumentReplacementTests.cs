using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class RecoverableDocumentReplacementTests
    {
        [Fact]
        public void Replace_promotes_verified_replacement_before_removing_backup()
        {
            FakeDocumentMutationStore store = CreateStore();
            CottonRecoverableDocumentReplacement<string> replacement = new CottonRecoverableDocumentReplacement<string>(store);

            string promoted = replacement.Replace("temporary", "current", "report.pdf", "report.backup");

            Assert.Equal("temporary", promoted);
            Assert.Equal("report.pdf", store.GetName("temporary"));
            Assert.False(store.Contains("current"));
            Assert.Equal(
                [
                    "rename:current:report.backup",
                    "rename:temporary:report.pdf",
                    "delete:current",
                ],
                store.Events);
        }

        [Fact]
        public void Replace_restores_current_document_when_promotion_fails()
        {
            FakeDocumentMutationStore store = CreateStore();
            store.FailingRenameCalls.Add(2);
            CottonRecoverableDocumentReplacement<string> replacement = new CottonRecoverableDocumentReplacement<string>(store);

            Assert.Throws<IOException>(
                () => replacement.Replace("temporary", "current", "report.pdf", "report.backup"));

            Assert.Equal("report.pdf", store.GetName("current"));
            Assert.False(store.Contains("temporary"));
            Assert.Equal(
                [
                    "rename:current:report.backup",
                    "rename:temporary:report.pdf",
                    "rename:current:report.pdf",
                    "delete:temporary",
                ],
                store.Events);
        }

        [Fact]
        public void Replace_preserves_backup_when_promotion_and_rollback_fail()
        {
            FakeDocumentMutationStore store = CreateStore();
            store.FailingRenameCalls.Add(2);
            store.FailingRenameCalls.Add(3);
            CottonRecoverableDocumentReplacement<string> replacement = new CottonRecoverableDocumentReplacement<string>(store);

            AggregateException exception = Assert.Throws<AggregateException>(
                () => replacement.Replace("temporary", "current", "report.pdf", "report.backup"));

            Assert.Equal(2, exception.InnerExceptions.Count);
            Assert.Equal("report.backup", store.GetName("current"));
            Assert.Equal("report.temporary", store.GetName("temporary"));
            Assert.DoesNotContain("delete:temporary", store.Events);
        }

        [Fact]
        public void Replace_reports_temporary_cleanup_failure_after_successful_rollback()
        {
            FakeDocumentMutationStore store = CreateStore();
            store.FailingRenameCalls.Add(2);
            store.FailingDeleteCalls.Add(1);
            CottonRecoverableDocumentReplacement<string> replacement = new CottonRecoverableDocumentReplacement<string>(store);

            AggregateException exception = Assert.Throws<AggregateException>(
                () => replacement.Replace("temporary", "current", "report.pdf", "report.backup"));

            Assert.Equal(2, exception.InnerExceptions.Count);
            Assert.Equal("report.pdf", store.GetName("current"));
            Assert.True(store.Contains("temporary"));
        }

        [Fact]
        public void Replace_keeps_promoted_document_when_backup_cleanup_fails()
        {
            FakeDocumentMutationStore store = CreateStore();
            store.FailingDeleteCalls.Add(1);
            CottonRecoverableDocumentReplacement<string> replacement = new CottonRecoverableDocumentReplacement<string>(store);

            Assert.Throws<IOException>(
                () => replacement.Replace("temporary", "current", "report.pdf", "report.backup"));

            Assert.Equal("report.pdf", store.GetName("temporary"));
            Assert.Equal("report.backup", store.GetName("current"));
        }

        private static FakeDocumentMutationStore CreateStore()
        {
            FakeDocumentMutationStore store = new FakeDocumentMutationStore();
            store.Add("current", "report.pdf");
            store.Add("temporary", "report.temporary");
            return store;
        }
    }
}
