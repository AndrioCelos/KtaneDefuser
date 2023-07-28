using System.Drawing.Imaging;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.PixelFormats;

namespace ImageProcessingTester;
internal static class WinFormsImageUtils {
	public static SixLabors.ImageSharp.Image<TPixel> ToImage<TPixel>(this Image image) where TPixel : unmanaged, IPixel<TPixel> {
		using var ms = new MemoryStream();
		image.Save(ms, ImageFormat.Bmp);
		ms.Position = 0;
		return SixLabors.ImageSharp.Image.Load<TPixel>(ms);
	}
	public static Bitmap ToWinFormsImage(this SixLabors.ImageSharp.Image image) {
		using var ms = new MemoryStream();
		image.Save(ms, new BmpEncoder());
		ms.Position = 0;
		return new Bitmap(ms);
	}
}
