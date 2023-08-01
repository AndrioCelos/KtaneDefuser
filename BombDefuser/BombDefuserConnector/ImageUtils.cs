using System;
using System.Collections.Generic;
using System.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace BombDefuserConnector;
public static class ImageUtils {
	public static Image<Rgb24> PerspectiveUndistort(Image<Rgb24> originalImage, IReadOnlyList<Point> points, InterpolationMode interpolationMode)
		=> PerspectiveUndistort(originalImage, points, interpolationMode, new(256, 256));
	public static Image<Rgb24> PerspectiveUndistort(Image<Rgb24> originalImage, IReadOnlyList<Point> points, InterpolationMode interpolationMode, Size resolution) {
		var bitmap = new Image<Rgb24>(resolution.Width, resolution.Height);
		bitmap.ProcessPixelRows(p => {
			for (var y = 0; y < bitmap.Height; y++) {
				var row = p.GetRowSpan(y);

				var sx1 = (float) points[0].X + (points[2].X - points[0].X) * y / bitmap.Height;
				var sy1 = (float) points[0].Y + (points[2].Y - points[0].Y) * y / bitmap.Height;
				var sx2 = (float) points[1].X + (points[3].X - points[1].X) * y / bitmap.Height;
				var sy2 = (float) points[1].Y + (points[3].Y - points[1].Y) * y / bitmap.Height;

				for (var x = 0; x < bitmap.Width; x++) {
					var sx = sx1 + (sx2 - sx1) * x / bitmap.Width;
					var sy = sy1 + (sy2 - sy1) * x / bitmap.Width;
					if (interpolationMode == InterpolationMode.Bilinear) {
						var vx1Weight = Math.Ceiling(sx) - sx;
						var vx2Weight = sx - (Math.Ceiling(sx) - 1);
						var vy1Weight = Math.Ceiling(sy) - sy;
						var vy2Weight = sy - (Math.Ceiling(sy) - 1);
						var c11 = originalImage[(int) Math.Floor(sx), (int) Math.Floor(sy)];
						var c12 = originalImage[(int) Math.Floor(sx), (int) Math.Ceiling(sy)];
						var c21 = originalImage[(int) Math.Ceiling(sx), (int) Math.Floor(sy)];
						var c22 = originalImage[(int) Math.Ceiling(sx), (int) Math.Ceiling(sy)];
						row[x] = new(
							(byte) Math.Round(vy1Weight * (vx1Weight * c11.R + vx2Weight * c21.R) + vy2Weight * (vx1Weight * c12.R + vx2Weight * c22.R)),
							(byte) Math.Round(vy1Weight * (vx1Weight * c11.G + vx2Weight * c21.G) + vy2Weight * (vx1Weight * c12.G + vx2Weight * c22.G)),
							(byte) Math.Round(vy1Weight * (vx1Weight * c11.B + vx2Weight * c21.B) + vy2Weight * (vx1Weight * c12.B + vx2Weight * c22.B)));

					} else
						row[x] = originalImage[(int) Math.Round(sx), (int) Math.Round(sy)];
				}
			}
		});
		return bitmap;
	}

	public static bool IsModuleBack(Rgb24 color, LightsState lightsState)
		=> Math.Abs(color.R - 136) + Math.Abs(color.G - 138) + Math.Abs(color.B - 150) <= 90;
	public static bool IsModuleBack(HsvColor hsv, LightsState lightsState)
		=> hsv.H is >= 180 and < 255 && hsv.S is >= 0.025f and < 0.25f && hsv.V is >= 0.45f and < 0.75f;

