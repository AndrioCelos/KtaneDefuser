using System;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Tesseract;

namespace KtaneDefuserConnector.Components;
public class TurnTheKey : ComponentReader<TurnTheKey.ReadData> {
	public override string Name => "Turn the Key";

	protected internal override ReadData Process(Image<Rgba32> image, LightsState lightsState, ref Image<Rgba32>? debugImage) {
		using var displayImage = GetNeedyDisplayImage(image, lightsState, debugImage);

		var d0 = ReadDigit(displayImage, 97);
		var d1 = ReadDigit(displayImage, 72);
		var d2 = ReadDigit(displayImage, 40);
		var d3 = ReadDigit(displayImage, 15);

		return new(new(0, d2 + d3 * 10, d0 + d1 * 10));

		static bool CheckH(Image<Rgba32> image, int x1, int x2, int y) {
			for (var x = x1; x < x2; x++) {
				var p = image[x, y];
				if (p.R >= 128) return true;
			}
			return false;
		}
		static bool CheckV(Image<Rgba32> image, int x, int y1, int y2) {
			for (var y = y1; y < y2; y++) {
				var p = image[x, y];
				if (p.R >= 128) return true;
			}
			return false;
		}
		static int ReadDigit(Image<Rgba32> image, int x) {
			var segments =
				(CheckV(image, x + 10,  4, 16) ? (1 << 0) : 0) |
				(CheckH(image, x + 11, x + 21, 20) ? (1 << 1) : 0) |
				(CheckH(image, x + 11, x + 21, 44) ? (1 << 2) : 0) |
				(CheckV(image, x + 10, 48, 62) ? (1 << 3) : 0) |
				(CheckH(image, x +  0, x + 10, 44) ? (1 << 4) : 0) |
				(CheckH(image, x +  0, x + 10, 20) ? (1 << 5) : 0) |
				(CheckV(image, x + 10, 24, 40) ? (1 << 6) : 0);
			return segments switch {
				0b0111111 => 0,
				0b0000110 => 1,
				0b1011011 => 2,
				0b1001111 => 3,
				0b1100110 => 4,
				0b1101101 => 5,
				0b1111101 => 6,
				0b0000111 => 7,
				0b1111111 => 8,
				0b1101111 => 9,
				_ => throw new ArgumentException($"Couldn't read pattern: {segments:x}")
			};
		}
	}

	public record ReadData(TimeSpan Time) : ComponentReadData(new Point(0, 0));
}
