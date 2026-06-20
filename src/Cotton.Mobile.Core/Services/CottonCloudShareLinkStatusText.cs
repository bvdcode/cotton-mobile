using System.Net;

namespace Cotton.Mobile.Services
{
    public static class CottonCloudShareLinkStatusText
    {
        public const string OfflineUnavailableStatus = "Offline. Link creation needs internet.";
        public const string CancelledStatus = "Link action cancelled.";
        public const string CopyFailedStatus = "Could not copy link.";
        public const string ShareFailedStatus = "Could not share link.";
        public const string ResetFileLinksOfflineUnavailableStatus = "Offline. Reset file links needs internet.";
        public const string ResetFileLinksUnavailableStatus = "Sign in to reset file links.";
        public const string ResetFileLinksInProgressStatus = "Resetting file links...";
        public const string ResetFileLinksCompletedStatus = "File links reset.";
        public const string ResetFileLinksCancelledStatus = "Reset file links cancelled.";

        public static string CreateCreatingStatus(string targetName)
        {
            return $"Creating link for {NormalizeTargetName(targetName)}...";
        }

        public static string CreateCopyingStatus(string targetName)
        {
            return $"Copying link for {NormalizeTargetName(targetName)}...";
        }

        public static string CreateSharingStatus(string targetName)
        {
            return $"Sharing link for {NormalizeTargetName(targetName)}...";
        }

        public static string CreateCopiedStatus(string targetName)
        {
            return $"Link copied for {NormalizeTargetName(targetName)}.";
        }

        public static string CreateCreatingStatus(int targetCount)
        {
            int count = NormalizeTargetCount(targetCount);
            return count == 1 ? "Creating link..." : $"Creating {count} links...";
        }

        public static string CreateCreatingItemStatus(int itemPosition, int targetCount)
        {
            int count = NormalizeTargetCount(targetCount);
            int position = Math.Clamp(itemPosition, 1, count);
            return count == 1 ? "Creating link..." : $"Creating link {position} of {count}...";
        }

        public static string CreateCopyingStatus(int targetCount)
        {
            int count = NormalizeTargetCount(targetCount);
            return count == 1 ? "Copying link..." : $"Copying {count} links...";
        }

        public static string CreateSharingStatus(int targetCount)
        {
            int count = NormalizeTargetCount(targetCount);
            return count == 1 ? "Sharing link..." : $"Sharing {count} links...";
        }

        public static string CreateCopiedStatus(int targetCount)
        {
            int count = NormalizeTargetCount(targetCount);
            return count == 1 ? "Link copied." : $"{count} links copied.";
        }

        public static string CreateCreationFailedStatus(
            CottonCloudShareLinkTargetKind targetKind,
            HttpStatusCode? statusCode,
            bool hasInternetAccess)
        {
            if (!hasInternetAccess)
            {
                return OfflineUnavailableStatus;
            }

            if (statusCode is HttpStatusCode.NotFound or HttpStatusCode.Gone)
            {
                return $"Could not create link. {CreateTargetLabel(targetKind)} is no longer available.";
            }

            if (statusCode is HttpStatusCode.BadRequest
                or HttpStatusCode.Conflict
                or HttpStatusCode.UnprocessableEntity)
            {
                return $"Could not create link. Server rejected this {CreateTargetLabel(targetKind).ToLowerInvariant()}.";
            }

            return "Could not create link.";
        }

        public static string CreateBulkCreationFailedStatus(HttpStatusCode? statusCode, bool hasInternetAccess)
        {
            if (!hasInternetAccess)
            {
                return OfflineUnavailableStatus;
            }

            if (statusCode is HttpStatusCode.NotFound or HttpStatusCode.Gone)
            {
                return "Could not create links. Some selected items are no longer available.";
            }

            if (statusCode is HttpStatusCode.BadRequest
                or HttpStatusCode.Conflict
                or HttpStatusCode.UnprocessableEntity)
            {
                return "Could not create links. Server rejected one of the selected items.";
            }

            return "Could not create links.";
        }

        public static string CreateResetFileLinksFailedStatus(HttpStatusCode? statusCode, bool hasInternetAccess)
        {
            if (!hasInternetAccess)
            {
                return ResetFileLinksOfflineUnavailableStatus;
            }

            if (statusCode is HttpStatusCode.Forbidden)
            {
                return "Could not reset file links. Sign in again.";
            }

            return "Could not reset file links.";
        }

        private static string NormalizeTargetName(string targetName)
        {
            return string.IsNullOrWhiteSpace(targetName) ? "item" : targetName.Trim();
        }

        private static int NormalizeTargetCount(int targetCount)
        {
            return Math.Max(1, targetCount);
        }

        private static string CreateTargetLabel(CottonCloudShareLinkTargetKind targetKind)
        {
            return targetKind switch
            {
                CottonCloudShareLinkTargetKind.Folder => "Folder",
                _ => "File",
            };
        }
    }
}
