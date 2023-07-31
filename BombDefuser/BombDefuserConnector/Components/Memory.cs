using System;
using System.Collections.Generic;
using System.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace BombDefuserConnector.Components;
public class Memory : ComponentProcessor<Memory.ReadData> {
	private static readonly Image<Rgb24>[] numberBitmaps = Enumerable.Range(1, 4).Select(n => Image.Load<Rgb24>((byte[]) Properties.Resources.ResourceManager.GetObject($@"MemoryB{n}")!)).ToArray();
	private static readonly Image<Rgb24>[] displayBitmaps = Enumerable.Range(1, 4).Select(n => Image.Load<Rgb24>((byte[]) Properties.Resources.ResourceManager.GetObject($@"MemoryD{n}")!)).ToArray();

	public override string Name => "Memory";
	protected internal override bool UsesNeedyFrame => false;

	protected internal override float IsModulePresent(Image<Rgb24> image) {
		var minDist = int.MaxValue;
		var referenceColour = new Rgb24(55, 95, 81);
		for (int x = image.Width / 4 - 10; x < image.Width / 4 + 10; x++) {
			for (int y = image.Height / 4 - 10; y < image.Height / 4 + 10; y++) {
				var pixel = image[x, y];
				var dist = Math.Abs(pixel.R - referenceColour.R) + Math.Abs(pixel.G - referenceColour.G) + Math.Abs(pixel.B - referenceColour.B);
				minDist = Math.Min(minDist, dist);
			}
		}

		// Keypad should be at about Y = 192
		var count = 0f;
		var referenceColour2 = new Rgb24(220, 196, 155);
		var referenceColour3 = new Rgb24(51, 46, 37);
		for (var x = 24; x < 172; x++) {
			var pixel = image[x, 192];
			var dist = pixel.R < 128
				? Math.Abs(pixel.R - referenceColour3.R) + Math.Abs(pixel.G - referenceColour3.G) + Math.Abs(pixel.B - referenceColour3.B)
				: Math.Abs(pixel.R - referenceColour2.R) + Math.Abs(pixel.G - referenceColour2.G) + Math.Abs(pixel.B - referenceColour2.B);
			count += Math.Max(0, 1 - dist / 20);
		}

		// And not at about Y = 128
		var count2 = 0f;
		var referenceColour4 = new Rgb24(170, 150, 120);
		for (var x = 24; x < 172; x++) {
			var pixel = image[x, 128];
			var dist = Math.Abs(pixel.R - referenceColour4.R) + Math.Abs(pixel.G - referenceColour4.G) + Math.Abs(pixel.B - referenceColour4.B);
			count2 += Math.Max(0, 1 - dist / 20);
		}

		return Math.Max(1 - (float) minDist / 50, 0) * 0.5f + Math.Max(0, count / 148 - count2 / 148) * 0.5f;
	}

