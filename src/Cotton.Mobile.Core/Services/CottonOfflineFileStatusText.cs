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

        public static string CreateRemovedStatus(string fileName)
        {
            return $"{NormalizeFileName(fileName)} removed from this device.";
        }

        public static string CreateNotOnDeviceStatus(string fileName)
        {
            return $"{NormalizeFileName(fileName)} is not on this device.";
        }

        public static string OfflineUnavailableStatus { get; } = "Offline. Keep offline needs internet.";

        public static string CancelledStatus { get; } = "Keep offline cancelled.";

        public static string FailedStatus { get; } = "Keep offline failed.";

        public static string RemoveCancelledStatus { get; } = "Remove offline cancelled.";

        public static string RemoveFailedStatus { get; } = "Remove offline failed.";

        private static string NormalizeFileName(string fileName)
        {
            return string.IsNullOrWhiteSpace(fileName) ? DefaultFileName : fileName.Trim();
        }
    }
}
