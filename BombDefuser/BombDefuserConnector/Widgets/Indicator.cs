using System;
using System.Collections.Generic;
using System.IO;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace BombDefuserConnector.Widgets;
public class Indicator : WidgetProcessor<Indicator.ReadData> {
	public override string Name => "Indicator";

	private static readonly Font FONT;

	static Indicator() {
		using var ms = new MemoryStream(Properties.Resources.OstrichSansHeavy);
		var fontCollection = new FontCollection();
		var fontFamily = fontCollection.Add(ms);
		FONT = new(fontFamily, 50);

		referenceImages = new[] {
			(CreateSampleImage("SND"), "SND"),
			(CreateSampleImage("CLR"), "CLR"),
			(CreateSampleImage("CAR"), "CAR"),
			(CreateSampleImage("IND"), "IND"),
			(CreateSampleImage("FRQ"), "FRQ"),
			(CreateSampleImage("SIG"), "SIG"),
			(CreateSampleImage("NSA"), "NSA"),
			(CreateSampleImage("MSA"), "MSA"),
			(CreateSampleImage("TRN"), "TRN"),
			(CreateSampleImage("BOB"), "BOB"),
			(CreateSampleImage("FRK"), "FRK"),
			(CreateSampleImage("NLL"), "NLL")
		};
	}

	private static Image<Rgb24> CreateSampleImage(string text) {
		var image = new Image<Rgb24>(128, 64, new(0, 0, 0));
		image.Mutate(c => c.DrawText(new TextOptions(FONT) { Dpi = 96, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Origin = new(64, 32) }, text, Color.White));
		var textBoundingBox = ImageUtils.FindEdges(image, image.Bounds, c => c.B >= 128);
		image.Mutate(c => c.Crop(textBoundingBox).Resize(128, 64, KnownResamplers.NearestNeighbor));
		return image;
	}

	private static readonly (Image<Rgb24> image, string text)[] referenceImages;

	protected internal override float IsWidgetPresent(Image<Rgb24> image, LightsState lightsState, PixelCounts pixelCounts)
		// This has many red pixels, few white pixels and no yellow pixels.
		=> Math.Max(0, pixelCounts.Red - pixelCounts.Yellow * 2 - Math.Max(0, pixelCounts.White - 4096) * 2) / 8192f;

	private static bool IsRed(HsvColor hsv) => hsv.H is >= 345 or < 15 && hsv.S >= 0.25f;
	private static bool IsLit(HsvColor hsv) => hsv.V >= 1;
	private static bool IsUnlit(HsvColor hsv) => hsv.H >= 30 && hsv.S < 0.15f && hsv.V is >= 0.05f and < 0.2f;

	protected internal override ReadData Process(Image<Rgb24> image, LightsState lightsState, ref Image<Rgb24>? debugBitmap) {
		var corners = ImageUtils.FindCorners(image, image.Bounds, c => IsRed(HsvColor.FromColor(c)), true);
		var indicatorImage = ImageUtils.PerspectiveUndistort(image, corners, InterpolationMode.NearestNeighbour, new(256, 112));
		if (debugBitmap is not null)
			ImageUtils.DebugDrawPoints(debugBitmap, corners);

		debugBitmap?.Mutate(c => c.Resize(new ResizeOptions() { Size = new(512, 512), Mode = ResizeMode.BoxPad, Position = AnchorPositionMode.TopLeft, PadColor = Color.Black }).DrawImage(indicatorImage, new Point(0, 256), 1));

		bool ledIsOnRight, isLit;
		var hsv = HsvColor.FromColor(indicatorImage[56, 56]);
		if (IsUnlit(hsv)) {
			ledIsOnRight = false;
			isLit = false;
		} else if (IsLit(hsv)) {
			ledIsOnRight = false;
			isLit = true;
		} else {
			hsv = HsvColor.FromColor(indicatorImage[200, 56]);
			if (IsUnlit(hsv)) {
				ledIsOnRight = true;
				isLit = false;
			} else if (IsLit(hsv)) {
				ledIsOnRight = true;
				isLit = true;
			} else
				throw new ArgumentException("Can't find LED");
		}

		if (ledIsOnRight)
			indicatorImage.Mutate(c => c.Rotate(RotateMode.Rotate180));

		var textBoundingBox = ImageUtils.FindEdges(indicatorImage, new(116, 28, 96, 56), c => c.B >= 128);
		indicatorImage.Mutate(c => c.Crop(textBoundingBox).Resize(128, 64, KnownResamplers.NearestNeighbor));
		debugBitmap?.Mutate(c => c.DrawImage(indicatorImage, new Point(0, 384), 1));

		var label = "";
		var labelGuesses = new List<(string label, int dist)>();
		var bestDist = int.MaxValue;
		foreach (var (refImage, text) in referenceImages) {
			var dist = 0;
			for (var y = 0; y < refImage.Height; y++) {
				for (var x = 0; x < refImage.Width; x++) {
					var cr = refImage[x, y];
					var cs = indicatorImage[x, y];
					dist += Math.Abs(cr.B - cs.B);
				}
			}
			labelGuesses.Add((text, dist));
			if (dist < bestDist) {
				bestDist = dist;
				label = text;
			}
		}

		return new(isLit, label);
	}

	public record ReadData(bool IsLit, string Label) {
		public override string ToString() => $"{(this.IsLit ? "Lit" : "Unlit")} {this.Label}";
	}
}
