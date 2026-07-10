// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

#if DEBUG
using Cotton.Mobile.Services;
using Cotton.Mobile.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Storage;
using System.Text;
using AndroidBitmap = Android.Graphics.Bitmap;
using AndroidCanvas = Android.Graphics.Canvas;
using AndroidColor = Android.Graphics.Color;
using AndroidPaint = Android.Graphics.Paint;
using AndroidPaintFlags = Android.Graphics.PaintFlags;
using AndroidPdfDocument = Android.Graphics.Pdf.PdfDocument;
using AndroidRectF = Android.Graphics.RectF;
using IoPath = System.IO.Path;

namespace Cotton.Mobile
{
    internal static class AndroidVisualQaFixture
    {
        private const string FixtureDirectoryName = "visual-qa";
        private const int ImageHeight = 900;
        private const int ImageWidth = 1200;
        private const int PdfHeight = 1754;
        private const int PdfWidth = 1240;

        public static async Task OpenFilePreviewAsync(IServiceProvider services, string previewName)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentException.ThrowIfNullOrWhiteSpace(previewName);

            VisualQaFileFixture fixture = await Task.Run(() => CreateFileFixture(previewName));
            var file = CottonFileBrowserEntry.CreateFile(
                Guid.NewGuid(),
                fixture.FileName,
                DateTime.UtcNow,
                fixture.SizeBytes,
                fixture.ContentType,
                previewHashEncryptedHex: null,
                eTag: "visual-qa");
            var downloadedFile = new CottonFileDownloadResult(
                fixture.FileName,
                fixture.FilePath,
                fixture.SizeBytes,
                fixture.ContentType);

            await services.GetRequiredService<IFilePreviewService>()
                .OpenAsync(file, downloadedFile);
        }

        public static async Task OpenAppLockGateAsync(IServiceProvider services)
        {
            ArgumentNullException.ThrowIfNull(services);

            AppLockGatePage page = ActivatorUtilities.CreateInstance<AppLockGatePage>(services);
            if (page.BindingContext is not AppLockGateViewModel viewModel)
            {
                throw new InvalidOperationException("Visual QA app lock view model is unavailable.");
            }

            viewModel.SetCompletion(new TaskCompletionSource<CottonDeviceUnlockResult>(
                TaskCreationOptions.RunContinuationsAsynchronously));
            INavigation? navigation = Shell.Current?.Navigation;
            if (navigation is null)
            {
                throw new InvalidOperationException("Visual QA app lock navigation is unavailable.");
            }

            await navigation.PushModalAsync(page, animated: false);
        }

        private static VisualQaFileFixture CreateFileFixture(string previewName)
        {
            string directoryPath = IoPath.Combine(FileSystem.CacheDirectory, FixtureDirectoryName);
            Directory.CreateDirectory(directoryPath);

            VisualQaFileDefinition definition = previewName switch
            {
                "viewer-text" => new VisualQaFileDefinition(
                    "Cotton Android release notes - July 2026.txt",
                    "text/plain",
                    CreateTextFixture),
                "viewer-image" => new VisualQaFileDefinition(
                    "Cotton mobile visual preview - Android 2026.png",
                    "image/png",
                    CreateImageFixture),
                "viewer-audio" => new VisualQaFileDefinition(
                    "Cotton notification preview.wav",
                    "audio/wav",
                    CreateAudioFixture),
                "viewer-pdf" => new VisualQaFileDefinition(
                    "Cotton mobile security overview - July 2026.pdf",
                    "application/pdf",
                    CreatePdfFixture),
                _ => throw new ArgumentOutOfRangeException(
                    nameof(previewName),
                    previewName,
                    "Visual QA preview kind is unknown."),
            };

            string filePath = IoPath.Combine(directoryPath, definition.FileName);
            definition.WriteFixture(filePath);
            long sizeBytes = new FileInfo(filePath).Length;
            return new VisualQaFileFixture(
                definition.FileName,
                filePath,
                sizeBytes,
                definition.ContentType);
        }

        private static void CreateTextFixture(string filePath)
        {
            const string content = """
                Cotton Cloud for Android

                Release quality brief

                Fast file access
                Open recent documents, images, audio, and PDFs without leaving Cotton.

                Offline by design
                Files marked On device remain available when the network is unavailable.

                Private by default
                App lock, scoped storage, and session controls protect this device.

                Updated July 2026
                """;
            File.WriteAllText(filePath, content, Encoding.UTF8);
        }

        private static void CreateImageFixture(string filePath)
        {
            using AndroidBitmap bitmap = AndroidBitmap.CreateBitmap(
                ImageWidth,
                ImageHeight,
                AndroidBitmap.Config.Argb8888!);
            using var canvas = new AndroidCanvas(bitmap);
            canvas.DrawColor(AndroidColor.Rgb(9, 11, 10));

            using var accentPaint = new AndroidPaint(AndroidPaintFlags.AntiAlias)
            {
                Color = AndroidColor.Rgb(198, 255, 0),
            };
            using var surfacePaint = new AndroidPaint(AndroidPaintFlags.AntiAlias)
            {
                Color = AndroidColor.Rgb(29, 35, 32),
            };
            using var titlePaint = new AndroidPaint(AndroidPaintFlags.AntiAlias)
            {
                Color = AndroidColor.Rgb(244, 247, 244),
                TextSize = 92,
            };
            using var bodyPaint = new AndroidPaint(AndroidPaintFlags.AntiAlias)
            {
                Color = AndroidColor.Rgb(190, 201, 195),
                TextSize = 42,
            };

            canvas.DrawRoundRect(new AndroidRectF(72, 72, 1128, 828), 56, 56, surfacePaint);
            canvas.DrawRoundRect(new AndroidRectF(96, 96, 248, 248), 42, 42, accentPaint);
            canvas.DrawText("COTTON", 96, 430, titlePaint);
            canvas.DrawText("Cloud files, at home on Android", 96, 510, bodyPaint);
            canvas.DrawRoundRect(new AndroidRectF(96, 610, 648, 724), 32, 32, accentPaint);

            using var actionPaint = new AndroidPaint(AndroidPaintFlags.AntiAlias)
            {
                Color = AndroidColor.Rgb(21, 25, 0),
                TextSize = 38,
            };
            canvas.DrawText("OPEN ON DEVICE", 146, 682, actionPaint);

            using FileStream output = File.Create(filePath);
            bool compressed = bitmap.Compress(AndroidBitmap.CompressFormat.Png!, 100, output);
            if (!compressed)
            {
                throw new InvalidOperationException("Could not create the visual QA image fixture.");
            }
        }

