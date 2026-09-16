// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Android;

namespace Cotton.Mobile.Platforms.Android
{
    public class CottonMediaLocationPermissionRequest : CottonMediaReadPermissionRequest
    {
        public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
            OperatingSystem.IsAndroidVersionAtLeast(29)
                ? [.. base.RequiredPermissions, (Manifest.Permission.AccessMediaLocation, true)]
                : base.RequiredPermissions;
    }
}