	public static Point[]? FindCorners(Image<Rgb24> image, Rectangle bounds, Predicate<Rgb24> predicate, int continuitySize) {
		var points = new Point[4];
		var diagonalSweeps = new (Point start, Size dir, Size increment)[] {
			(new(bounds.Left, bounds.Top), new(-1, 1), new(1, 0)),
			(new(bounds.Right - 1, bounds.Top), new(1, 1), new(-1, 0)),
			(new(bounds.Left, bounds.Bottom - 1), new(1, 1), new(0, -1)),
			(new(bounds.Right - 1, bounds.Bottom - 1), new(-1, 1), new(0, -1)),
		};

		for (var n = 0; n < 4; n++) {
			var found = false;
			var (start, dir, increment) = diagonalSweeps[n];
			var diagonalPoint1 = start;
			var diagonalPoint2 = start;
			for (var i = 0; i < 256; i++) {
				var point = diagonalPoint1;
				for (var j = 0; j <= i; j++) {
					if (bounds.Contains(point) && predicate(image[point.X, point.Y])) {
						if (continuitySize > 0) {
							// Try to filter out outliers by requiring 12 out of 16 further pixels along the diagonal to also match.
							var pixelCount = 0;
							for (var k = 1; k <= 16; k++) {
								var point3 = point + (increment * 2 + dir) * k;
								if (bounds.Contains(point3) && predicate(image[point3.X, point3.Y]))
									pixelCount++;
							}
							if (pixelCount >= continuitySize)
								found = true;
						} else
							found = true;
						if (found)
							break;
					}
					point += dir;
				}
				if (found) {
					var point2 = diagonalPoint2;
					for (var j = 0; j <= i; j++) {
						if (bounds.Contains(point2) && predicate(image[point2.X, point2.Y])) {
							if (continuitySize > 0) {
								var pixelCount = 0;
								for (var k = 1; k <= 16; k++) {
									var point3 = point2 + (increment * 2 + dir) * k;
									if (bounds.Contains(point3) && predicate(image[point3.X, point3.Y]))
										pixelCount++;
								}
								if (pixelCount >= continuitySize)
									break;
							} else
								break;
						}
						point2 -= dir;
					}
					points[n] = new((point.X + point2.X) / 2, (point.Y + point2.Y) / 2);
					break;
				}
				diagonalPoint1 += increment;
				diagonalPoint2 += increment + dir;
			}
			if (!found)
				throw new ArgumentException($"Can't find corner {n}");
		}
		return points;
	}

	public static Rectangle FindEdges(Image<Rgb24> image, Rectangle rectangle, Predicate<Rgb24> predicate) {
		for (int edge = 0; edge < 2; edge++) {
			while (true) {
				var y = edge == 0 ? rectangle.Top : rectangle.Bottom - 1;
				var found = false;
				for (int x = rectangle.Left; x < rectangle.Right; x++) {
					if (predicate(image[x, y])) {
						found = true;
						break;
					}
				}
				if (found) break;
				if (edge == 0) rectangle.Y++;
				rectangle.Height--;
			}
		}
		for (int edge = 0; edge < 2; edge++) {
			while (true) {
				var x = edge == 0 ? rectangle.Left : rectangle.Right - 1;
				var found = false;
				for (int y = rectangle.Top; y < rectangle.Bottom; y++) {
					if (predicate(image[x, y])) {
						found = true;
						break;
					}
				}
				if (found) break;
				if (edge == 0) rectangle.X++;
				rectangle.Width--;
			}
		}
		return rectangle;
	}

	public static void DebugDrawPoints(Image image, Point[] points) {
		for (var i = 0; i < points.Length; i++) {
			image.Mutate(c => c.Fill((i % 4) switch { 0 => Color.Red, 1 => Color.Yellow, 2 => Color.Lime, 3 => Color.RoyalBlue, _ => Color.Magenta }, new EllipsePolygon(points[i], 2)));
		}
	}

	public static int ColorProximity(Rgb24 color, int refR, int refG, int refB, int scale)
		=> Math.Max(0, scale - Math.Abs(color.R - refR) - Math.Abs(color.G - refG) - Math.Abs(color.B - refB));

	public static int ColorProximity(Rgb24 color, int refR1, int refG1, int refB1, int refR2, int refG2, int refB2, int scale)
		=> Math.Max(0, scale - Math.Min(
			Math.Abs(color.R - refR1) + Math.Abs(color.G - refG1) + Math.Abs(color.B - refB1),
			Math.Abs(color.R - refR2) + Math.Abs(color.G - refG2) + Math.Abs(color.B - refB2)
		));

