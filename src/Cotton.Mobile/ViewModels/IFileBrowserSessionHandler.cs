namespace Cotton.Mobile.ViewModels
{
    public interface IFileBrowserSessionHandler
    {
        Task HandleFileBrowserSessionExpiredAsync(Uri? instanceUri);
    }
}