	protected internal override ReadData Process(Image<Rgb24> image, ref Image<Rgb24>? debugBitmap) {
		bool isButtonBackP(Point point) => isButtonBack(image[point.X, point.Y]);
		bool isButtonBack(Rgb24 color) {
			var hsv = HsvColor.FromColor(color);
			return hsv.S >= 0.2f && hsv.H is >= 30 and <= 50 && hsv.V >= 0.6f;
		}
		bool isDisplayBack(Point point)
			=> isDisplayBack2(point) && (isDisplayBack2(new(point.X - 4, point.Y)) || isDisplayBack2(new(point.X + 4, point.Y)) || isDisplayBack2(new(point.X, point.Y - 4)) || isDisplayBack2(new(point.X, point.Y + 4)));
		bool isDisplayBack2(Point point)
			=> point.X is >= 0 and < 256 && point.Y is >= 0 and < 256 && isDisplayBackColor(image[point.X, point.Y]);
		bool isDisplayBackColor(Rgb24 color) {
			var hsv = HsvColor.FromColor(color);
			return hsv.H is >= 145 and <= 180 && hsv.S >= 0.35f && hsv.V >= 0.25f;
		}
		bool isDisplayTextColor(Rgb24 color)
			=> color.R >= 224 && color.G >= 224 && color.B >= 224;

		if (debugBitmap is not null) {
			debugBitmap.Mutate(c => c.Brightness(0.5f));
			for (var y = 0; y < image.Height; y++) {
				for (var x = 0; x < image.Width; x++) {
					var color = image[x, y];
					if (y >= 144 ? isButtonBack(color) : isDisplayBack(new(x, y)))
						debugBitmap[x, y] = color;
				}
			}
		}

		// Extract the keypad.
		var points = new Point[4];
		var diagonalSweeps = new (Point start, Size dir, Size increment)[] {
			(new(0, 128), new(-1, 1), new(1, 0)),
			(new(255, 128), new(1, 1), new(-1, 0)),
			(new(0, 255), new(1, 1), new(0, -1)),
			(new(255, 255), new(-1, 1), new(0, -1)),
		};

		for (var n = 0; n < 4; n++) {
			var found = false;
			var (start, dir, increment) = diagonalSweeps[n];
			var diagonalPoint1 = start;
			var diagonalPoint2 = start;
			for (var i = 0; i < 256; i++) {
				var point = diagonalPoint1;
				for (var j = 0; j <= i; j++) {
					if (point.Y >= 128 && isButtonBackP(point)) {
						found = true;
						break;
					}
					point += dir;
				}
				if (found) {
					var point2 = diagonalPoint2;
					for (var j = 0; j <= i; j++) {
						if (point2.Y >= 128 && isButtonBackP(point2))
							break;
						point2 -= dir;
					}
					points[n] = new((point.X + point2.X) / 2, (point.Y + point2.Y) / 2);
					debugBitmap?.Mutate(c => c.Fill(n switch { 0 => Color.Red, 1 => Color.Yellow, 2 => Color.Lime, _ => Color.RoyalBlue }, new EllipsePolygon(points[n], 3)));
					break;
				}
				diagonalPoint1 += increment;
				diagonalPoint2 += increment + dir;
			}
			if (!found)
				throw new ArgumentException($"Can't find keypad corner {n}");
		}

		// Extract the display.
		var displayPoints = new Point[4];
		var diagonalSweeps2 = new (Point start, Size dir, Size increment)[] {
							(new(0, 0), new(-1, 1), new(1, 0)),
							(new(255, 0), new(1, 1), new(-1, 0)),
							(new(0, 127), new(1, 1), new(0, -1)),
							(new(255, 127), new(-1, 1), new(0, -1)),
						};

		for (var n = 0; n < 4; n++) {
			var found = false;
			var (start, dir, increment) = diagonalSweeps2[n];
			var diagonalPoint1 = start;
			var diagonalPoint2 = start;
			for (var i = 0; i < 256; i++) {
				var point = diagonalPoint1;
				for (var j = 0; j <= i; j++) {
					if (isDisplayBack(point)) {
						found = true;
						break;
					}
					point += dir;
				}
				if (found) {
					var point2 = diagonalPoint2;
					for (var j = 0; j <= i; j++) {
						if (isDisplayBack(point2))
							break;
						point2 -= dir;
					}
					displayPoints[n] = new((point.X + point2.X) / 2, (point.Y + point2.Y) / 2);
					debugBitmap?.Mutate(c => c.Fill(n switch { 0 => Color.Red, 1 => Color.Yellow, 2 => Color.Lime, _ => Color.RoyalBlue }, new EllipsePolygon(points[n], 3)));
					break;
				}
				diagonalPoint1 += increment;
				diagonalPoint2 += increment + dir;
			}
			if (!found)
				throw new ArgumentException($"Can't find keypad corner {n}");
		}

		int matchImages(Image<Rgb24> reference, Image<Rgb24> sample, Point sampleLocation, Func<Rgb24, bool> colorKeySelector) {
			var matchingPixels = 0;
			for (var y = 0; y < reference.Height; y++) {
				for (var x = 0; x < reference.Width; x++) {
					if (colorKeySelector(reference[x, y]) == colorKeySelector(sample[x + sampleLocation.X, y + sampleLocation.Y]))
						matchingPixels++;
				}
			}
			return matchingPixels;
		}

		var keypadBitmap = ImageUtils.PerspectiveUndistort(image, points, InterpolationMode.Bilinear, new(304, 128));
		debugBitmap?.Mutate(c => c.Resize(512, 512, KnownResamplers.NearestNeighbor).DrawImage(keypadBitmap, 1));

		var matches = new List<(int pos, int label, int matchScore)>();
		var labels = new int[4];
		var totalMatchScore = 0;
		for (var p = 0; p < 4; p++) {
			for (var n = 0; n < 4; n++) {
				matches.Add((p, n + 1, matchImages(numberBitmaps[n], keypadBitmap, new(p * 80, 0), isButtonBack)));
			}
		}

		while (matches.Count > 0) {
			var topEntry = matches.MaxBy(e => e.matchScore);
			totalMatchScore += topEntry.matchScore;
			labels[topEntry.pos] = topEntry.label;
			matches.RemoveAll(e => e.pos == topEntry.pos || e.label == topEntry.label);
		}

		var displayBitmap = ImageUtils.PerspectiveUndistort(image, displayPoints, InterpolationMode.Bilinear, new(160, 128));
		debugBitmap?.Mutate(c => c.DrawImage(displayBitmap, new Point(512 - 160, 160), 1));

		var best = 0;
		var bestMatchScore = -1;
		for (var i = 1; i <= 4; i++) {
			var matchScore = matchImages(displayBitmaps[i - 1], displayBitmap, Point.Empty, isDisplayTextColor);
			if (matchScore > bestMatchScore) {
				best = i;
				bestMatchScore = matchScore;
			}
		}

		return new(best, labels);
	}

	public record ReadData(int Display, int[] Buttons) {
		public override string ToString() => $"{this.Display} XS {string.Join(' ', this.Buttons)}";
	}
}
