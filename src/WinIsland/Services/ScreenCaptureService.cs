using System.Drawing;
using System.Windows.Forms;

namespace WinIsland.Services;

public static class ScreenCaptureService
{
    public static void SaveAllDisplays(string path)
    {
        var bounds = SystemInformation.VirtualScreen;
        if (bounds.Width <= 0 || bounds.Height <= 0)
            throw new InvalidOperationException("Windows did not return a valid desktop size.");

        using var bitmap = new Bitmap(bounds.Width, bounds.Height);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bitmap.Size,
                CopyPixelOperation.SourceCopy);
        }
        bitmap.Save(path, System.Drawing.Imaging.ImageFormat.Png);
    }
}
