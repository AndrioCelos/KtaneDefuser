using System;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace KtaneDefuserConnector.Widgets;
public class Indicator : WidgetReader<Indicator.ReadData> {
	public override string Name => "Indicator";

	private static readonly TextRecogniser textRecogniser = new(new(TextRecogniser.Fonts.OstrichSansHeavy, 48), 0, 255, new(128, 64),
		"SND", "CLR", "CAR", "IND", "FRQ", "SIG", "NSA", "MSA", "TRN", "BOB", "FRK", "NLL");

	protected internal override float IsWidgetPresent(Image<Rgba32> image, LightsState lightsState, PixelCounts pixelCounts)
		// This has many red pixels, few white pixels and no yellow pixels.
		=> Math.Max(0, pixelCounts.Red - pixelCounts.Yellow * 2 - Math.Max(0, pixelCounts.White - 4096) * 2) / 8192f;

	private static bool IsRed(HsvColor hsv) => hsv is { H: >= 345 or < 15, S: >= 0.25f };
	private static bool IsLit(HsvColor hsv) => hsv.V >= 1;
	private static bool IsUnlit(HsvColor hsv) => hsv is { H: >= 30, S: < 0.15f, V: >= 0.05f and < 0.2f };

	protected internal override ReadData Process(Image<Rgba32> image, LightsState lightsState, ref Image<Rgba32>? debugImage) {
		var corners = ImageUtils.FindCorners(image, image.Bounds, c => IsRed(HsvColor.FromColor(c)), 12);
		var indicatorImage = ImageUtils.PerspectiveUndistort(image, corners, InterpolationMode.NearestNeighbour, new(256, 112));
		debugImage?.DebugDrawPoints(corners);
		debugImage?.Mutate(c => c.Resize(new ResizeOptions() { Size = new(512, 512), Mode = ResizeMode.BoxPad, Position = AnchorPositionMode.TopLeft, PadColor = Color.Black }).DrawImage(indicatorImage, new Point(0, 256), 1));

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
		//indicatorImage.Mutate(c => c.Crop(textBoundingBox).Resize(128, 64, KnownResamplers.NearestNeighbor));
		//debugImage?.Mutate(c => c.DrawImage(indicatorImage, new Point(0, 384), 1));

		var label = textRecogniser.Recognise(indicatorImage, textBoundingBox);

		return new(isLit, label);
	}

	public record ReadData(bool IsLit, string Label) {
		public override string ToString() => $"{(IsLit ? "Lit" : "Unlit")} {Label}";
	}
}
