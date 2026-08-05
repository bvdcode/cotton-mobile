// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Behaviors
{
    internal class AndroidLongPressRunnable(Action action) : Java.Lang.Object, Java.Lang.IRunnable
    {
        private readonly Action _action = action ?? throw new ArgumentNullException(nameof(action));

        public void Run()
        {
            _action();
        }
    }
}
