using System.Security.Cryptography;
using System.Text;

namespace Cotton.Mobile.Services
{
    public sealed class CottonAndroidBackgroundTransferScheduleIdentity
    {
        private const string UniqueWorkPrefix = "cotton-transfer";

        private CottonAndroidBackgroundTransferScheduleIdentity(
            int jobId,
            string uniqueWorkName,
            string transferTag)
        {
            if (jobId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(jobId), "Android job id must be positive.");
            }

            if (string.IsNullOrWhiteSpace(uniqueWorkName))
            {
                throw new ArgumentException("Unique work name is required.", nameof(uniqueWorkName));
            }

            if (string.IsNullOrWhiteSpace(transferTag))
            {
                throw new ArgumentException("Transfer tag is required.", nameof(transferTag));
            }

            JobId = jobId;
            UniqueWorkName = uniqueWorkName.Trim();
            TransferTag = transferTag.Trim();
        }

        public int JobId { get; }

        public string UniqueWorkName { get; }

        public string TransferTag { get; }

        public static CottonAndroidBackgroundTransferScheduleIdentity Create(
            Uri instanceUri,
            Guid transferId)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            if (!instanceUri.IsAbsoluteUri)
            {
                throw new ArgumentException("Instance URI must be absolute.", nameof(instanceUri));
            }

            if (transferId == Guid.Empty)
            {
                throw new ArgumentException("Transfer id cannot be empty.", nameof(transferId));
            }

            string instanceHash = CreateInstanceHash(instanceUri);
            string transferKey = transferId.ToString("N");
            string uniqueWorkName = $"{UniqueWorkPrefix}-{instanceHash}-{transferKey}";
            string transferTag = $"{UniqueWorkPrefix}-{transferKey}";
            int jobId = CreateJobId($"{instanceUri.AbsoluteUri}|{transferKey}");

            return new CottonAndroidBackgroundTransferScheduleIdentity(
                jobId,
                uniqueWorkName,
                transferTag);
        }

        private static string CreateInstanceHash(Uri instanceUri)
        {
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(instanceUri.AbsoluteUri));
            return Convert.ToHexString(hash, 0, 8).ToLowerInvariant();
        }

        private static int CreateJobId(string value)
        {
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
            int jobId = (hash[0] << 24)
                | (hash[1] << 16)
                | (hash[2] << 8)
                | hash[3];
            jobId &= int.MaxValue;

            return jobId == 0 ? 1 : jobId;
        }
    }
}
