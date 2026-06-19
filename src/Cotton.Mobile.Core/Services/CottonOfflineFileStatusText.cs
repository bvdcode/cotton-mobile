namespace Cotton.Mobile.Services
{
    public static class CottonOfflineFileStatusText
    {
        private const string DefaultFileName = "File";

        public static string CreateStartingStatus(string fileName)
        {
            return $"Keeping {NormalizeFileName(fileName)} offline...";
        }

        public static string CreateAvailableStatus(string fileName)
        {
            return $"{NormalizeFileName(fileName)} is available offline.";
        }

        public static string OfflineUnavailableStatus { get; } = "Offline. Keep offline needs internet.";

        public static string CancelledStatus { get; } = "Keep offline cancelled.";

        public static string FailedStatus { get; } = "Keep offline failed.";

        private static string NormalizeFileName(string fileName)
        {
            return string.IsNullOrWhiteSpace(fileName) ? DefaultFileName : fileName.Trim();
        }
    }
}
