using System.Globalization;

namespace Cotton.Mobile.Services
{
    public static class CottonScannedDocumentUploadContract
    {
        public const string PdfContentType = "application/pdf";
        public const string SourceMetadataValue = "scanned-document";
        public const string ScannerModeMetadataValue = "full";
        public const string ResultFormatMetadataValue = "pdf";

        public static string CreatePdfFileName(DateTimeOffset capturedAt)
        {
            return string.Create(
                CultureInfo.InvariantCulture,
                $"Scanned document {capturedAt:yyyy-MM-dd HH-mm-ss}.pdf");
        }

        public static IReadOnlyDictionary<string, string> CreateMetadata(int? pageCount)
        {
            if (pageCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pageCount), "Document page count cannot be negative.");
            }

            var metadata = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [CottonFileUploadMetadataKeys.Source] = SourceMetadataValue,
                [CottonFileUploadMetadataKeys.DocumentScannerMode] = ScannerModeMetadataValue,
                [CottonFileUploadMetadataKeys.DocumentScannerResultFormat] = ResultFormatMetadataValue,
            };

            if (pageCount > 0)
            {
                metadata[CottonFileUploadMetadataKeys.DocumentScannerPageCount] =
                    pageCount.Value.ToString(CultureInfo.InvariantCulture);
            }

            return metadata;
        }
    }
}
