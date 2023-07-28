using System;
using System.Collections.Generic;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace BombDefuserConnector.Widgets;
internal class PortPlate : WidgetProcessor {
	public override string Name => "Port Plate";

	public override float IsWidgetPresent(Image<Rgb24> image, LightsState lightsState, PixelCounts pixelCounts)
		// This has many dark grey pixels.
		=> Math.Max(0, pixelCounts.Grey - 4096) / 8192f;

	private static bool IsGrey(HsvColor hsv) => (hsv.S < 0.08f && hsv.V < 0.5f) || (hsv.H < 120 && hsv.S < 0.12f && hsv.V >= 0.8f);  // Also include the pale cream bezel on RJ-45 ports.
	private static bool IsPink(HsvColor hsv) => hsv.H >= 330 && hsv.S is >= 0.4f and < 0.6f && hsv.V >= 0.8f;
	private static bool IsTeal(HsvColor hsv) => hsv.H is >= 180 and < 210 && hsv.S is >= 0.4f and < 0.7f && hsv.V >= 0.4f;
	private static bool IsRcaRed(HsvColor hsv) => hsv.H < 15 && hsv.S >= 0.75f && hsv.V >= 0.25f;
	private static bool IsDviRed(HsvColor hsv) => hsv.H is >= 345 or < 30 && hsv.S is >= 0.4f and < 0.75f && hsv.V is >= 0.25f and < 0.75f;
	private static bool IsGreen(HsvColor hsv) => hsv.H is >= 120 and < 150 && hsv.S is >= 0.3f and < 0.6f && hsv.V is >= 0.5f and < 0.75f;

	public override object Process(Image<Rgb24> image, LightsState lightsState, ref Image<Rgb24>? debugBitmap) {
		var corners = ImageUtils.FindCorners(image, new(8, 8, 240, 240), c => IsGrey(HsvColor.FromColor(c)), true);
		var plateImage = ImageUtils.PerspectiveUndistort(image, corners, InterpolationMode.NearestNeighbour, new(256, 128));
		if (debugBitmap is not null)
			ImageUtils.DebugDrawPoints(debugBitmap, corners);

		debugBitmap?.Mutate(c => c.Resize(new ResizeOptions() { Size = new(512, 512), Mode = ResizeMode.BoxPad, Position = AnchorPositionMode.TopLeft, PadColor = Color.Black }).DrawImage(plateImage, new Point(0, 256), 1));

		int pinkCount = 0, tealCount = 0;
		int redCountEdge = 0, redCountMiddle = 0, greenCount = 0, blackCount = 0;

		plateImage.ProcessPixelRows(accessor => {
			for (var y = 0; y < accessor.Height; y++) {
				var row = accessor.GetRowSpan(y);
				for (var x = 0; x < accessor.Width; x++) {
					var p = row[x];
					var hsv = HsvColor.FromColor(p);
					if (IsGreen(hsv)) greenCount++;
					else if (x is >= 48 and < 208) {
						if (IsDviRed(hsv)) redCountMiddle++;
						else if (IsPink(hsv)) pinkCount++;
						else if (IsTeal(hsv)) tealCount++;
					} else {
						if (x is < 64 or >= 192 && IsRcaRed(hsv)) redCountEdge++;
						else if (hsv.V < 0.1f) blackCount++;
					}
				}
			}
		});

		var ports = new List<string>();
		if (pinkCount >= 600) ports.Add("Parallel");
		if (tealCount >= 300) ports.Add("Serial");
		if (redCountEdge >= 200) ports.Add("StereoRCA");
		if (pinkCount < 600 && redCountMiddle >= 1000) ports.Add("DVID");
		if (greenCount >= 200) ports.Add("PS2");
		if (blackCount >= 200) ports.Add("RJ45");

		return new ReadData(ports, pinkCount, tealCount, redCountEdge, redCountMiddle, greenCount, blackCount);
	}

	public record ReadData(ICollection<string> ports, int pinkCount, int tealCount, int redCountEdge, int redCountMiddle, int greenCount, int blackCount) {
		public override string? ToString() => ports.Count == 0 ? "nil" : string.Join(' ', this.ports);
	}
}
