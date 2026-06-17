namespace Cotton.Mobile.Services
{
    public interface IUserDialogService
    {
        Task ShowAlertAsync(string title, string message, string cancel);

        Task<string?> ShowActionSheetAsync(
            string title,
            string cancel,
            string? destruction,
            params string[] buttons);
    }
}
