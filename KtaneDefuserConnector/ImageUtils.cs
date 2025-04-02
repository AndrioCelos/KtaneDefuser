using System;
using System.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace KtaneDefuserConnector;
public static class ImageUtils {
	/// <summary>Returns an image drawn by stretching the specified quadrilateral area from another image.</summary>
	public static Image<Rgba32> PerspectiveUndistort(Image<Rgba32> originalImage, Quadrilateral quadrilateral, InterpolationMode interpolationMode)
		=> PerspectiveUndistort(originalImage, quadrilateral, interpolationMode, new(256, 256));
	/// <summary>Returns an image drawn by stretching the specified quadrilateral area from another image.</summary>
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
	/// <param name="continuitySize">The number out of 16 further pixels along the inward diagonal that must also match.</param>
	public static Quadrilateral FindCorners(Image<Rgba32> image, Rectangle bounds, Predicate<Rgba32> predicate, int continuitySize)
		=> TryFindCorners(image, bounds, predicate, continuitySize, out var quadrilateral) ? quadrilateral : throw new ArgumentException("No area matching the specified condition was found.");
	/// <summary>Finds a point closest to each corner of the specified rectangle of the area meeting the specified predicate.</summary>
	/// <param name="continuitySize">The number out of 16 further pixels along the inward diagonal that must also match.</param>
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
					if (predicate(r[x])) {
						found = true;
						break;
					}
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
					if (predicate(accessor.GetRowSpan(y)[x])) {
						found = true;
						break;
					}
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
	// The linear function is defined by a gradient and y-intercent: y = m * x + c
	// The gradient (m) and intercept (c) are multiplied by 65536 because fixed-point math is faster than floating-point math.
	// The intercept is also offset by +32768 for rounding.
	public static Rgba32 ColourCorrect(Rgba32 pixel, LightsState lightsState) => lightsState switch {
		LightsState.Buzz      => new((byte) Math.Min(Math.Max(( 319305 * pixel.R +  429087) >> 16, 0), 255), (byte) Math.Min(Math.Max(( 315405 * pixel.G +  664756) >> 16, 0), 255), (byte) Math.Min(Math.Max(( 316774 * pixel.B +  663781) >> 16, 0), 255), pixel.A),
		LightsState.Off       => new((byte) Math.Min(Math.Max((2127280 * pixel.R +  497929) >> 16, 0), 255), (byte) Math.Min(Math.Max((1964778 * pixel.G +  715096) >> 16, 0), 255), (byte) Math.Min(Math.Max((1570129 * pixel.B +  740225) >> 16, 0), 255), pixel.A),
		LightsState.Emergency => new((byte) Math.Min(Math.Max((  53982 * pixel.R + -299830) >> 16, 0), 255), (byte) Math.Min(Math.Max((  84761 * pixel.G +  256655) >> 16, 0), 255), (byte) Math.Min(Math.Max((  85066 * pixel.B +  250049) >> 16, 0), 255), pixel.A),
		_ => pixel
	};
	public static Rgba32 ColourUncorrect(Rgba32 pixel, LightsState lightsState) => lightsState switch {
		LightsState.Buzz      => new((byte) Math.Min(Math.Max((  13118 * pixel.R +   -2642) >> 16, 0), 255), (byte) Math.Min(Math.Max((  13099 * pixel.G +  -26052) >> 16, 0), 255), (byte) Math.Min(Math.Max((  12964 * pixel.B +  -15923) >> 16, 0), 255), pixel.A),
		LightsState.Off       => new((byte) Math.Min(Math.Max((   1879 * pixel.R +   37662) >> 16, 0), 255), (byte) Math.Min(Math.Max((   1946 * pixel.G +   43646) >> 16, 0), 255), (byte) Math.Min(Math.Max((   2449 * pixel.B +   42723) >> 16, 0), 255), pixel.A),
		LightsState.Emergency => new((byte) Math.Min(Math.Max((  78034 * pixel.R +  647314) >> 16, 0), 255), (byte) Math.Min(Math.Max((  50172 * pixel.G +  -70498) >> 16, 0), 255), (byte) Math.Min(Math.Max((  49926 * pixel.B +  -57006) >> 16, 0), 255), pixel.A),
		_ => pixel
	};

