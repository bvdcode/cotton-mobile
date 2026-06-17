using Microsoft.Maui.ApplicationModel;

namespace Cotton.Mobile.Services
{
    public class UserDialogService : IUserDialogService
    {
        public async Task ShowAlertAsync(string title, string message, string cancel)
        {
            Page? page = Application.Current?.Windows.FirstOrDefault()?.Page;
            if (page is null)
            {
                return;
            }

			await MainThread.InvokeOnMainThreadAsync(
				() => page.DisplayAlertAsync(title, message, cancel));
		}

        public async Task<string?> ShowActionSheetAsync(
            string title,
            string cancel,
            string? destruction,
            params string[] buttons)
        {
            Page? page = Application.Current?.Windows.FirstOrDefault()?.Page;
            if (page is null)
            {
                return null;
            }

            return await MainThread.InvokeOnMainThreadAsync(
                () => page.DisplayActionSheetAsync(title, cancel, destruction, buttons));
        }
	}
}
