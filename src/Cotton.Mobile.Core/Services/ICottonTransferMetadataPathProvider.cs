namespace Cotton.Mobile.Services
{
    public interface ICottonTransferMetadataPathProvider
    {
        string CreateTransferMetadataDirectory(Uri instanceUri);
    }
}
