namespace Cotton.Mobile.Services
{
    internal class CottonStoredOfflineFilePinManifest
    {
        public int SchemaVersion { get; set; }

        public DateTime SavedAtUtc { get; set; }

        public List<CottonStoredOfflineFilePinItem>? Items { get; set; }
    }
}
