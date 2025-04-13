using System.IO;
using Avalonia.Media.Imaging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ImageProcessingTester;
internal static class AvaloniaImageUtil {
	public static Image<TPixel> ToImage<TPixel>(this Bitmap image) where TPixel : unmanaged, IPixel<TPixel> {
		using var ms = new MemoryStream();
		image.Save(ms);
		ms.Position = 0;
		return Image.Load<TPixel>(ms);
	}
	public static Bitmap ToAvaloniaImage(this Image image) {
		using var ms = new MemoryStream();
		image.SaveAsPngAsync(ms);
		ms.Position = 0;
		return new(ms);
	}
}
