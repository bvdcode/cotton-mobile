// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Microsoft.Maui.ApplicationModel;

namespace Cotton.Mobile.Services
{
    internal static class ModalPageNavigation
    {
        public static async Task<INavigation> ShowAsync(Page page)
        {
            ArgumentNullException.ThrowIfNull(page);

            INavigation navigation = await MainThread.InvokeOnMainThreadAsync(GetNavigation);
            await MainThread.InvokeOnMainThreadAsync(
                () => navigation.PushModalAsync(page, animated: false));
            return navigation;
        }

        public static Task DismissAsync(INavigation navigation, Page page)
        {
            ArgumentNullException.ThrowIfNull(navigation);
            ArgumentNullException.ThrowIfNull(page);

            return MainThread.InvokeOnMainThreadAsync(async () =>
            {
                IReadOnlyList<Page> modalStack = navigation.ModalStack;
                if (!modalStack.Contains(page))
                {
                    return;
                }

                if (!ReferenceEquals(modalStack[^1], page))
                {
                    throw new InvalidOperationException("The modal page must be active before dismissal.");
                }

                await navigation.PopModalAsync(animated: false);
            });
        }

        private static INavigation GetNavigation()
        {
            return ActiveApplicationPage.GetRequired().Navigation;
        }
    }
}
