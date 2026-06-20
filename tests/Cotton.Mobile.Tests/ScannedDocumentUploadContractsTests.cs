using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class ScannedDocumentUploadContractsTests
    {
        [Fact]
        public void Scanned_document_file_name_is_pdf_and_path_safe()
        {
            var capturedAt = new DateTimeOffset(2026, 6, 20, 3, 45, 12, TimeSpan.Zero);

            string name = CottonScannedDocumentUploadContract.CreatePdfFileName(capturedAt);

            Assert.Equal("Scanned document 2026-06-20 03-45-12.pdf", name);
        }

        [Fact]
        public void Scanned_document_metadata_records_pdf_scan_details()
        {
            IReadOnlyDictionary<string, string> metadata =
                CottonScannedDocumentUploadContract.CreateMetadata(3);

            Assert.Equal(
                CottonScannedDocumentUploadContract.SourceMetadataValue,
                metadata[CottonFileUploadMetadataKeys.Source]);
            Assert.Equal(
                CottonScannedDocumentUploadContract.ScannerModeMetadataValue,
                metadata[CottonFileUploadMetadataKeys.DocumentScannerMode]);
            Assert.Equal(
                CottonScannedDocumentUploadContract.ResultFormatMetadataValue,
                metadata[CottonFileUploadMetadataKeys.DocumentScannerResultFormat]);
            Assert.Equal("3", metadata[CottonFileUploadMetadataKeys.DocumentScannerPageCount]);
        }

        [Fact]
        public void Scanned_document_metadata_omits_unknown_page_count()
        {
            IReadOnlyDictionary<string, string> metadata =
                CottonScannedDocumentUploadContract.CreateMetadata(null);

            Assert.False(metadata.ContainsKey(CottonFileUploadMetadataKeys.DocumentScannerPageCount));
        }
    }
}
