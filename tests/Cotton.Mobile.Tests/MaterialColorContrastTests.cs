using System.Globalization;
using System.Xml.Linq;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class MaterialColorContrastTests
    {
        private static readonly string[] ThemeNames = ["Light", "Dark"];

        private const string ColorsPath = "src/Cotton.Mobile/Resources/Styles/Colors.xaml";
        private const double MinimumTextContrast = 4.5;
        private const double MinimumNonTextContrast = 3.0;
        private static readonly XNamespace XamlNamespace = "http://schemas.microsoft.com/winfx/2009/xaml";

        [Fact]
        public void MaterialThemeTextPairingsMeetWcagAa()
        {
            IReadOnlyDictionary<string, Rgba> colors = LoadColors();

            foreach (string theme in ThemeNames)
            {
                AssertContrast(colors, theme, "OnPrimary", "Primary", MinimumTextContrast);
                AssertContrast(colors, theme, "OnPrimaryContainer", "PrimaryContainer", MinimumTextContrast);
                AssertContrast(colors, theme, "OnSecondary", "Secondary", MinimumTextContrast);
                AssertContrast(colors, theme, "OnSecondaryContainer", "SecondaryContainer", MinimumTextContrast);
                AssertContrast(colors, theme, "OnTertiary", "Tertiary", MinimumTextContrast);
                AssertContrast(colors, theme, "OnTertiaryContainer", "TertiaryContainer", MinimumTextContrast);
                AssertContrast(colors, theme, "OnError", "Error", MinimumTextContrast);
                AssertContrast(colors, theme, "OnErrorContainer", "ErrorContainer", MinimumTextContrast);
                AssertContrast(colors, theme, "InverseOnSurface", "InverseSurface", MinimumTextContrast);
                AssertContrast(colors, theme, "OnSurface", "Surface", MinimumTextContrast);
                AssertContrast(colors, theme, "OnSurfaceVariant", "SurfaceVariant", MinimumTextContrast);
                AssertContrast(colors, theme, "OnBackground", "Background", MinimumTextContrast);

                // Pairings the app actually renders: supporting copy sits on Surface,
                // and both copy and titles sit on PrimaryContainer inside status and selected cards.
                AssertContrast(colors, theme, "OnSurfaceVariant", "Surface", MinimumTextContrast);
                AssertContrast(colors, theme, "OnSurfaceVariant", "PrimaryContainer", MinimumTextContrast);
                AssertContrast(colors, theme, "OnSurface", "PrimaryContainer", MinimumTextContrast);
            }
        }

        [Fact]
        public void MaterialThemeActionsAndBoundariesMeetNonTextContrast()
        {
            IReadOnlyDictionary<string, Rgba> colors = LoadColors();

            foreach (string theme in ThemeNames)
            {
                AssertContrast(colors, theme, "Primary", "Surface", MinimumNonTextContrast);
                AssertContrast(colors, theme, "Error", "Surface", MinimumNonTextContrast);
                AssertContrast(colors, theme, "Outline", "Surface", MinimumNonTextContrast);

                // Glyph tints: action icons on the bar and on the selected navigation pill,
                // and framed icons drawn on a secondary container.
                AssertContrast(colors, theme, "Primary", "PrimaryContainer", MinimumNonTextContrast);
                AssertContrast(colors, theme, "OnSecondaryContainer", "SecondaryContainer", MinimumNonTextContrast);
            }
        }

        private static Dictionary<string, Rgba> LoadColors()
        {
            XDocument document = XDocument.Parse(RepositoryPath.ReadText(ColorsPath));
            return document
                .Descendants()
                .Where(element => element.Name.LocalName == "Color")
                .ToDictionary(
                    element => (string?)element.Attribute(XamlNamespace + "Key")
                        ?? throw new InvalidOperationException("Material color is missing x:Key."),
                    element => ParseColor(element.Value),
                    StringComparer.Ordinal);
        }

        private static Rgba ParseColor(string value)
        {
            string normalized = value.Trim();
            if (string.Equals(normalized, "Transparent", StringComparison.OrdinalIgnoreCase))
            {
                return new Rgba(0d, 0d, 0d, 0d);
            }

            string hex = normalized.TrimStart('#');
            if (hex.Length is not (6 or 8))
            {
                throw new InvalidOperationException($"Unsupported material color '{value}'.");
            }

            int offset = hex.Length == 8 ? 2 : 0;
            double alpha = hex.Length == 8 ? ParseByte(hex, 0) / 255d : 1d;
            return new Rgba(
                ParseByte(hex, offset) / 255d,
                ParseByte(hex, offset + 2) / 255d,
                ParseByte(hex, offset + 4) / 255d,
                alpha);
        }

        private static int ParseByte(string hex, int offset)
        {
            return int.Parse(hex.AsSpan(offset, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        }

        private static void AssertContrast(
            IReadOnlyDictionary<string, Rgba> colors,
            string theme,
            string foregroundRole,
            string backgroundRole,
            double minimumContrast)
        {
            string suffix = string.Equals(theme, "Dark", StringComparison.Ordinal) ? "Dark" : string.Empty;
            string foregroundKey = $"{foregroundRole}{suffix}";
            string backgroundKey = $"{backgroundRole}{suffix}";
            double contrast = Contrast(colors[foregroundKey], colors[backgroundKey]);

            Assert.True(
                contrast >= minimumContrast,
                $"{foregroundKey} on {backgroundKey} is {contrast:F2}:1; expected at least {minimumContrast:F1}:1.");
        }

        private static double Contrast(Rgba foreground, Rgba background)
        {
            Rgba composite = Composite(foreground, background);
            double foregroundLuminance = RelativeLuminance(composite);
            double backgroundLuminance = RelativeLuminance(background);
            double lighter = Math.Max(foregroundLuminance, backgroundLuminance);
            double darker = Math.Min(foregroundLuminance, backgroundLuminance);
            return (lighter + 0.05d) / (darker + 0.05d);
        }

        private static Rgba Composite(Rgba foreground, Rgba background)
        {
            if (Math.Abs(background.Alpha - 1d) > double.Epsilon)
            {
                throw new InvalidOperationException("Material contrast backgrounds must be opaque.");
            }

            double inverseAlpha = 1d - foreground.Alpha;
            return new Rgba(
                (foreground.Red * foreground.Alpha) + (background.Red * inverseAlpha),
                (foreground.Green * foreground.Alpha) + (background.Green * inverseAlpha),
                (foreground.Blue * foreground.Alpha) + (background.Blue * inverseAlpha),
                1d);
        }

        private static double RelativeLuminance(Rgba color)
        {
            return (0.2126d * Linearize(color.Red))
                + (0.7152d * Linearize(color.Green))
                + (0.0722d * Linearize(color.Blue));
        }

        private static double Linearize(double channel)
        {
            return channel <= 0.04045d
                ? channel / 12.92d
                : Math.Pow((channel + 0.055d) / 1.055d, 2.4d);
        }
    }
}
