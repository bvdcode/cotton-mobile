using Microsoft.Maui.ApplicationModel;

namespace Cotton.Mobile.Services
{
    public class FeedbackService : IFeedbackService
    {
        private const string FeedbackSubject = "Cotton Cloud mobile feedback";

        private readonly CottonMobileOptions _options;
        private readonly ICottonMobileApplicationMetadata _metadata;
        private readonly ILauncher _launcher;

        public FeedbackService(
            CottonMobileOptions options,
            ICottonMobileApplicationMetadata metadata,
            ILauncher launcher)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(metadata);
            ArgumentNullException.ThrowIfNull(launcher);

            _options = options;
            _metadata = metadata;
            _launcher = launcher;
        }

        public Task<bool> OpenFeedbackAsync(
            string? instanceUrl,
            string? profileName,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string body = CreateFeedbackBody(instanceUrl, profileName);
            var uri = new Uri(
                $"mailto:{_options.SupportEmail}?subject={Uri.EscapeDataString(FeedbackSubject)}&body={Uri.EscapeDataString(body)}");
            return _launcher.OpenAsync(uri);
        }

        private string CreateFeedbackBody(string? instanceUrl, string? profileName)
        {
            return string.Join(
                Environment.NewLine,
                "Please describe what happened:",
                string.Empty,
                string.Empty,
                "---",
                $"App: {_metadata.ApplicationName} {_metadata.ApplicationVersion}",
                $"Device: {_metadata.DeviceName}",
                $"OS: {_metadata.OperatingSystem}",
                $"Instance: {CreateValue(instanceUrl)}",
                $"Account: {CreateValue(profileName)}");
        }

        private static string CreateValue(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? "Not available" : value.Trim();
        }
    }
}
