using Windows.Graphics.Imaging;
using Windows.Storage;

namespace SmartWallpaper.Services;

/// <summary>
/// Готовит картинку ровно под разрешение монитора (пиксель в пиксель),
/// чтобы Windows ничего не масштабировала сама. Декодер системный — понимает JPG, PNG, BMP, WEBP, HEIC и т.д.
/// </summary>
public static class WallpaperRenderer
{
    public static async Task RenderAsync(string sourcePath, string destPath, int width, int height, FitMode fit)
    {
        var source = await StorageFile.GetFileFromPathAsync(sourcePath);
        using var input = await source.OpenAsync(FileAccessMode.Read);
        var decoder = await BitmapDecoder.CreateAsync(input);

        double sw = decoder.OrientedPixelWidth, sh = decoder.OrientedPixelHeight;
        var transform = new BitmapTransform { InterpolationMode = BitmapInterpolationMode.Fant };
        uint outW, outH;

        switch (fit)
        {
            case FitMode.Fill:
            {
                double scale = Math.Max(width / sw, height / sh);
                transform.ScaledWidth = (uint)Math.Max(width, Math.Ceiling(sw * scale));
                transform.ScaledHeight = (uint)Math.Max(height, Math.Ceiling(sh * scale));
                transform.Bounds = new BitmapBounds
                {
                    X = (transform.ScaledWidth - (uint)width) / 2,
                    Y = (transform.ScaledHeight - (uint)height) / 2,
                    Width = (uint)width,
                    Height = (uint)height,
                };
                (outW, outH) = ((uint)width, (uint)height);
                break;
            }
            case FitMode.Fit:
            {
                double scale = Math.Min(width / sw, height / sh);
                transform.ScaledWidth = (uint)Math.Clamp(Math.Round(sw * scale), 1, width);
                transform.ScaledHeight = (uint)Math.Clamp(Math.Round(sh * scale), 1, height);
                (outW, outH) = (transform.ScaledWidth, transform.ScaledHeight);
                break;
            }
            default:
                transform.ScaledWidth = (uint)width;
                transform.ScaledHeight = (uint)height;
                (outW, outH) = ((uint)width, (uint)height);
                break;
        }

        var pixelData = await decoder.GetPixelDataAsync(
            BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore, transform,
            ExifOrientationMode.RespectExifOrientation, ColorManagementMode.ColorManageToSRgb);
        byte[] pixels = pixelData.DetachPixelData();

        if (fit == FitMode.Fit && (outW != width || outH != height))
            pixels = Letterbox(pixels, (int)outW, (int)outH, width, height);

        Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
        string temp = destPath + ".tmp";
        using (var file = new FileStream(temp, FileMode.Create, FileAccess.ReadWrite))
        using (var output = file.AsRandomAccessStream())
        {
            var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, output);
            encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore, (uint)width, (uint)height, 96, 96, pixels);
            await encoder.FlushAsync();
        }
        File.Move(temp, destPath, overwrite: true);
    }

    /// <summary>Кладёт картинку по центру чёрного холста размером с монитор.</summary>
    private static byte[] Letterbox(byte[] image, int w, int h, int canvasW, int canvasH)
    {
        var canvas = new byte[canvasW * canvasH * 4];
        for (int i = 3; i < canvas.Length; i += 4) canvas[i] = 255; // непрозрачный чёрный

        int offsetX = (canvasW - w) / 2, offsetY = (canvasH - h) / 2;
        for (int y = 0; y < h; y++)
            System.Buffer.BlockCopy(image, y * w * 4, canvas, ((offsetY + y) * canvasW + offsetX) * 4, w * 4);
        return canvas;
    }
}
