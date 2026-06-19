namespace Cotton.Mobile.Services
{
    public class CottonShareIntakePathProvider : ICottonShareIntakePathProvider
    {
        public string CreateShareIntakeDirectory()
        {
            return CottonMobileStoragePaths.CreateShareIntakeDirectory();
        }
    }
}
