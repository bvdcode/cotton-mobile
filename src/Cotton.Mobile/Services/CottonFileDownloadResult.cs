namespace Cotton.Mobile.Services
{
    public class CottonFileDownloadResult
    {
        public CottonFileDownloadResult(string fileName, string filePath, long sizeBytes)
        {
            FileName = string.IsNullOrWhiteSpace(fileName) ? throw new ArgumentException("File name is required.", nameof(fileName)) : fileName;
            FilePath = string.IsNullOrWhiteSpace(filePath) ? throw new ArgumentException("File path is required.", nameof(filePath)) : filePath;
            SizeBytes = sizeBytes;
        }

        public string FileName { get; }

        public string FilePath { get; }

        public long SizeBytes { get; }
    }
}
