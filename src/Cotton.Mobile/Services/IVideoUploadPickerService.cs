namespace Cotton.Mobile.Services
{
    public interface IVideoUploadPickerService
    {
        Task<CottonFileUploadSource?> PickVideoAsync(CancellationToken cancellationToken = default);
    }
}
