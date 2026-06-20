using System.Security.Cryptography;
using System.Text;

namespace Cotton.Mobile.Services
{
    public class CottonAndroidBackgroundSyncScheduleIdentity
    {
        private const string UniqueWorkPrefix = "cotton-sync";

        private CottonAndroidBackgroundSyncScheduleIdentity(string uniqueWorkName, string syncTag)
        {
            if (string.IsNullOrWhiteSpace(uniqueWorkName))
            {
                throw new ArgumentException("Unique sync work name is required.", nameof(uniqueWorkName));
            }

            if (string.IsNullOrWhiteSpace(syncTag))
            {
                throw new ArgumentException("Sync work tag is required.", nameof(syncTag));
            }

            UniqueWorkName = uniqueWorkName.Trim();
            SyncTag = syncTag.Trim();
        }

        public string UniqueWorkName { get; }

        public string SyncTag { get; }

        public static CottonAndroidBackgroundSyncScheduleIdentity Create(Uri instanceUri)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            if (!instanceUri.IsAbsoluteUri)
            {
                throw new ArgumentException("Instance URI must be absolute.", nameof(instanceUri));
            }

            string instanceHash = CreateInstanceHash(instanceUri);
            string workName = $"{UniqueWorkPrefix}-{instanceHash}";
            return new CottonAndroidBackgroundSyncScheduleIdentity(workName, workName);
        }

        private static string CreateInstanceHash(Uri instanceUri)
        {
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(instanceUri.AbsoluteUri));
            return Convert.ToHexString(hash, 0, 8).ToLowerInvariant();
        }
    }
}