	public static void ColourCorrect(Image<Rgb24> image, LightsState lightsState) {
		if (lightsState == LightsState.On) return;

		// This will apply a linear function to each component of each pixel.
		// The linear function is defined by a gradient and intercent: y = m * x + c
		// The gradient (M) and y-intercept (C) are multiplied by 65536 because fixed-point math is faster than floating-point math.
		// 32768 is added to the intercepts for rounding.
		(int rM, int rC, int gM, int gC, int bM, int bC) = lightsState switch {
			LightsState.Buzz      => ( 358520, -744026 + 32768,  358520, -744026 + 32768,  358520, -744026 + 32768),
			LightsState.Off       => (1913651,  642253 + 32768, 1913651,  642253 + 32768, 1507328,  786432 + 32768),
			LightsState.Emergency => (  48545,  242726 + 32768,   90502, -405699 + 32768,   89902, -403298 + 32768),
			_                     => (  65536,       0 + 32768,   65536,       0 + 32768,   65536,       0 + 32768)
		};

		image.ProcessPixelRows(p => {
			for (var y = 0; y < image.Height; y++) {
				var row = p.GetRowSpan(y);
				for (var x = 0; x < image.Width; x++) {
					var color = image[x, y];
					int r = color.R, g = color.G, b = color.B;

					r = Math.Min(255, Math.Max(0, (r * rM + rC) >> 16));
					g = Math.Min(255, Math.Max(0, (g * gM + gC) >> 16));
					b = Math.Min(255, Math.Max(0, (b * bM + bC) >> 16));

					row[x] = new((byte) r, (byte) g, (byte) b);
				}
			}
		});
	}

	public static float CheckSimilarity(Image<Rgb24> subject, params Image<Rgba32>[] samples) {
		var size = samples.First().Size;
		int score = 0, total = 0;
		for (int y = 0; y < size.Height; y++) {
			for (int x = 0; x < size.Width; x++) {
				var colour = subject[x, y];
				var maxIncrement = 0;
				var maxTotalIncrement = 0;
				foreach (var sample in samples) {
					var sampleColour = sample[x, y];
					if (sampleColour.A > 0) {
						var increment = Math.Max(127 - Math.Abs(colour.R - sampleColour.R) - Math.Abs(colour.G - sampleColour.G) - Math.Abs(colour.B - sampleColour.B), 0) * sampleColour.A;
						var totalIncrement = 127 * sampleColour.A;
						if (increment > maxIncrement) maxIncrement = increment;
						if (totalIncrement > maxTotalIncrement) maxTotalIncrement = totalIncrement;
					}
				}
				score += maxIncrement;
				total += maxTotalIncrement;
			}
		}
		return (float) score / total;
	}

	public static float CheckForNeedyFrame(Image<Rgb24> image) {
		var yellowPixels = 0;
		for (int y = 0; y < image.Height * 80 / 256; y++) {
			for (int x = 0; x < image.Width; x++) {
				var hsvColor = HsvColor.FromColor(image[x, y]);
				if (hsvColor.H is >= 30 and <= 60 && hsvColor.S >= 0.5f && hsvColor.V >= 0.4f)  // Include the stripboard on Capacitor Discharge.
					yellowPixels++;
			}
		}
		return yellowPixels / 1000f;
	}

	public static bool CheckForBlankComponent(Image<Rgb24> image) {
		var orangePixels = 0;
		for (int y = 0; y < image.Height; y++) {
			for (int x = 0; x < image.Width; x++) {
				var hsvColor = HsvColor.FromColor(image[x, y]);
				if (hsvColor.H is >= 15 and <= 25 && hsvColor.S is >= 0.6f and <= 0.8f && hsvColor.V >= 0.35f)
					orangePixels++;
			}
		}
		return orangePixels >= 40000;
	}

	public static ModuleLightState GetLightState(Image<Rgb24> image, params Point[] points) => GetLightState(image, (IReadOnlyList<Point>) points);
	public static ModuleLightState GetLightState(Image<Rgb24> image, IReadOnlyList<Point> points) {
		var x = (int) Math.Round(points[1].X + (points[2].X - points[1].X) * 0.1015625);
		var y = (int) Math.Round(points[1].Y + (points[2].Y - points[1].Y) * 0.1015625);
		var hsv = HsvColor.FromColor(image[x, y]);
		return hsv.S >= 0.85f && hsv.H is >= 105 and <= 150
			? ModuleLightState.Solved
			: hsv.S >= 0.65f && hsv.H is >= 330 or <= 30
			? ModuleLightState.Strike
			: ModuleLightState.Off;
	}
}
