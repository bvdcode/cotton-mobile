using Microsoft.Maui.ApplicationModel;

namespace Cotton.Mobile.Services
{
    public class UserDialogService : IUserDialogService
    {
        public async Task ShowAlertAsync(string title, string message, string cancel)
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                Page? page = GetCurrentPage();
                if (page is null)
                {
                    return;
                }

                await page.DisplayAlertAsync(title, message, cancel);
            });
        }

        public async Task<bool> ShowConfirmationAsync(string title, string message, string accept, string cancel)
        {
            return await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                Page? page = GetCurrentPage();
                if (page is null)
                {
                    return false;
                }

                return await page.DisplayAlertAsync(title, message, accept, cancel);
            });
        }

        public async Task<string?> ShowActionSheetAsync(
            string title,
            string cancel,
            string? destruction,
            params string[] buttons)
        {
            return await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                Page? page = GetCurrentPage();
                if (page is null)
                {
                    return null;
                }

                return await page.DisplayActionSheetAsync(title, cancel, destruction, buttons);
            });
        }

        private static Page? GetCurrentPage()
        {
            return Application.Current?.Windows.FirstOrDefault()?.Page;
        }
    }
}
