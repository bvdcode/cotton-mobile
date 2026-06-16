namespace Cotton.Mobile.Services
{
    public interface IUserDialogService
    {
        Task ShowAlertAsync(string title, string message, string cancel);
    }
}
