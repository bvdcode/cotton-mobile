using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.Services
{
    public class ScreenReaderService : IScreenReaderService
    {
        private readonly ILogger<ScreenReaderService> _logger;

        public ScreenReaderService(ILogger<ScreenReaderService> logger)
        {
            ArgumentNullException.ThrowIfNull(logger);

            _logger = logger;
        }

        public void Announce(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            try
            {
                SemanticScreenReader.Announce(message);
            }
            catch (Exception exception)
            {
                _logger.LogDebug(exception, "Failed to announce Cotton mobile screen-reader message.");
            }
        }
    }
}
