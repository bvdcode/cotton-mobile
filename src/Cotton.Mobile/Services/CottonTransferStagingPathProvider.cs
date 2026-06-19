namespace Cotton.Mobile.Services
{
    public class CottonTransferStagingPathProvider : ICottonTransferStagingPathProvider
    {
        public string CreateTransferStagingDirectory(Uri instanceUri)
        {
            return CottonMobileStoragePaths.CreateTransferStagingDirectory(instanceUri);
        }
    }
}
