namespace Cotton.Mobile.Services
{
    public interface ICottonTransferStagingPathProvider
    {
        string CreateTransferStagingDirectory(Uri instanceUri);
    }
}
