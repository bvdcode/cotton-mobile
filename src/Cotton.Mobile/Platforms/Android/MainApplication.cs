// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Android.App;
using Android.Runtime;

namespace Cotton.Mobile
{
	[Application]
	public class MainApplication(IntPtr handle, JniHandleOwnership ownership) : MauiApplication(handle, ownership)
	{
        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
	}
}
