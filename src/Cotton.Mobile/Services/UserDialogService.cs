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
	}
}
