// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID
using Android.OS;

namespace Cotton.Mobile.Services
{
    internal static class AndroidBackgroundTransferJobExtras
    {
        private const string InstanceUriKey = "cotton.instance_uri";
        private const string TransferIdKey = "cotton.transfer_id";
        private const string DisplayNameKey = "cotton.display_name";
        private const string WorkKindKey = "cotton.work_kind";

        public static PersistableBundle Create(CottonAndroidBackgroundTransferRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            var extras = new PersistableBundle();
            extras.PutString(InstanceUriKey, request.InstanceUri.AbsoluteUri);
            extras.PutString(TransferIdKey, request.TransferId.ToString("D"));
            extras.PutString(DisplayNameKey, request.DisplayName);
            extras.PutInt(WorkKindKey, (int)request.WorkKind);
            return extras;
        }

        public static bool TryRead(
            PersistableBundle? extras,
            out Uri? instanceUri,
            out Guid transferId,
            out string displayName,
            out CottonAndroidTransferWorkKind workKind)
        {
            instanceUri = null;
            transferId = Guid.Empty;
            displayName = string.Empty;
            workKind = CottonAndroidTransferWorkKind.ManualUpload;

            string? instanceValue = extras?.GetString(InstanceUriKey);
            string? transferValue = extras?.GetString(TransferIdKey);
            string? displayNameValue = extras?.GetString(DisplayNameKey);
            int workKindValue = extras?.GetInt(WorkKindKey, (int)CottonAndroidTransferWorkKind.ManualUpload)
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
