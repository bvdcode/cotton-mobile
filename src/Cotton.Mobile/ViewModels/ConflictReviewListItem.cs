// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Cotton.Mobile.Resources.Localization;
using Cotton.Mobile.Services;

namespace Cotton.Mobile.ViewModels
{
    public class ConflictReviewListItem(CottonSyncConflictSnapshot conflict, bool queued) : ObservableObject
    {
        private bool _isSelected;
        private bool _isQueued = queued;
        public CottonSyncConflictSnapshot Conflict { get; } = conflict;
        public string Name => Conflict.LocalFile.DisplayName;
        public string Path => Conflict.LocalFile.RelativePath;
        public bool IsPathVisible => Path != Name;
        public string PhoneSize => Format(ConflictReviewResources.PhoneSize, Size(Conflict.LocalFile.SizeBytes));
        public string CloudSize => Format(ConflictReviewResources.CloudSize, Size(Conflict.RemoteSizeBytes));
        public string PhoneDate => Format(ConflictReviewResources.PhoneDate, Date(Conflict.LocalFile.LocalUpdatedAtUtc));
        public string CloudDate => Format(ConflictReviewResources.CloudDate, Date(Conflict.RemoteUpdatedAt));
        public bool CanSelect => Conflict.CanReplace && !IsQueued;

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value && CanSelect);
        }

        public bool IsQueued
        {
            get => _isQueued;
            set
            {
                if (SetProperty(ref _isQueued, value))
                {
                    IsSelected = false;
                    OnPropertyChanged(nameof(CanSelect));
                }
            }
        }

        public string Difference
        {
            get
            {
                if (Conflict.RemoteType == CottonFileBrowserEntryType.Folder
                    || Conflict.RemoteRelativePath != Conflict.LocalFile.RelativePath)
                {
                    return ConflictReviewResources.FolderConflict;
                }

                if (!Conflict.LocalFile.SizeBytes.HasValue || !Conflict.RemoteSizeBytes.HasValue)
                {
                    return ConflictReviewResources.SizeUnknown;
                }

                long difference = Conflict.LocalFile.SizeBytes.Value - Conflict.RemoteSizeBytes.Value;
                return difference switch
                {
                    > 0 => Format(ConflictReviewResources.Larger, Size(difference)),
                    < 0 => Format(ConflictReviewResources.Smaller, Size(-difference)),
                    0 => ConflictReviewResources.DifferentContent,
                };
            }
        }

        private static string Format(string format, object value)
        {
            return string.Format(CultureInfo.CurrentCulture, format, value);
        }

        private static string Date(DateTime value)
        {
            return value.ToLocalTime().ToString("g", CultureInfo.CurrentCulture);
        }

        private static string Size(long? bytes)
        {
            return bytes.HasValue
                ? string.Format(CultureInfo.CurrentCulture, ConflictReviewResources.Bytes,
                    CottonFileSizeFormatter.Format(bytes.Value), bytes.Value)
                : ConflictReviewResources.Unknown;
        }
    }
}
