// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Commands
{
    internal static class AsyncCommandExecution
    {
        public static async Task RunAsync(
            Func<Task> executeAsync,
            Action<Exception> onException)
        {
            ArgumentNullException.ThrowIfNull(executeAsync);
            ArgumentNullException.ThrowIfNull(onException);

            try
            {
                await executeAsync();
            }
            catch (Exception exception)
            {
                onException(exception);
            }
        }

        public static Task RunAsync<T>(
            T? parameter,
            Func<T, Task> executeAsync,
            Action<Exception> onException)
            where T : class
        {
            ArgumentNullException.ThrowIfNull(executeAsync);

            return RunAsync(
                () => executeAsync(parameter
                    ?? throw new ArgumentNullException(nameof(parameter))),
                onException);
        }
    }
}
