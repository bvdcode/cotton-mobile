namespace Cotton.Mobile.Services
{
    public interface ICottonTransferActivitySignal
    {
        event EventHandler? TransferActivityChanged;

        void NotifyTransferActivityChanged();
    }
}
