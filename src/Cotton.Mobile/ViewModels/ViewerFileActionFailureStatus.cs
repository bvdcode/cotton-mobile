namespace Cotton.Mobile.ViewModels
{
    using Cotton.Mobile.Services;

    public static class ViewerFileActionFailureStatus
    {
        private const string MissingFileStatus = "File no longer available.";
        private const string OpenUnavailableStatus = "No app can open this file.";

        public static string Create(Exception exception, string fallbackStatus)
        {
            ArgumentNullException.ThrowIfNull(exception);
            ArgumentException.ThrowIfNullOrWhiteSpace(fallbackStatus);

            return exception switch
            {
                FileNotFoundException => MissingFileStatus,
                FileOpenUnavailableException => OpenUnavailableStatus,
                _ => fallbackStatus,
            };
        }
    }
}
