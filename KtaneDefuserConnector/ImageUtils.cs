using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace KtaneDefuserConnector;

public static class ImageUtils {
	public const int StandardResolution = 256;
	public const int ReferenceScreenWidth = 1920;
	public const int ReferenceScreenHeight = 1080;

	/// <summary>Returns an image drawn by stretching the specified quadrilateral area from another image.</summary>
	public static Image<Rgba32> PerspectiveUndistort(Image<Rgba32> originalImage, Quadrilateral quadrilateral, InterpolationMode interpolationMode)
		=> PerspectiveUndistort(originalImage, quadrilateral, interpolationMode, new(StandardResolution, StandardResolution));

	/// <summary>Returns an image drawn by stretching the specified quadrilateral area from another image.</summary>
	[SuppressMessage("ReSharper", "PossibleLossOfFraction")]
	public static Image<Rgba32> PerspectiveUndistort(Image<Rgba32> originalImage, Quadrilateral quadrilateral, InterpolationMode interpolationMode, Size resolution) {
		var bitmap = new Image<Rgba32>(resolution.Width, resolution.Height);
		bitmap.ProcessPixelRows(p => {
			for (var y = 0; y < bitmap.Height; y++) {
				var row = p.GetRowSpan(y);

				var sx1 = (float) quadrilateral.TopLeft.X + (quadrilateral.BottomLeft.X - quadrilateral.TopLeft.X) * y / bitmap.Height;
				var sy1 = (float) quadrilateral.TopLeft.Y + (quadrilateral.BottomLeft.Y - quadrilateral.TopLeft.Y) * y / bitmap.Height;
				var sx2 = (float) quadrilateral.TopRight.X + (quadrilateral.BottomRight.X - quadrilateral.TopRight.X) * y / bitmap.Height;
				var sy2 = (float) quadrilateral.TopRight.Y + (quadrilateral.BottomRight.Y - quadrilateral.TopRight.Y) * y / bitmap.Height;

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

	public static bool IsModuleBack(Rgba32 color, LightsState lightsState)
		=> Math.Abs(color.R - 136) + Math.Abs(color.G - 138) + Math.Abs(color.B - 150) <= 90;

	public static bool IsModuleBack(HsvColor hsv, LightsState lightsState)
		=> hsv.H is >= 180 and < 255 && hsv.S is >= 0.025f and < 0.25f && hsv.V is >= 0.45f and < 0.75f;

	/// <summary>Finds a point closest to each corner of the specified rectangle of the area meeting the specified predicate.</summary>
	/// <param name="image">The image to search.</param>
	/// <param name="bounds">The bounds within the image to search within.</param>
	/// <param name="predicate">The condition to search for.</param>
	/// <param name="continuitySize">The number out of 16 further pixels along the inward diagonal that must also match.</param>
	public static Quadrilateral FindCorners(Image<Rgba32> image, Rectangle bounds, Predicate<Rgba32> predicate, int continuitySize)
		=> TryFindCorners(image, bounds, predicate, continuitySize, out var quadrilateral) ? quadrilateral : throw new ArgumentException("No area matching the specified condition was found.");

	/// <summary>Finds a point closest to each corner of the specified rectangle of the area meeting the specified predicate.</summary>
	/// <param name="image">The image to search.</param>
	/// <param name="bounds">The bounds within the image to search within.</param>
	/// <param name="predicate">The condition to search for.</param>
	/// <param name="continuitySize">The number out of 16 further pixels along the inward diagonal that must also match.</param>
	/// <param name="quadrilateral">Returns a quadrilateral consisting of the found corners.</param>
	public static bool TryFindCorners(Image<Rgba32> image, Rectangle bounds, Predicate<Rgba32> predicate, int continuitySize, out Quadrilateral quadrilateral) {
		quadrilateral = new();
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
			for (var i = 0; i < StandardResolution; i++) {
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

					quadrilateral[n] = new((point.X + point2.X) / 2, (point.Y + point2.Y) / 2);
					break;
				}

				diagonalPoint1 += increment;
				diagonalPoint2 += increment + dir;
			}

			if (!found) return false;
		}

		return true;
	}

	/// <summary>Finds a rectangle within the specified rectangle bounding pixels matching the specified predicate.</summary>
	public static bool TryFindEdges<TPixel>(Image<TPixel> image, Rectangle rectangle, Predicate<TPixel> predicate, out Rectangle result) where TPixel : unmanaged, IPixel<TPixel> {
		var success = false;
		var result2 = new Rectangle();
		image.ProcessPixelRows(a => {
			success = TryFindEdges(a, rectangle, predicate, out var result);
			result2 = result;
		});
		result = result2;
		return success;
	}

	/// <summary>Finds a rectangle within the specified rectangle bounding pixels matching the specified predicate.</summary>
	public static bool TryFindEdges<TPixel>(PixelAccessor<TPixel> accessor, Rectangle rectangle, Predicate<TPixel> predicate, out Rectangle result) where TPixel : unmanaged, IPixel<TPixel> {
		if (rectangle.Height <= 0 || rectangle.Width <= 0) {
			result = rectangle;
			return false;
		}

		for (var edge = 0; edge < 2; edge++) {
			while (true) {
				var r = accessor.GetRowSpan(edge == 0 ? rectangle.Top : rectangle.Bottom - 1);
				var found = false;
				for (var x = rectangle.Left; x < rectangle.Right; x++) {
					if (!predicate(r[x])) continue;
					found = true;
					break;
				}

				if (found) break;
				if (edge == 0) rectangle.Y++;
				rectangle.Height--;
				if (rectangle.Height == 0) {
					result = rectangle;
					return false;
				}
			}
		}

		for (var edge = 0; edge < 2; edge++) {
			while (true) {
				var x = edge == 0 ? rectangle.Left : rectangle.Right - 1;
				var found = false;
				for (var y = rectangle.Top; y < rectangle.Bottom; y++) {
					if (!predicate(accessor.GetRowSpan(y)[x])) continue;
					found = true;
					break;
				}

				if (found) break;
				if (edge == 0) rectangle.X++;
				rectangle.Width--;
			}
		}

		result = rectangle;
		return true;
	}

	/// <summary>Finds a rectangle within the specified rectangle bounding pixels matching the specified predicate.</summary>
	public static Rectangle FindEdges<TPixel>(Image<TPixel> image, Rectangle rectangle, Predicate<TPixel> predicate) where TPixel : unmanaged, IPixel<TPixel>
		=> TryFindEdges(image, rectangle, predicate, out var result) ? result : throw new ArgumentException("No area matching the specified condition was found.");

	/// <summary>Finds a rectangle within the specified rectangle bounding pixels matching the specified predicate.</summary>
	public static Rectangle FindEdges<TPixel>(PixelAccessor<TPixel> accessor, Rectangle rectangle, Predicate<TPixel> predicate) where TPixel : unmanaged, IPixel<TPixel>
		=> TryFindEdges(accessor, rectangle, predicate, out var result) ? result : throw new ArgumentException("No area matching the specified condition was found.");

	public static void DebugDrawPoints(this Image image, Quadrilateral quadrilateral) {
		image.Mutate(c => c
			.Fill(Color.Red, new EllipsePolygon(quadrilateral.TopLeft, 2))
			.Fill(Color.Yellow, new EllipsePolygon(quadrilateral.TopRight, 2))
			.Fill(Color.Lime, new EllipsePolygon(quadrilateral.BottomLeft, 2))
			.Fill(Color.RoyalBlue, new EllipsePolygon(quadrilateral.BottomRight, 2)));
	}

	/// <summary>Returns <paramref name="scale"/> minus the distance in RGB coordinates between the specified colours.</summary>
	public static int ColorProximity(Rgba32 color, int refR, int refG, int refB, int scale)
		=> Math.Max(0, scale - Math.Abs(color.R - refR) - Math.Abs(color.G - refG) - Math.Abs(color.B - refB));

	/// <summary>Returns <paramref name="scale"/> minus the shorter of the distances in RGB coordinates between the specified colour and two reference colours.</summary>
	public static int ColorProximity(Rgba32 color, int refR1, int refG1, int refB1, int refR2, int refG2, int refB2, int scale)
		=> Math.Max(0, scale - Math.Min(
			Math.Abs(color.R - refR1) + Math.Abs(color.G - refG1) + Math.Abs(color.B - refB1),
			Math.Abs(color.R - refR2) + Math.Abs(color.G - refG2) + Math.Abs(color.B - refB2)
		));

	/// <summary>Corrects the colours of an image with the specified lights state to approximate <see cref="LightsState.On"/> colours.</summary>
	public static void ColourCorrect(this Image<Rgba32> image, LightsState lightsState) => ColourCorrect(image, lightsState, image.Bounds);

	/// <summary>Corrects the colours of the specified area of an image with the specified lights state to approximate <see cref="LightsState.On"/> colours.</summary>
	public static void ColourCorrect(this Image<Rgba32> image, LightsState lightsState, Rectangle rectangle) {
		if (lightsState == LightsState.On) return;
		image.ProcessPixelRows(a => {
			for (var y = rectangle.Top; y < rectangle.Bottom; y++) {
				var row = a.GetRowSpan(y);
				for (var x = rectangle.Left; x < rectangle.Right; x++) {
					row[x] = ColourCorrect(row[x], lightsState);
				}
			}
		});
	}

	/// <summary>Alters the colours of an image taken under <see cref="LightsState.On"/> to simulate the specified lights state.</summary>
	public static void ColourUncorrect(this Image<Rgba32> image, LightsState lightsState) {
		if (lightsState == LightsState.On) return;
		image.ProcessPixelRows(a => {
			for (var y = 0; y < a.Height; y++) {
				var row = a.GetRowSpan(y);
				for (var x = 0; x < a.Width; x++) {
					var pixel = ColourUncorrect(row[x], lightsState);
					row[x] = pixel;
				}
			}
		});
	}

	// The following lines were calculated using a linear least squares regression on sample images.
	// The linear function is defined by a gradient and y-intercept: y = m * x + c
	// The gradient (m) and intercept (c) are multiplied by 65536 because fixed-point math is faster than floating-point math.
	// The intercept is also offset by +32768 for rounding.
	public static Rgba32 ColourCorrect(Rgba32 pixel, LightsState lightsState) => lightsState switch {
		LightsState.Buzz => new((byte) Math.Clamp((319305 * pixel.R + 429087) >> 16, 0, 255), (byte) Math.Clamp((315405 * pixel.G + 664756) >> 16, 0, 255),
			(byte) Math.Clamp((316774 * pixel.B + 663781) >> 16, 0, 255), pixel.A),
		LightsState.Off => new((byte) Math.Clamp((2127280 * pixel.R + 497929) >> 16, 0, 255), (byte) Math.Clamp((1964778 * pixel.G + 715096) >> 16, 0, 255),
			(byte) Math.Clamp((1570129 * pixel.B + 740225) >> 16, 0, 255), pixel.A),
		LightsState.Emergency => new((byte) Math.Clamp((53982 * pixel.R - 299830) >> 16, 0, 255), (byte) Math.Clamp((84761 * pixel.G + 256655) >> 16, 0, 255),
			(byte) Math.Clamp((85066 * pixel.B + 250049) >> 16, 0, 255), pixel.A),
		_ => pixel
	};

	public static Rgba32 ColourUncorrect(Rgba32 pixel, LightsState lightsState) => lightsState switch {
		LightsState.Buzz => new((byte) Math.Clamp((13118 * pixel.R - 2642) >> 16, 0, 255), (byte) Math.Clamp((13099 * pixel.G - 26052) >> 16, 0, 255),
			(byte) Math.Clamp((12964 * pixel.B - 15923) >> 16, 0, 255), pixel.A),
		LightsState.Off => new((byte) Math.Clamp((1879 * pixel.R + 37662) >> 16, 0, 255), (byte) Math.Clamp((1946 * pixel.G + 43646) >> 16, 0, 255),
			(byte) Math.Clamp((2449 * pixel.B + 42723) >> 16, 0, 255), pixel.A),
		LightsState.Emergency => new((byte) Math.Clamp((78034 * pixel.R + 647314) >> 16, 0, 255), (byte) Math.Clamp((50172 * pixel.G - 70498) >> 16, 0, 255),
			(byte) Math.Clamp((49926 * pixel.B - 57006) >> 16, 0, 255), pixel.A),
		_ => pixel
	};

	/// <summary>Returns a value indicating how similar the specified image is to any of the samples.</summary>
	public static float CheckSimilarity(Image<Rgba32> subject, params Image<Rgba32>[] samples) {
		var size = samples.First().Size;
		int score = 0, total = 0;
		for (var y = 0; y < size.Height; y++) {
			for (var x = 0; x < size.Width; x++) {
				var colour = subject[x, y];
				var maxIncrement = 0;
				var maxTotalIncrement = 0;
				foreach (var sample in samples) {
					var sampleColour = sample[x, y];
					if (sampleColour.A <= 0) continue;
					var increment = Math.Max(127 - Math.Abs(colour.R - sampleColour.R) - Math.Abs(colour.G - sampleColour.G) - Math.Abs(colour.B - sampleColour.B), 0) * sampleColour.A;
					var totalIncrement = 127 * sampleColour.A;
					if (increment > maxIncrement) maxIncrement = increment;
					if (totalIncrement > maxTotalIncrement) maxTotalIncrement = totalIncrement;
				}

				score += maxIncrement;
				total += maxTotalIncrement;
			}
		}

		return (float) score / total;
	}

	private static readonly Rectangle[] LightsStateSearchRects = [new(96, 48, 16, 16), new(1856, 192, 16, 16), new(48, 1024, 16, 16), new(1856, 944, 16, 16)];
	private static readonly int[] LightsStateSearchTolerances = [10, 4, 4, 20];
	private static readonly Rgb24[,] LightsStateSearchColours = new Rgb24[,] {
		{
			new( 28,  25,  23),
			new( 33,  30,  27),
			new(214, 151, 119),
			new( 80,  59,  47)
		}, {
			new(  9,   9,   9),
			new( 24,  18,  11),
			new( 99,  57,  27),
			new( 51,  30,  15)
		}, {
			new(  6,   8,  10),
			new(  5,   5,   6),
			new(  7,   6,   7),
			new(  7,   6,   6)
		}, {
			new(126,  24,  21),
			new( 81,  30,  27),
			new(249, 152, 119),
			new(168,  59,  48)
		}
	};

	public static LightsState GetLightsState(Image<Rgba32> image) {
		LightsState? result = null;
		image.ProcessPixelRows(a => {
			// Vanilla bomb: look for colours around the edges of the screen, excluding the desktop camera control indicators.
			for (var rectIndex = 0; rectIndex < 4; rectIndex++) {
				int on = 0, buzz = 0, off = 0, emergency = 0, black = 0;
				var rect = LightsStateSearchRects[rectIndex];
				var row = a.GetRowSpan(rect.Y * a.Height / ReferenceScreenHeight);
				foreach (var x in a.Width.MapRange(rect.Left, rect.Right, originalSize: ReferenceScreenWidth)) {
					var p = row[x];
					if (p is { R: 0, G: 0, B: 0 }) {
						black++;
						continue;
					}

					int i;
					for (i = 0; i < 4; i++) {
						var c = LightsStateSearchColours[i, rectIndex];
						var d = Math.Abs(p.R - c.R) + Math.Abs(p.G - c.G) + Math.Abs(p.B - c.B);
						if (d <= LightsStateSearchTolerances[i]) break;
					}

					switch (i) {
						case 0: on++; break;
						case 1: buzz++; break;
						case 2: off++; break;
						case 3: emergency++; break;
					}
				}

				result = on >= 12 ? LightsState.On : buzz >= 12 ? LightsState.Buzz : off >= 12 ? LightsState.Off : emergency >= 12 ? LightsState.Emergency : black >= 12 ? LightsState.Black : null;
				if (result is not null) return;
			}
			
			// Centurion: identify by the casing dividers with the zoom level at +2.
			{
				int on = 0, buzz = 0, off = 0, emergency = 0;
				var row2 = a.GetRowSpan(685 * a.Height / ReferenceScreenHeight);
				foreach (var x in a.Width.MapRange(844, 1149, originalSize: ReferenceScreenWidth)) {
					var hsv = HsvColor.FromColor(row2[x]);
					switch (hsv) {
						case { H: <= 60, S: < 0.15f, V: >= 0.50f and < 0.90f }: on++; break;
						case { H: <= 60, S: < 0.15f, V: >= 0.08f and < 0.20f }: buzz++; break;
						case { H: <= 240, S: < 0.30f, V: >= 0.01f and < 0.05f }: off++; break;
						case { H: <= 15, S: >= 0.40f and < 0.50f, V: >= 0.65f }: emergency++; break;
					}
				}

				result = on >= 12 ? LightsState.On : buzz >= 12 ? LightsState.Buzz : off >= 12 ? LightsState.Off : emergency >= 12 ? LightsState.Emergency : null;
			}
		});
		return result ?? LightsState.On;
	}

	private static bool IsBombCasing(Rgba32 pixel) => HsvColor.FromColor(pixel) is { S: < 0.12f, V: >= 0.33f };

	public static IEnumerable<Rectangle> GetCenturionTopWidgetBounds(Image<Rgba32> image, Rectangle baseRectangle) {
		for (var line = 0; line < 2; line++) {
			var lineY = baseRectangle.Top + baseRectangle.Height * (1 + line * 2) / 4;
			var x = baseRectangle.Left;
			while (x < baseRectangle.Right) {
				if (!IsBombCasing(image[x, lineY])) {
					int x1 = x, y1 = lineY, y2 = lineY;
					while (x < baseRectangle.Right) {
						while (y1 > baseRectangle.Top && !IsBombCasing(image[x, y1 - 1]))
							y1--;
						while (y2 < baseRectangle.Bottom && !IsBombCasing(image[x, y2 + 1]))
							y2++;
						if (Enumerable.Range(y1, y2 - y1).All(y => IsBombCasing(image[x, y]))) break;
						x++;
					}

					var width = x - x1;
					var height = y2 - y1;
					if (width >= 30 && height >= 30) {
						if (width >= height * 3) {
							// This is likely multiple widgets, so separate them.
							var numRectangles = (int) Math.Round((double) width / height / 2.25);
							var width2 = width / numRectangles;
							for (var i = 0; i < numRectangles; i++)
								yield return new(x1 + width * i / numRectangles, y1, width2, height);
						} else {
							yield return new(x1, y1, width, height);
						}
					}
				}

				x++;
			}
		}
	}

	public static Rectangle GetCenturionSideWidgetBaseRectangle(Image<Rgba32> image, int top, int height) {
		var result = new Rectangle();
		image.ProcessPixelRows(a => {
			var left = 640 * a.Width / ReferenceScreenWidth;
			var right = 1280 * a.Width / ReferenceScreenWidth;

			int leftSum = 0, rightSum = 0;
			for (var dy = 0; dy < height; dy++) {
				var row = a.GetRowSpan(top + dy);
				int outLeft2 = left, outRight2 = right;
				var matches = 0;
				for (var x = left; x < right; x++) {
					if (IsBombBacking(row[x])) {
						matches++;
						if (matches >= 8) break;
					} else {
						matches = 0;
						outLeft2 = x;
					}
				}
				matches = 0;
				for (var x = right; x > left; x--) {
					if (IsBombBacking(row[x])) {
						matches++;
						if (matches >= 8) break;
					} else {
						matches = 0;
						outRight2 = x;
					}
				}

				leftSum += outLeft2;
				rightSum += outRight2;
			}
			
			result = new(leftSum / height + 1, top, (rightSum - leftSum) / height, height);
			return;

			static bool IsBombBacking(Rgba32 p) => HsvColor.FromColor(p) is { S: < 0.11f, V: >= 0.35f and < 0.98f };
		});
		return result;
	}

	public static IEnumerable<Rectangle> GetCenturionSideWidgetBounds(Image<Rgba32> image, Rectangle baseRectangle) {
		for (var line = 0; line < 2; line++) {
			var lineX = baseRectangle.Left + baseRectangle.Width * (1 + line * 2) / 4;
			var y = baseRectangle.Top;
			while (y < baseRectangle.Bottom) {
				if (!IsBombCasing(image[lineX, y])) {
					int y1 = y, x1 = lineX, x2 = lineX;
					while (y < baseRectangle.Bottom) {
						while (x1 > baseRectangle.Left && !IsBombCasing(image[x1 - 1, y]))
							x1--;
						while (x2 < baseRectangle.Right && !IsBombCasing(image[x2 + 1, y]))
							x2++;
						if (Enumerable.Range(x1, x2 - x1).All(x => IsBombCasing(image[x, y]))) break;
						y++;
					}

					var width = x2 - x1;
					var height = y - y1;
					if (width >= 30 && height >= 30) {
						if (height >= width * 3) {
							// This is likely multiple widgets, so separate them.
							var numRectangles = (int) Math.Round((double) height / width / 2.25);
							var height2 = height / numRectangles;
							for (var i = 0; i < numRectangles; i++)
								yield return new(x1, y1 + height * i / numRectangles, width, height2);
						} else {
							yield return new(x1, y1, width, height);
						}
					}
				}

				y++;
			}
		}
	}

	public static bool IsCenturion(Image<Rgba32> image) {
		if (image.Height == 1) return false;
		// Search an area of the table that's covered by the Centurion, but not the vanilla bomb case.
		// TODO: Currently only works with the default gameplay room.
		var result = false;
		image.ProcessPixelRows(p => {
			var row = p.GetRowSpan(p.Height * 840 / 1080);
			for (var x = p.Width / 2 - 200; x < p.Width / 2 + 200; x++) {
				if (row[x] is { R: 7 or 8, G: 7 or 8, B: 7 or 8 } // Lights off
					|| HsvColor.FromColor(row[x]) is { H: >= 15 and <= 30, S: >= 0.25f and < 0.5f }) // Lights on
					continue;
				result = true;
				return;
			}
		});
		return result;
	}

	public static Rectangle Map(this Image image, int x, int y, int width, int height, int originalSize = StandardResolution)
		=> new(x * image.Width / originalSize, y * image.Height / originalSize, width * image.Width / originalSize, height * image.Height / originalSize);
	public static Rectangle Map(this Image image, Rectangle rectangle, int originalSize = StandardResolution)
		=> new(rectangle.X * image.Width / originalSize, rectangle.Y * image.Height / originalSize, rectangle.Width * image.Width / originalSize, rectangle.Height * image.Height / originalSize);
	public static Rectangle Map(this PixelAccessor<Rgba32> pixelAccessor, int x, int y, int width, int height, int originalSize = StandardResolution)
		=> new(x * pixelAccessor.Width / originalSize, y * pixelAccessor.Height / originalSize, width * pixelAccessor.Width / originalSize, height * pixelAccessor.Height / originalSize);
	public static Rectangle Map(this PixelAccessor<Rgba32> pixelAccessor, Rectangle rectangle, int originalSize = StandardResolution)
		=> new(rectangle.X * pixelAccessor.Width / originalSize, rectangle.Y * pixelAccessor.Height / originalSize, rectangle.Width * pixelAccessor.Width / originalSize, rectangle.Height * pixelAccessor.Height / originalSize);
	public static IEnumerable<int> MapRange(this int newLength, int start, int end, int step = 1, int originalSize = StandardResolution) {
		var start2 = start * newLength / originalSize;
		var end2 = end * newLength / originalSize;
		for (var v = start2; v < end2; v += step)
			yield return v;
	}

	public static bool IsSelectionHighlight(this Rgba32 p, LightsState lightsState) => IsSelectionHighlight(HsvColor.FromColor(p), lightsState);
	public static bool IsSelectionHighlight(this HsvColor hsv, LightsState lightsState) => lightsState == LightsState.Emergency
		? hsv is { H: >= 5 and < 15, S: >= 0.80f, V: >= 0.80f }
		: hsv is { H: >= 5 and < 35, S: >= 0.60f, V: >= 0.60f }; 

	public static bool IsSelectionHighlightStrict(this Rgba32 p, LightsState lightsState) => IsSelectionHighlightStrict(HsvColor.FromColor(p), lightsState);
	public static bool IsSelectionHighlightStrict(this HsvColor hsv, LightsState lightsState) => lightsState is LightsState.Buzz or LightsState.Off
		? IsSelectionHighlight(hsv, lightsState)
		: hsv is { H: >= 6 and < 10, S: >= 0.95f, V: >= 0.95f };
}
