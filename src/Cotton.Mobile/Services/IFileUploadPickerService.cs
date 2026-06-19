namespace Cotton.Mobile.Services
{
    public interface IFileUploadPickerService
    {
        Task<CottonFileUploadSource?> PickFileAsync(CancellationToken cancellationToken = default);
    }
}
