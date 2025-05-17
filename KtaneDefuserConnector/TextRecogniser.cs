using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using JetBrains.Annotations;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace KtaneDefuserConnector;
/// <summary>Identifies images of text.</summary>
internal class TextRecogniser {
	internal static class Fonts {
		private static readonly FontCollection FontCollection = new();

		internal static readonly FontFamily Arial = SystemFonts.Get("Arial");
		internal static readonly FontFamily CabinMedium = LoadFontFamily(Properties.Resources.CabinMedium);
		internal static readonly FontFamily OstrichSansHeavy = LoadFontFamily(Properties.Resources.OstrichSansHeavy);

		private static FontFamily LoadFontFamily(byte[] fontFile) {
			using var ms = new MemoryStream(fontFile);
			return FontCollection.Add(ms);
		}
	}

	private readonly (Image<L8> image, float aspectRatio, string s)[] _samples;
	private readonly byte _backgroundValue;
	private readonly byte _foregroundValue;

	/// <param name="font">The font used in the images to recognise.</param>
	/// <param name="backgroundValue">The background brightness in the images to recognise.</param>
	/// <param name="foregroundValue">The text brightness in the images to recognise.</param>
	/// <param name="resolution">The resolution that will be used for sample images.</param>
	/// <param name="strings">The strings to choose between.</param>
	/// <exception cref="ArgumentException">The specified font is too large for all sample strings to fit in the specified size.</exception>
	[SuppressMessage("ReSharper", "PossibleLossOfFraction")]
	public TextRecogniser(Font font, byte backgroundValue, byte foregroundValue, Size resolution, params string[] strings) {
		_backgroundValue = backgroundValue;
		_foregroundValue = foregroundValue;
		_samples = new (Image<L8>, float, string)[strings.Length];
		var textOptions = new RichTextOptions(font) { Dpi = 96, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Origin = new(resolution.Width / 2, resolution.Height / 2) };
		for (var i = 0; i < strings.Length; i++) {
			var image = new Image<L8>(resolution.Width, resolution.Height, new(0));
			// ReSharper disable once AccessToModifiedClosure
			image.Mutate(c => c.DrawText(textOptions, strings[i], Color.White));
			var textBoundingBox = ImageUtils.FindEdges(image, image.Bounds, c => c.PackedValue >= 128);
			if (textBoundingBox.Top <= 0 || textBoundingBox.Bottom >= image.Height)
				throw new ArgumentException($"Sample text height went out of the specified bounds. {TextMeasurer.MeasureSize(strings[i], textOptions)}");
			if (textBoundingBox.Left <= 0 || textBoundingBox.Right >= image.Width)
				throw new ArgumentException($"Sample text width went out of the specified bounds. {TextMeasurer.MeasureSize(strings[i], textOptions)}");
			image.Mutate(c => c.Crop(textBoundingBox).Resize(resolution, KnownResamplers.NearestNeighbor, false));
			_samples[i] = (image, (float) textBoundingBox.Width / textBoundingBox.Height, strings[i]);
		}
	}

	/// <summary>Identifies the text in the specified bounding box of the specified image.</summary>
	[Pure]
	public string Recognise(Image<Rgba32> image, Rectangle rectangle) => Recognise(image, rectangle, _backgroundValue, _foregroundValue);
	/// <summary>Identifies the text in the specified bounding box of the specified image.</summary>
	[Pure]
	public string Recognise(Image<Rgba32> image, Rectangle rectangle, byte backgroundValue, byte foregroundValue) {
		var denominator = foregroundValue - backgroundValue;
		string? result = null;
		var bestDist = int.MaxValue;
		var checkRatio = (float) rectangle.Width / rectangle.Height;

#if TEXT_RECOGNISER_DEBUG
		System.Diagnostics.Debug.WriteLine($"## {nameof(TextRecogniser)} starting recognition...");
		Directory.CreateDirectory("TextRecogniserDebug");
		var filenameBase = DateTime.Now.ToString("O").Replace(':', '-');
#endif

		foreach (var (refImage, refRatio, s) in _samples) {
			if (checkRatio / refRatio is < 0.5f or > 2) continue;  // Skip strings that are way too narrow or too wide to match this rectangle.

			refImage.ProcessPixelRows(image, (ar, ac) => {
				var gridTotals = new int[(ar.Width + 7) / 8];
#if TEXT_RECOGNISER_DEBUG
				using var debugImage = new Image<L8>(ar.Width * 3, ar.Height);
				debugImage.Mutate(p => p.DrawImage(refImage, new Point(refImage.Width * 2, 0), 1));
#endif
				var dist = 0;
				for (var y = 0; y < ar.Height; y++) {
					var refRow = ar.GetRowSpan(y);
					var checkRow = ac.GetRowSpan(rectangle.Y + y * rectangle.Height / ar.Height);
					for (var x = 0; x < ar.Width; x++) {
						var checkPixel = checkRow[rectangle.X + x * rectangle.Width / ar.Width];
						var checkL = (Math.Min(Math.Min(checkPixel.R, checkPixel.G), checkPixel.B) + Math.Max(Math.Max(checkPixel.R, checkPixel.G), checkPixel.B)) / 2;
						var checkLScaled1 = Math.Clamp((checkL - backgroundValue) * 255 / denominator, 0, 255);
						var checkLScaled = (checkLScaled1 * checkLScaled1) >> 8;
						var refL = refRow[x].PackedValue;

						gridTotals[x / 8] += Math.Abs(checkLScaled - refL);
#if TEXT_RECOGNISER_DEBUG
						debugImage[x + ar.Width, y] = new((byte) checkLScaled);
						debugImage[x, y] = new((byte) Math.Abs(checkLScaled - refL));
#else
						//if (dist >= bestDist) return;
#endif
					}

					if (y % 8 == 7) {
						for (var gx = 0; gx < gridTotals.Length; gx++) {
							dist += (gridTotals[gx] * gridTotals[gx]) >> 14;
#if TEXT_RECOGNISER_DEBUG
							System.Diagnostics.Debug.Write($"{gridTotals[gx]} ");
#else
							if (dist >= bestDist) return;
#endif
						}
						Array.Clear(gridTotals);
					}
				}
#if TEXT_RECOGNISER_DEBUG
				System.Diagnostics.Debug.WriteLine($"'{s}': {dist}");
				debugImage.SaveAsPng(Path.Combine("TextRecogniserDebug", $"{filenameBase}.{s}.dist.png"));
				if (dist >= bestDist) return;
#endif
				// If we got here, this is the best match so far.
				bestDist = dist;
				result = s;
			});
		}

#if TEXT_RECOGNISER_DEBUG
		System.Diagnostics.Debug.WriteLine($"Recognition result: '{result}'");
#endif
		return result ?? throw new ArgumentException("Couldn't recognise the text.");
	}
}
