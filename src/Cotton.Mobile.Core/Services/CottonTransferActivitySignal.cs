namespace Cotton.Mobile.Services
{
    public sealed class CottonTransferActivitySignal : ICottonTransferActivitySignal
    {
        public event EventHandler? TransferActivityChanged;

        public void NotifyTransferActivityChanged()
        {
            TransferActivityChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
