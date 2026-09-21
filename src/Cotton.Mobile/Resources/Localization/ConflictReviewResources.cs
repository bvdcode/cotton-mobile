// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using System.Globalization;
using System.Resources;

namespace Cotton.Mobile.Resources.Localization
{
    public static class ConflictReviewResources
    {
        private static readonly ResourceManager ResourceManagerInstance = new(typeof(ConflictReviewResources));
        public static string Title => ResourceManagerInstance.GetString(nameof(Title), CultureInfo.CurrentUICulture)
            ?? throw new MissingManifestResourceException(nameof(Title));
        public static string Description => ResourceManagerInstance.GetString(nameof(Description), CultureInfo.CurrentUICulture)
            ?? throw new MissingManifestResourceException(nameof(Description));
        public static string SelectAll => ResourceManagerInstance.GetString(nameof(SelectAll), CultureInfo.CurrentUICulture)
            ?? throw new MissingManifestResourceException(nameof(SelectAll));
        public static string ClearSelection => ResourceManagerInstance.GetString(nameof(ClearSelection), CultureInfo.CurrentUICulture)
            ?? throw new MissingManifestResourceException(nameof(ClearSelection));
        public static string Replace => ResourceManagerInstance.GetString(nameof(Replace), CultureInfo.CurrentUICulture)
            ?? throw new MissingManifestResourceException(nameof(Replace));
        public static string Refresh => ResourceManagerInstance.GetString(nameof(Refresh), CultureInfo.CurrentUICulture)
            ?? throw new MissingManifestResourceException(nameof(Refresh));
        public static string PhoneSize => ResourceManagerInstance.GetString(nameof(PhoneSize), CultureInfo.CurrentUICulture)
            ?? throw new MissingManifestResourceException(nameof(PhoneSize));
        public static string CloudSize => ResourceManagerInstance.GetString(nameof(CloudSize), CultureInfo.CurrentUICulture)
            ?? throw new MissingManifestResourceException(nameof(CloudSize));
        public static string PhoneDate => ResourceManagerInstance.GetString(nameof(PhoneDate), CultureInfo.CurrentUICulture)
            ?? throw new MissingManifestResourceException(nameof(PhoneDate));
        public static string CloudDate => ResourceManagerInstance.GetString(nameof(CloudDate), CultureInfo.CurrentUICulture)
            ?? throw new MissingManifestResourceException(nameof(CloudDate));
        public static string Larger => ResourceManagerInstance.GetString(nameof(Larger), CultureInfo.CurrentUICulture)
            ?? throw new MissingManifestResourceException(nameof(Larger));
        public static string Smaller => ResourceManagerInstance.GetString(nameof(Smaller), CultureInfo.CurrentUICulture)
            ?? throw new MissingManifestResourceException(nameof(Smaller));
        public static string DifferentContent => ResourceManagerInstance.GetString(nameof(DifferentContent), CultureInfo.CurrentUICulture)
            ?? throw new MissingManifestResourceException(nameof(DifferentContent));
        public static string SizeUnknown => ResourceManagerInstance.GetString(nameof(SizeUnknown), CultureInfo.CurrentUICulture)
            ?? throw new MissingManifestResourceException(nameof(SizeUnknown));
        public static string FolderConflict => ResourceManagerInstance.GetString(nameof(FolderConflict), CultureInfo.CurrentUICulture)
            ?? throw new MissingManifestResourceException(nameof(FolderConflict));
        public static string Queued => ResourceManagerInstance.GetString(nameof(Queued), CultureInfo.CurrentUICulture)
            ?? throw new MissingManifestResourceException(nameof(Queued));
        public static string QueuedStatus => ResourceManagerInstance.GetString(nameof(QueuedStatus), CultureInfo.CurrentUICulture)
            ?? throw new MissingManifestResourceException(nameof(QueuedStatus));
        public static string Empty => ResourceManagerInstance.GetString(nameof(Empty), CultureInfo.CurrentUICulture)
            ?? throw new MissingManifestResourceException(nameof(Empty));
        public static string Loading => ResourceManagerInstance.GetString(nameof(Loading), CultureInfo.CurrentUICulture)
            ?? throw new MissingManifestResourceException(nameof(Loading));
        public static string Failed => ResourceManagerInstance.GetString(nameof(Failed), CultureInfo.CurrentUICulture)
            ?? throw new MissingManifestResourceException(nameof(Failed));
        public static string ConfirmTitle => ResourceManagerInstance.GetString(nameof(ConfirmTitle), CultureInfo.CurrentUICulture)
            ?? throw new MissingManifestResourceException(nameof(ConfirmTitle));
        public static string ConfirmBody => ResourceManagerInstance.GetString(nameof(ConfirmBody), CultureInfo.CurrentUICulture)
            ?? throw new MissingManifestResourceException(nameof(ConfirmBody));
        public static string Confirm => ResourceManagerInstance.GetString(nameof(Confirm), CultureInfo.CurrentUICulture)
            ?? throw new MissingManifestResourceException(nameof(Confirm));
        public static string Cancel => ResourceManagerInstance.GetString(nameof(Cancel), CultureInfo.CurrentUICulture)
            ?? throw new MissingManifestResourceException(nameof(Cancel));
        public static string RetryQueue => ResourceManagerInstance.GetString(nameof(RetryQueue), CultureInfo.CurrentUICulture)
            ?? throw new MissingManifestResourceException(nameof(RetryQueue));
        public static string Unknown => ResourceManagerInstance.GetString(nameof(Unknown), CultureInfo.CurrentUICulture)
            ?? throw new MissingManifestResourceException(nameof(Unknown));
        public static string Bytes => ResourceManagerInstance.GetString(nameof(Bytes), CultureInfo.CurrentUICulture)
            ?? throw new MissingManifestResourceException(nameof(Bytes));
    }
}
