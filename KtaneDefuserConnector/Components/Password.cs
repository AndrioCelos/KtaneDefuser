using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace KtaneDefuserConnector.Components;
public class Password : ComponentReader<Password.ReadData> {
	public override string Name => "Password";
	protected internal override ComponentFrameType FrameType => ComponentFrameType.Solvable;

	private static readonly Dictionary<BitVector32, char> charPatterns = new() {
		{ new(0b0100101001011110100100110), 'A' },
		{ new(0b0011101001001110100100111), 'B' },
		{ new(0b0011001001000010100100110), 'C' },
		{ new(0b0011101001010010100100111), 'D' },
		{ new(0b0011100001001110000100111), 'E' },
		{ new(0b0000100001001110000100111), 'F' },
		{ new(0b0011001001011010000101110), 'G' },
		{ new(0b0100101001011110100101001), 'H' },
		{ new(0b0000100001000010000100001), 'I' },
		{ new(0b0011001001010000100001000), 'J' },
		{ new(0b0100100101000110010101001), 'K' },
		{ new(0b0011100001000010000100001), 'L' },
		{ new(0b1000110001101011101110001), 'M' },
		{ new(0b1000111001101011001110001), 'N' },
		{ new(0b0011001001010010100100110), 'O' },
		{ new(0b0000100001001110100100111), 'P' },
		{ new(0b010000011001001010010100100110), 'Q' },
		{ new(0b0100100101001110100100111), 'R' },
		{ new(0b0011101000001100000101110), 'S' },
		{ new(0b0001000010000100001000111), 'T' },
		{ new(0b0011001001010010100101001), 'U' },
		{ new(0b0010001010010101000110001), 'V' },
		{ new(0b0101010101101011010110001), 'W' },
		{ new(0b1000101010001000101010001), 'X' },
		{ new(0b0010000100001000101010001), 'Y' },
		{ new(0b0011100001000100010000111), 'Z' }
	};

	protected internal override float IsModulePresent(Image<Rgba32> image) {
		// Password: look for the display in the correct Y range
		var count = 0f;
		var count2 = 0f;

		for (var y = 32; y < 224; y += 16) {
			for (var x = 24; x < 208; x += 4) {
				var pixel = image[x, y];
				var n = ImageUtils.ColorProximity(pixel, 165, 240, 10, 123, 205, 21, 60);
				if (y is > 80 and < 176)
					count += n;
				else
					count2 += n;
				count2 += Math.Max(0, count / 200 - count2 / 100);
			}
		}

		return Math.Min(1, Math.Max(0, count / 200 - count2 / 100));
	}

	protected internal override ReadData Process(Image<Rgba32> image, LightsState lightsState, ref Image<Rgba32>? debugImage) {
		static Rectangle GetLetterBounds(PixelAccessor<Rgba32> a, int x) {
			int top, bottom, left, right, misses;

			misses = 0;
			for (top = 55; top >= 0; top--) {
				var r = a.GetRowSpan(top);
				misses++;
				for (var dx = -24; dx < 24; dx++) {
					if (r[x + dx] is Rgba32 p && p.G < 96 && p.B < 24) {
						misses = 0;
						break;
					}
				}
				if (misses >= 4) break;
			}
			top += 4;

			misses = 0;
			for (bottom = 55; bottom < 110; bottom++) {
				var r = a.GetRowSpan(bottom);
				misses++;
				for (var dx = -24; dx < 24; dx++) {
					if (r[x + dx] is Rgba32 p && p.G < 96 && p.B < 24) {
						misses = 0;
						break;
					}
				}
				if (misses >= 4) break;
			}
			bottom -= 3;

			misses = 0;
			for (left = x; ; left--) {
				misses++;
				for (var y = top; y < bottom; y++) {
					if (left >= 0 && a.GetRowSpan(y)[left] is Rgba32 p && p.G < 96 && p.B < 24) {
						misses = 0;
						break;
					}
				}
				if (misses >= 4) break;
			}
			left += 4;

			for (right = x; ; right++) {
				misses++;
				for (var y = top; y < bottom; y++) {
					if (right < a.Width && a.GetRowSpan(y)[right] is Rgba32 p && p.G < 96 && p.B < 24) {
						misses = 0;
						break;
					}
				}
				if (misses >= 4) break;
			}
			right -= 3;

			return new(left, top, right - left, bottom - top);
		}

		var displayCorners = ImageUtils.FindCorners(image, new(16, 64, 224, 128), c => HsvColor.FromColor(c) is HsvColor hsv && hsv.H is >= 75 and < 120 && hsv.S >= 0.8f && hsv.V >= 0.5f, 12);
		using var displayImage = ImageUtils.PerspectiveUndistort(image, displayCorners, InterpolationMode.NearestNeighbour, new(256, 110));
		if (debugImage is not null) {
			debugImage.Mutate(c => c.DrawImage(displayImage, 1));
			ImageUtils.DebugDrawPoints(debugImage, displayCorners);
		}

		var chars = new char[5];
		var debugImage2 = debugImage;
		displayImage.ProcessPixelRows(a => {
			for (var i = 0; i < 5; i++) {
				var bounds = GetLetterBounds(a, 24 + 51 * i);
				debugImage2?.Mutate(p => p.Draw(Color.Red, 1, bounds));

				var charHeight = bounds.Height >= 45 ? 6 : 5;  // That pesky 'Q' making special cases again.
				var charWidth = (int) Math.Round(bounds.Width / (bounds.Height / (double) charHeight));
				var pixelHeight = (double) bounds.Height / charHeight;
				var baseX = (bounds.Width - pixelHeight * (charWidth - 1)) / 2;
				var pattern = new BitVector32();
				for (var y = 0; y < charHeight; y++) {
					var py = (int) Math.Round(bounds.Y + pixelHeight * (y + 0.5));
					var r = a.GetRowSpan(py);
					for (var x = 0; x < charWidth; x++) {
						var px = (int) Math.Round(bounds.X + baseX + pixelHeight * x);
						if (r[px].G < 96) {
							if (debugImage2 is not null) debugImage2[px, py] = Color.Red;
							pattern[1 << (y * 5 + x)] = true;
						} else
							if (debugImage2 is not null) debugImage2[px, py] = Color.White;
					}
				}
				chars[i] = charPatterns[pattern];
			}
		});

		return new(chars);
	}

	public record ReadData(char[] Display) {
		public override string ToString() => new(this.Display);
	}
}
