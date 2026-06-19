namespace Cotton.Mobile.Services
{
    public interface ICaptureDestinationPickerPageService
    {
        Task OpenAsync(Uri instanceUri, CancellationToken cancellationToken = default);
    }
}
