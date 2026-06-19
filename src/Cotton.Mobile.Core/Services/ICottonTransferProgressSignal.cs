namespace Cotton.Mobile.Services
{
    public interface ICottonTransferProgressSignal
    {
        event EventHandler<CottonTransferProgressChangedEventArgs>? TransferProgressChanged;

        void NotifyTransferProgressChanged(
            Guid transferId,
            CottonTransferProgressSnapshot progress);
    }
}