        private static void CreateAudioFixture(string filePath)
        {
            const int bitsPerSample = 16;
            const int channelCount = 1;
            const int durationSeconds = 4;
            const int sampleRate = 44100;
            const double frequency = 440;
            int sampleCount = sampleRate * durationSeconds;
            int bytesPerSample = bitsPerSample / 8;
            int dataLength = sampleCount * channelCount * bytesPerSample;

            using FileStream output = File.Create(filePath);
            using var writer = new BinaryWriter(output, Encoding.ASCII, leaveOpen: false);
            writer.Write(Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + dataLength);
            writer.Write(Encoding.ASCII.GetBytes("WAVE"));
            writer.Write(Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)channelCount);
            writer.Write(sampleRate);
            writer.Write(sampleRate * channelCount * bytesPerSample);
            writer.Write((short)(channelCount * bytesPerSample));
            writer.Write((short)bitsPerSample);
            writer.Write(Encoding.ASCII.GetBytes("data"));
            writer.Write(dataLength);

            for (int index = 0; index < sampleCount; index++)
            {
                double envelope = Math.Min(1d, index / (sampleRate * 0.08d));
                double sample = Math.Sin(2d * Math.PI * frequency * index / sampleRate)
                    * short.MaxValue
                    * 0.18d
                    * envelope;
                writer.Write((short)sample);
            }
        }

        private static void CreatePdfFixture(string filePath)
        {
            using var document = new AndroidPdfDocument();
            AddPdfPage(
                document,
                pageNumber: 1,
                title: "Cotton Mobile",
                subtitle: "Security overview",
                lines:
                [
                    "App lock uses the Android device credential.",
                    "Offline files stay inside app-managed storage.",
                    "Active sessions can be reviewed and revoked.",
                ]);
            AddPdfPage(
                document,
                pageNumber: 2,
                title: "Private by default",
                subtitle: "Android 2026",
                lines:
                [
                    "No broad file-system permission is required.",
                    "Media access is requested only for backup.",
                    "Sensitive previews avoid reusable cache copies.",
                ]);

            using FileStream output = File.Create(filePath);
            document.WriteTo(output);
        }

        private static void AddPdfPage(
            AndroidPdfDocument document,
            int pageNumber,
            string title,
            string subtitle,
            IReadOnlyList<string> lines)
        {
            AndroidPdfDocument.PageInfo pageInfo = new AndroidPdfDocument.PageInfo.Builder(
                PdfWidth,
                PdfHeight,
                pageNumber).Create()
                ?? throw new InvalidOperationException("Could not create the visual QA PDF page.");
            AndroidPdfDocument.Page page = document.StartPage(pageInfo)
                ?? throw new InvalidOperationException("Could not start the visual QA PDF page.");
            AndroidCanvas canvas = page.Canvas
                ?? throw new InvalidOperationException("Visual QA PDF page canvas is unavailable.");
            canvas.DrawColor(AndroidColor.Rgb(249, 251, 248));

            using var accentPaint = new AndroidPaint(AndroidPaintFlags.AntiAlias)
            {
                Color = AndroidColor.Rgb(79, 98, 0),
            };
            using var titlePaint = new AndroidPaint(AndroidPaintFlags.AntiAlias)
            {
                Color = AndroidColor.Rgb(24, 29, 26),
                TextSize = 88,
            };
            using var subtitlePaint = new AndroidPaint(AndroidPaintFlags.AntiAlias)
            {
                Color = AndroidColor.Rgb(79, 98, 0),
                TextSize = 42,
            };
            using var bodyPaint = new AndroidPaint(AndroidPaintFlags.AntiAlias)
            {
                Color = AndroidColor.Rgb(64, 72, 67),
                TextSize = 38,
            };

            canvas.DrawRoundRect(new AndroidRectF(88, 96, 1152, 124), 14, 14, accentPaint);
            canvas.DrawText(title, 88, 300, titlePaint);
            canvas.DrawText(subtitle, 88, 390, subtitlePaint);
            for (int index = 0; index < lines.Count; index++)
            {
                float top = 570 + index * 190;
                canvas.DrawRoundRect(new AndroidRectF(88, top - 68, 112, top + 26), 12, 12, accentPaint);
                canvas.DrawText(lines[index], 156, top, bodyPaint);
            }

            canvas.DrawText($"{pageNumber} / 2", 1030, 1640, bodyPaint);
            document.FinishPage(page);
            page.Dispose();
        }

        private record VisualQaFileFixture(
            string FileName,
            string FilePath,
            long SizeBytes,
            string ContentType);

        private record VisualQaFileDefinition(
            string FileName,
            string ContentType,
            Action<string> WriteFixture);
    }
}
#endif
