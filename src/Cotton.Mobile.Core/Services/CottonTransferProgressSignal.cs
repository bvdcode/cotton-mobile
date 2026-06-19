namespace Cotton.Mobile.Services
{
    public class CottonTransferProgressSignal : ICottonTransferProgressSignal
    {
        public event EventHandler<CottonTransferProgressChangedEventArgs>? TransferProgressChanged;

        public void NotifyTransferProgressChanged(
            Guid transferId,
            CottonTransferProgressSnapshot progress)
        {
            TransferProgressChanged?.Invoke(
                this,
                new CottonTransferProgressChangedEventArgs(transferId, progress));
        }
    }
}
