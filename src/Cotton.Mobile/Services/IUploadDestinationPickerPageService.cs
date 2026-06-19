namespace Cotton.Mobile.Services
{
    public interface IUploadDestinationPickerPageService
    {
        Task<CottonUploadDestinationSnapshot?> PickAsync(
            Uri instanceUri,
            CancellationToken cancellationToken = default);
    }
}
