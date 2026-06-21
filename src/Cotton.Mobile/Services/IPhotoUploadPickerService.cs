namespace Cotton.Mobile.Services
{
    public interface IPhotoUploadPickerService
    {
        Task<CottonFileUploadSource?> PickPhotoAsync(CancellationToken cancellationToken = default);

        Task<IReadOnlyList<CottonFileUploadSource>> PickPhotosAsync(CancellationToken cancellationToken = default);
    }
}