	/// <summary>Returns a value indicating how similar the specified image is to any of the samples.</summary>
	public static float CheckSimilarity(Image<Rgba32> subject, params Image<Rgba32>[] samples) {
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

	/// <summary>Returns a value indicating how much the module in the specified image looks like a needy frame.</summary>
	[Obsolete("Being replaced with GetComponentFrame.")]
	public static float CheckForNeedyFrame(Image<Rgba32> image) {
		var yellowPixels = 0;
		for (int y = 0; y < image.Height * 80 / 256; y++) {
			for (int x = 0; x < image.Width; x++) {
				var hsvColor = HsvColor.FromColor(image[x, y]);
				if (hsvColor.H is >= 30 and <= 60 && hsvColor.S >= 0.5f && hsvColor.V >= 0.4f)  // Include the stripboard on Capacitor Discharge.
					yellowPixels++;
			}
		}
		return yellowPixels / 2000f;
	}

	public static ComponentFrameType GetComponentFrame(Image<Rgba32> image) {
		var rectangles = new Rectangle[] { new(0, 0, image.Width, image.Height / 4), new(0, 0, image.Width / 4, image.Height), new(0, image.Height * 3 / 4, image.Width, image.Height / 4), new(image.Width * 3 / 4, 0, image.Width / 4, image.Height) };
		var total = 0;
		var match = new int[3];
		image.ProcessPixelRows(p => {
			for (var i = 0; i < rectangles.Length; i++) {
				var rect = rectangles[i];
				for (var y = rect.Top; y < rect.Bottom; y++) {
					var r = p.GetRowSpan(y);
					for (var x = rect.Left; x < rect.Right; x++) {
						total++;

						var hsv = HsvColor.FromColor(r[x]);
						if (hsv.H is >= 180 and < 240 && hsv.S < 0.5f && hsv.V is >= 0.15f and < 0.5f)
							match[1]++;  // Needy background
						else if (i == 0 && hsv.H is >= 30 and < 60 && hsv.S >= 0.5f && hsv.V is >= 0.3f and < 0.75f)
							match[1]++;  // Needy yellow stripes
						else if (i == 0 && hsv.H >= 240 && hsv.S is >= 0.25f and < 0.5f && hsv.V is >= 0.1f and < 0.2f)
							match[1]++;  // Needy black stripes
						else if (hsv.H is >= 150 and < 240 && hsv.S < 0.5f && hsv.V >= 0.5f)
							match[0]++;  // Solvable background
						else if (hsv.S < 0.25f && hsv.V < 0.2f)
							match[2]++;  // Timer background
						else if (hsv.S < 0.25f && hsv.V < 0.2f)
							match[2]++;  // Timer background
						else if (hsv.H < 60 && hsv.S >= 0.5f && hsv.V < 0.2f)
							match[2] += 2;  // Timer display background
					}
				}
			}
		});
		return match[2] >= total / 4 && match[2] >= match[1] && match[2] >= match[0] ? ComponentFrameType.Timer
			: match[0] > match[1] && match[0] >= total / 4 ? ComponentFrameType.Solvable
			: match[1] > match[0] && match[1] >= total / 5 ? ComponentFrameType.Needy
			: ComponentFrameType.Unknown;
	}

	/// <summary>Returns a value indicating whether the module in the specified image looks like a blank component.</summary>
	public static bool CheckForBlankComponent(Image<Rgba32> image) {
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

	/// <summary>Reads the light state of the module in the specified quadrilateral area.</summary>
	public static ModuleLightState GetLightState(Image<Rgba32> image, Quadrilateral points) {
		var x = (int) Math.Round(points.TopRight.X + (points.BottomLeft.X - points.TopRight.X) * 0.1015625);
		var y = (int) Math.Round(points.TopRight.Y + (points.BottomLeft.Y - points.TopRight.Y) * 0.1015625);
		var hsv = HsvColor.FromColor(image[x, y]);
		return hsv.S >= 0.85f && hsv.H is >= 105 and <= 150 && hsv.V >= 0.5f
			? ModuleLightState.Solved
			: hsv.S >= 0.65f && hsv.H is >= 330 or <= 30 && hsv.V >= 0.75f
			? ModuleLightState.Strike
			: ModuleLightState.Off;
	}

	private static readonly Rectangle[] lightsStateSearchRects = [new(96, 48, 16, 16), new(960, 48, 16, 16), new(1748, 236, 16, 16)];
	private static readonly int[] lightsStateSearchTolerances = [0x20000, 0x20000, 0x8000, 0x30000];
	private static readonly Rgb48[,] lightsStateSearchColours = new Rgb48[,] {
		{
			new(0x1c08, 0x19e9, 0x17a3),
			new(0x24eb, 0x2364, 0x206f),
			new(0x244f, 0x2106, 0x1e3e),
		}, {
			new(0x0900, 0x093e, 0x0911),
			new(0x1c98, 0x1733, 0x0ee0),
			new(0x1aeb, 0x1491, 0x0c8d),
		}, {
			new(0x060e, 0x0800, 0x0a05),
			new(0x05f2, 0x0700, 0x08f4),
			new(0x0600, 0x06c3, 0x0800),
		}, {
			new(0x7e80, 0x1855, 0x1573),
			new(0x7b1d, 0x220f, 0x1f74),
			new(0x6122, 0x20bf, 0x1df0),
		}
	};
	public static LightsState GetLightsState(Image<Rgba32> image) {
		var result = LightsState.On;
		image.ProcessPixelRows(a => {
			for (var i = 1; i < 4; i++) {
				for (var rectIndex = 0; rectIndex < 3; rectIndex++) {
					var rect = lightsStateSearchRects[rectIndex];
					var dist = 0;
					var refColour = lightsStateSearchColours[i, rectIndex];
					for (var y = rect.Top; y < rect.Bottom; y++) {
						var r = a.GetRowSpan(y);
						for (var x = rect.Left; x < rect.Right; x++) {
							var p = r[x];
							dist += Math.Abs((p.R << 8) - refColour.R) + Math.Abs((p.G << 8) - refColour.G) + Math.Abs((p.B << 8) - refColour.B);
						}
					}
					if (dist < lightsStateSearchTolerances[i]) {
						result = (LightsState) i;
						return;
					}
				}
			}
		});
		return result;
	}
}
