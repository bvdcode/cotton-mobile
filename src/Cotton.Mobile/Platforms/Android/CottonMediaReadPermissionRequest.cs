// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Android;
using Microsoft.Maui.ApplicationModel;

namespace Cotton.Mobile.Platforms.Android
{
    public class CottonMediaReadPermissionRequest : Permissions.BasePlatformPermission
    {
        public override (string androidPermission, bool isRuntime)[] RequiredPermissions
        {
            get
            {
                if (OperatingSystem.IsAndroidVersionAtLeast(33))
                {
                    return
                    [
                        (Manifest.Permission.ReadMediaImages, true),
                        (Manifest.Permission.ReadMediaVideo, true),
                    ];
                }

                return
                [
                    (Manifest.Permission.ReadExternalStorage, true),
                ];
            }
        }
    }
}
