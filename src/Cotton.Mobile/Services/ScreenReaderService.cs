namespace Cotton.Mobile.Services
{
    public class ScreenReaderService : IScreenReaderService
    {
        public void Announce(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                SemanticScreenReader.Announce(message);
            }
        }
    }
}
