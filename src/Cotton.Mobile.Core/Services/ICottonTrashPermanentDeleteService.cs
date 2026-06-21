namespace Cotton.Mobile.Services
{
    public interface ICottonTrashPermanentDeleteService
    {
        Task<CottonTrashPermanentDeleteResult> DeleteForeverAsync(
            Uri instanceUri,
            CottonFileBrowserEntry item,
            CancellationToken cancellationToken = default);
    }
}
