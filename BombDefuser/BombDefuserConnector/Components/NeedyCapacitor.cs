using System;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.ColorSpaces.Conversion;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace BombDefuserConnector.Components;
public class NeedyCapacitor : ComponentProcessor<NeedyCapacitor.ReadData> {
	public override string Name => "Needy Capacitor";
	protected internal override bool UsesNeedyFrame => true;

	protected internal override float IsModulePresent(Image<Rgb24> image) {
		// Look for the brown lever frame.
		var count = 0;
		image.ProcessPixelRows(a => {
			for (var y = 80; y < 256; y++) {
				var row = a.GetRowSpan(y);
				for (var x = 144; x < 240; x++) {
					var hsv = HsvColor.FromColor(row[x]);
					if (hsv.H is >= 15 and <= 60 && hsv.S is >= 0.15f and <= 0.5f)
						count++;
				}
			}
		});
		return count / 6000f;
	}
	protected internal override ReadData Process(Image<Rgb24> image, ref Image<Rgb24>? debugBitmap) {
		// Find the timer.
		var bezelCorners = ImageUtils.FindCorners(image, new(80, 16, 96, 64), c => HsvColor.FromColor(c) is HsvColor hsv && hsv.H <= 150 && hsv.S <= 0.25f && hsv.V is >= 0.3f and <= 0.75f, 6) ?? throw new ArgumentException("Can't find needy timer bezel corners");
		if (debugBitmap != null) ImageUtils.DebugDrawPoints(debugBitmap, bezelCorners);
		var left = Math.Min(bezelCorners[0].X, bezelCorners[2].X);
		var top = Math.Min(bezelCorners[0].Y, bezelCorners[1].Y);
		var right = Math.Max(bezelCorners[1].X, bezelCorners[3].X) + 1;
		var bottom = Math.Max(bezelCorners[2].Y, bezelCorners[3].Y) + 1;
		var bezelRectangle = new Rectangle(left, top, right - left, bottom - top);
		var displayCorners = ImageUtils.FindCorners(image, bezelRectangle, c => HsvColor.FromColor(c) is HsvColor hsv && hsv.H is >= 345 or <= 15 && hsv.V <= 0.2f, 6) ?? throw new ArgumentException("Can't find needy timer corners");
		if (debugBitmap != null) ImageUtils.DebugDrawPoints(debugBitmap, displayCorners);

		var displayImage = ImageUtils.PerspectiveUndistort(image, displayCorners, InterpolationMode.NearestNeighbour, new(128, 64));
		debugBitmap?.Mutate(p => p.DrawImage(displayImage, 1));

		bool checkRectangle(Image<Rgb24> image, Rectangle rectangle) {
			for (var dy = 0; dy < rectangle.Height; dy++) {
				for (var dx = 0; dx < rectangle.Width; dx++) {
					var p = image[rectangle.Left + dx, rectangle.Top + dy];
					if (p.R >= 128) return true;
				}
			}
			return false;
		}
		int? readDigit(Image<Rgb24> image, int x) {
			var segments =
				(checkRectangle(image, new(x +  0,  0, 1, 16)) ? (1 << 0) : 0) |
				(checkRectangle(image, new(x +  0, 18, 16, 1)) ? (1 << 1) : 0) |
				(checkRectangle(image, new(x +  0, 46, 16, 1)) ? (1 << 2) : 0) |
				(checkRectangle(image, new(x +  0, 48, 1, 16)) ? (1 << 3) : 0) |
				(checkRectangle(image, new(x - 16, 46, 16, 1)) ? (1 << 4) : 0) |
				(checkRectangle(image, new(x - 16, 18, 16, 1)) ? (1 << 5) : 0) |
				(checkRectangle(image, new(x +  0, 25, 1, 16)) ? (1 << 6) : 0);
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
				0 => null,
				_ => throw new ArgumentException($"Couldn't read pattern: {segments:x}")
			};
		}

		var d0 = readDigit(displayImage, 88);
		var d1 = readDigit(displayImage, 42);
		return d0 is null && d1 is null
			? new(null)
			: d0 is not null
			? new(d1 is not null ? d0 + d1 * 10 : d0)
			: throw new ArgumentException("Can't read needy timer");
	}

	public record ReadData(int? Time);
}
