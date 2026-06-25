// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Microsoft.Maui.Controls.Shapes;

namespace Cotton.Mobile.Controls
{
    public static class IconPathData
    {
        private static readonly PathGeometryConverter Converter = new();

        public static Geometry ArrowUp => Create("M4 12 L5.4 13.4 L11 7.8 L11 20 L13 20 L13 7.8 L18.6 13.4 L20 12 L12 4 Z");

        public static Geometry Backup => Create("M12 3 L17 8 L14 8 L14 14 L10 14 L10 8 L7 8 Z M5 17 L19 17 L19 19 L5 19 Z");

        public static Geometry Check => Create("M9 16.2 L4.8 12 L3.4 13.4 L9 19 L21 7 L19.6 5.6 Z");

        public static Geometry Close => Create("M6.4 5 L12 10.6 L17.6 5 L19 6.4 L13.4 12 L19 17.6 L17.6 19 L12 13.4 L6.4 19 L5 17.6 L10.6 12 L5 6.4 Z");

        public static Geometry Download => Create("M11 4 L13 4 L13 13 L16.5 9.5 L18 11 L12 17 L6 11 L7.5 9.5 L11 13 Z M5 19 L19 19 L19 21 L5 21 Z");

        public static Geometry Error => Create("M12 3 L22 20 L2 20 Z M11 9 L13 9 L13 14 L11 14 Z M11 16 L13 16 L13 18 L11 18 Z");

        public static Geometry MoreVertical => Create("M11 5 L13 5 L13 7 L11 7 Z M11 11 L13 11 L13 13 L11 13 Z M11 17 L13 17 L13 19 L11 19 Z");

        public static Geometry Play => Create("M8 5 L19 12 L8 19 Z");

        public static Geometry Plus => Create("M11 5 L13 5 L13 11 L19 11 L19 13 L13 13 L13 19 L11 19 L11 13 L5 13 L5 11 L11 11 Z");

        public static Geometry Reset => Create("M12 5 C8.7 5 6 7.7 6 11 L3 11 L7 15 L11 11 L8 11 C8 8.8 9.8 7 12 7 C14.2 7 16 8.8 16 11 C16 13.2 14.2 15 12 15 C10.8 15 9.7 14.5 9 13.7 L7.6 15.1 C8.7 16.3 10.3 17 12 17 C15.3 17 18 14.3 18 11 C18 7.7 15.3 5 12 5 Z");

        public static Geometry Search => Create("M9.5 3 C5.9 3 3 5.9 3 9.5 C3 13.1 5.9 16 9.5 16 C11 16 12.4 15.5 13.5 14.7 L18.7 20 L20 18.7 L14.8 13.5 C15.6 12.4 16 11 16 9.5 C16 5.9 13.1 3 9.5 3 Z M9.5 5 C12 5 14 7 14 9.5 C14 12 12 14 9.5 14 C7 14 5 12 5 9.5 C5 7 7 5 9.5 5 Z");

        public static Geometry Transfer => Create("M7 3 L3 7 L6 7 L6 14 L8 14 L8 7 L11 7 Z M17 21 L21 17 L18 17 L18 10 L16 10 L16 17 L13 17 Z");

        public static Geometry ViewTiles => Create("M4 5 L10 5 L10 11 L4 11 Z M14 5 L20 5 L20 11 L14 11 Z M4 13 L10 13 L10 19 L4 19 Z M14 13 L20 13 L20 19 L14 19 Z");

        private static Geometry Create(string path)
        {
            object? value = Converter.ConvertFromInvariantString(path);
            if (value is Geometry geometry)
            {
                return geometry;
            }

            throw new InvalidOperationException("Icon path data must create a geometry.");
        }
    }
}
