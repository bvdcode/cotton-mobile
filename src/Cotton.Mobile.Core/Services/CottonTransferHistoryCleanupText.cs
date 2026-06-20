namespace Cotton.Mobile.Services
{
    public static class CottonTransferHistoryCleanupText
    {
        public const string CancelAction = "Cancel";
        public const string ClearHistoryAction = "Clear history";
        public const string ClearHistoryTitle = "Clear completed transfer history";
        public const string ClearHistoryMessage =
            "Completed and cancelled transfers will be removed from this device. Waiting, running, paused, and failed uploads stay in Transfers.";

        public static string CreateClearedStatus(CottonTransferHistoryCleanupPlan plan)
        {
            ArgumentNullException.ThrowIfNull(plan);

            return plan.RemovedCount switch
            {
                0 => "No completed transfer history to clear.",
                1 => "1 transfer history item cleared.",
                _ => $"{plan.RemovedCount:N0} transfer history items cleared.",
            };
        }
    }
}
