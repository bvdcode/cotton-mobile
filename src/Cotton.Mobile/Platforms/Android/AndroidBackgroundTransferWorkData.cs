#if ANDROID
using AndroidX.Work;

namespace Cotton.Mobile.Services
{
    internal static class AndroidBackgroundTransferWorkData
    {
        private const string InstanceUriKey = "cotton.instance_uri";
        private const string TransferIdKey = "cotton.transfer_id";
        private const string DisplayNameKey = "cotton.display_name";
        private const string WorkKindKey = "cotton.work_kind";

        public static Data Create(CottonAndroidBackgroundTransferRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            return new Data.Builder()
                .PutString(InstanceUriKey, request.InstanceUri.AbsoluteUri)
                .PutString(TransferIdKey, request.TransferId.ToString("D"))
                .PutString(DisplayNameKey, request.DisplayName)
                .PutInt(WorkKindKey, (int)request.WorkKind)
                .Build()
                ?? throw new InvalidOperationException("Android WorkManager data builder returned no data.");
        }

        public static bool TryRead(
            Data? data,
            out Uri? instanceUri,
            out Guid transferId,
            out string displayName,
            out CottonAndroidTransferWorkKind workKind)
        {
            instanceUri = null;
            transferId = Guid.Empty;
            displayName = string.Empty;
            workKind = CottonAndroidTransferWorkKind.ManualUpload;

            string? instanceValue = data?.GetString(InstanceUriKey);
            string? transferValue = data?.GetString(TransferIdKey);
            string? displayNameValue = data?.GetString(DisplayNameKey);
            int workKindValue = data?.GetInt(WorkKindKey, (int)CottonAndroidTransferWorkKind.ManualUpload)
                ?? (int)CottonAndroidTransferWorkKind.ManualUpload;

            if (!Uri.TryCreate(instanceValue, UriKind.Absolute, out Uri? parsedInstanceUri)
                || !Guid.TryParse(transferValue, out Guid parsedTransferId)
                || parsedTransferId == Guid.Empty
                || string.IsNullOrWhiteSpace(displayNameValue)
                || !Enum.IsDefined(typeof(CottonAndroidTransferWorkKind), workKindValue))
            {
                return false;
            }

            instanceUri = parsedInstanceUri;
            transferId = parsedTransferId;
            displayName = displayNameValue.Trim();
            workKind = (CottonAndroidTransferWorkKind)workKindValue;
            return true;
        }
    }
}
#endif
