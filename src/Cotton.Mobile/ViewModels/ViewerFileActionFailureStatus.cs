namespace Cotton.Mobile.ViewModels
{
    public static class ViewerFileActionFailureStatus
    {
        private const string MissingFileStatus = "File no longer available.";

        public static string Create(Exception exception, string fallbackStatus)
        {
            ArgumentNullException.ThrowIfNull(exception);
            ArgumentException.ThrowIfNullOrWhiteSpace(fallbackStatus);

            return exception is FileNotFoundException ? MissingFileStatus : fallbackStatus;
        }
    }
}
