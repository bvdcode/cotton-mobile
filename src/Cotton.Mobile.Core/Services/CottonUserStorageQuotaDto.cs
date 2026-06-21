namespace Cotton.Mobile.Services
{
    public class CottonUserStorageQuotaDto
    {
        public long UsedBytes { get; set; }

        public long? QuotaBytes { get; set; }

        public long? AvailableBytes { get; set; }
    }
}
