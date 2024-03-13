using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace BombDefuserConnector;
/// <summary>Identifies images of text.</summary>
internal class TextRecogniser {
	internal static class Fonts {
		private static readonly FontCollection fontCollection = new();

		internal static readonly FontFamily CABIN_MEDIUM = LoadFontFamily(Properties.Resources.CabinMedium);
		internal static readonly FontFamily OSTRICH_SANS_HEAVY = LoadFontFamily(Properties.Resources.OstrichSansHeavy);

		private static FontFamily LoadFontFamily(byte[] fontFile) {
			using var ms = new MemoryStream(fontFile);
			return fontCollection.Add(ms);
		}
	}

	private readonly (Image<L8> image, float aspectRatio, string s)[] samples;
	private readonly byte backgroundValue;
	private readonly byte foregroundValue;

	/// <param name="font">The font used in the images to recognise.</param>
	/// <param name="resolution">The resolution that will be used for sample images.</param>
	/// <param name="strings">The strings to choose between.</param>
	/// <exception cref="ArgumentException">The specified font is too large for all sample strings to fit in the specified size.</exception>
	public TextRecogniser(Font font, Size resolution, params string[] strings)
		: this(font, 0, 255, resolution, strings) { }
	/// <param name="font">The font used in the images to recognise.</param>
	/// <param name="backgroundValue">The background brightness in the images to recognise.</param>
	/// <param name="foregroundValue">The text brightness in the images to recognise.</param>
	/// <param name="resolution">The resolution that will be used for sample images.</param>
	/// <param name="strings">The strings to choose between.</param>
	/// <exception cref="ArgumentException">The specified font is too large for all sample strings to fit in the specified size.</exception>
	public TextRecogniser(Font font, byte backgroundValue, byte foregroundValue, Size resolution, params string[] strings) {
		this.backgroundValue = backgroundValue;
		this.foregroundValue = foregroundValue;
		this.samples = new (Image<L8>, float, string)[strings.Length];
		var textOptions = new RichTextOptions(font) { Dpi = 96, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Origin = new(resolution.Width / 2, resolution.Height / 2) };
		for (var i = 0; i < strings.Length; i++) {
			var image = new Image<L8>(resolution.Width, resolution.Height, new(0));
			image.Mutate(c => c.DrawText(textOptions, strings[i], Color.White));
			var textBoundingBox = ImageUtils.FindEdges(image, image.Bounds, c => c.PackedValue >= 128);
			if (textBoundingBox.Top <= 0 || textBoundingBox.Bottom >= image.Height)
				throw new ArgumentException("Sample text height went out of the specified bounds.");
			if (textBoundingBox.Left <= 0 || textBoundingBox.Right >= image.Width)
				throw new ArgumentException("Sample text width went out of the specified bounds.");
			image.Mutate(c => c.Crop(textBoundingBox).Resize(resolution, KnownResamplers.NearestNeighbor, false));
			this.samples[i] = (image, (float) textBoundingBox.Width / textBoundingBox.Height, strings[i]);
		}
	}

	/// <summary>Identifies the text in the specified bounding box of the specified image.</summary>
	public string Recognise(Image<Rgba32> image, Rectangle rectangle) => this.Recognise(image, rectangle, this.backgroundValue, this.foregroundValue);
	/// <summary>Identifies the text in the specified bounding box of the specified image.</summary>
	public string Recognise(Image<Rgba32> image, Rectangle rectangle, byte backgroundValue, byte foregroundValue) {
		var denominator = foregroundValue - backgroundValue;
		string? result = null;
		var bestDist = int.MaxValue;
		var checkRatio = (float) rectangle.Width / rectangle.Height;
		/*
		var debugImage = new Image<L8>(this.samples[2].image.Width, this.samples[2].image.Height);
		debugImage.ProcessPixelRows(image, (ar, ac) => {
			for (var y = 0; y < ar.Height; y++) {
				var rr = ar.GetRowSpan(y);
				var rc = ac.GetRowSpan(rectangle.Y + y * rectangle.Height / ar.Height);
				for (var x = 0; x < ar.Width; x++) {
					var pc = rc[rectangle.X + x * rectangle.Width / ar.Width];
					var lc = Math.Min(Math.Min(pc.R, pc.G), pc.B) + Math.Max(Math.Max(pc.R, pc.G), pc.B);
					rr[x] = new((byte) lc);
				}
			}
		});
		*/
		foreach (var (refImage, refRatio, s) in this.samples) {
			if (Math.Abs(checkRatio - refRatio) > 1) continue;  // Skip strings that are way too narrow or too wide to match this rectangle.
			refImage.ProcessPixelRows(image, (ar, ac) => {
				var dist = 0;
				for (var y = 0; y < ar.Height; y++) {
					var rr = ar.GetRowSpan(y);
					var rc = ac.GetRowSpan(rectangle.Y + y * rectangle.Height / ar.Height);
					for (var x = 0; x < ar.Width; x++) {
						var pc = rc[rectangle.X + x * rectangle.Width / ar.Width];
						var lc = (Math.Min(Math.Min(pc.R, pc.G), pc.B) + Math.Max(Math.Max(pc.R, pc.G), pc.B)) / 2;
						var lcScaled = Math.Max(0, Math.Min(255, (lc - backgroundValue) * 255 / denominator));
						var lr = rr[x].PackedValue;
						dist += Math.Abs(lcScaled - lr);
						if (dist >= bestDist) return;
					}
				}
				// If we got here, this is the best match so far.
				bestDist = dist;
				result = s;
			});
		}

		return result ?? throw new ArgumentException("Couldn't recognise the text.");
	}
}
