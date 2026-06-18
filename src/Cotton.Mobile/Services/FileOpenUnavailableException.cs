namespace Cotton.Mobile.Services
{
    public class FileOpenUnavailableException : InvalidOperationException
    {
        public FileOpenUnavailableException(string message)
            : base(message)
        {
        }
    }
}
