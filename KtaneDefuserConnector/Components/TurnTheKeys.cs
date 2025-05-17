using System;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace KtaneDefuserConnector.Components;
public class TurnTheKeys : ComponentReader<TurnTheKeys.ReadData> {
	public override string Name => "Turn the Keys";

	protected internal override ReadData Process(Image<Rgba32> image, LightsState lightsState, ref Image<Rgba32>? debugImage) {
		using var displayImage = GetNeedyDisplayImage(image, lightsState, debugImage);

		var d0 = ReadDigit(displayImage, 91);
		var d1 = ReadDigit(displayImage, 67);
		var d2 = ReadDigit(displayImage, 43);
		var d3 = ReadDigit(displayImage, 19);
		var priority = d0 + d1 * 10 + d2 * 100 + d3 * 1000;

		// Find the selection.
		Point point = default;
		image.ProcessPixelRows(p => {
			foreach (var y in image.Height.MapRange(104, 148, 8)) {
				var row = p.GetRowSpan(y);
				foreach (var x in image.Width.MapRange(24, 232, 2)) {
					if (HsvColor.FromColor(row[x]) is not { H: < 30, S: >= 0.65f, V: >= 0.5f }) continue;
					point = new(x, y);
					return;
				}
			}
		});

		return new(priority, false, false, point.X switch { 0 => null, < 128 => 0, _ => 1 });

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

	public record ReadData(int Priority, bool IsKey1Turned, bool IsKey2Turned, int? Cursor);
}
