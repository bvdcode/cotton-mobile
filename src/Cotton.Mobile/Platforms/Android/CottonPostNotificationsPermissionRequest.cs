// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Android;
using Microsoft.Maui.ApplicationModel;

namespace Cotton.Mobile
{
    public class CottonPostNotificationsPermissionRequest : Permissions.BasePlatformPermission
    {
        public override (string androidPermission, bool isRuntime)[] RequiredPermissions
        {
            get
            {
                if (!OperatingSystem.IsAndroidVersionAtLeast(33))
                {
                    return [];
                }

                return
                [
                    (Manifest.Permission.PostNotifications, true),
                ];
            }
        }
    }
}
