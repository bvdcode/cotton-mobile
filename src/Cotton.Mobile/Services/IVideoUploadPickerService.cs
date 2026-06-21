namespace Cotton.Mobile.Services
{
    public interface IVideoUploadPickerService
    {
        Task<CottonFileUploadSource?> PickVideoAsync(CancellationToken cancellationToken = default);

        Task<IReadOnlyList<CottonFileUploadSource>> PickVideosAsync(CancellationToken cancellationToken = default);
    }
}
