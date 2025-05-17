using System.IO;
using Avalonia.Media.Imaging;
using SixLabors.ImageSharp;

namespace VisionTester;
internal static class AvaloniaImageUtil {
	public static Bitmap ToAvaloniaImage(this Image image) {
		using var ms = new MemoryStream();
		image.SaveAsPngAsync(ms);
		ms.Position = 0;
		return new(ms);
	}
}
