namespace Cotton.Mobile.Services
{
    public static class CottonCloudStorageQuotaDiagnosticText
    {
        public static string Create(CottonCloudStorageQuotaSnapshot quota)
        {
            ArgumentNullException.ThrowIfNull(quota);

            return $"{quota.SummaryText} · {quota.DetailText}";
        }
    }
}
