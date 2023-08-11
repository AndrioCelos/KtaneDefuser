using System;
using System.Collections;
using System.Collections.Generic;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace BombDefuserConnector.Widgets;
public class PortPlate : WidgetReader<PortPlate.Ports> {
	public override string Name => "Port Plate";

	protected internal override float IsWidgetPresent(Image<Rgba32> image, LightsState lightsState, PixelCounts pixelCounts)
		// This has many dark grey pixels.
		=> Math.Max(0, pixelCounts.Grey - 4096) / 8192f;

	private static bool IsGrey(HsvColor hsv) => (hsv.S < 0.08f && hsv.V < 0.5f) || (hsv.H < 120 && hsv.S < 0.12f && hsv.V >= 0.8f);  // Also include the pale cream bezel on RJ-45 ports.
	private static bool IsPink(HsvColor hsv) => hsv.H >= 330 && hsv.S is >= 0.4f and < 0.6f && hsv.V >= 0.65f;
	private static bool IsTeal(HsvColor hsv) => hsv.H is >= 180 and < 210 && hsv.S is >= 0.4f and < 0.7f && hsv.V >= 0.4f;
	private static bool IsRcaRed(HsvColor hsv) => hsv.H < 15 && hsv.S >= 0.75f && hsv.V >= 0.25f;
	private static bool IsDviRed(HsvColor hsv) => hsv.H is >= 345 or < 30 && hsv.S is >= 0.4f and < 0.75f && hsv.V is >= 0.25f and < 0.75f;
	private static bool IsGreen(HsvColor hsv) => hsv.H is >= 120 and < 150 && hsv.S is >= 0.3f and < 0.6f && hsv.V is >= 0.5f and < 0.75f;

	protected internal override Ports Process(Image<Rgba32> image, LightsState lightsState, ref Image<Rgba32>? debugImage) {
		var corners = ImageUtils.FindCorners(image, new(8, 8, 240, 240), c => IsGrey(HsvColor.FromColor(c)), 12);
		var plateImage = ImageUtils.PerspectiveUndistort(image, corners, InterpolationMode.NearestNeighbour, new(256, 128));
		if (debugImage is not null)
			ImageUtils.DebugDrawPoints(debugImage, corners);

		debugImage?.Mutate(c => c.Resize(new ResizeOptions() { Size = new(512, 512), Mode = ResizeMode.BoxPad, Position = AnchorPositionMode.TopLeft, PadColor = Color.Black }).DrawImage(plateImage, new Point(0, 256), 1));

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

		var ports = (PortType) 0;
		if (pinkCount >= 1200) ports |= PortType.Parallel;
		if (tealCount >= 300) ports |= PortType.Serial;
		if (redCountEdge >= 200) ports |= PortType.StereoRCA;
		if (pinkCount < 600 && redCountMiddle >= 1000) ports |= PortType.DviD;
		if (greenCount >= 200) ports |= PortType.PS2;
		if (blackCount >= 200) ports |= PortType.RJ45;
		return new(ports);
	}

	public struct Ports : IReadOnlyCollection<PortType> {
		public PortType Value;

		public Ports(PortType value) => this.Value = value;

		public readonly bool Contains(PortType portType) => this.Value.HasFlag(portType);

		public readonly int Count {
			get {
				var count = 0;
				var enumerator = this.GetEnumerator();
				while (enumerator.MoveNext()) count++;
				return count;
			}
		}

		public readonly IEnumerator<PortType> GetEnumerator() {
			if (this.Value.HasFlag(PortType.Parallel)) yield return PortType.Parallel;
			if (this.Value.HasFlag(PortType.Serial)) yield return PortType.Serial;
			if (this.Value.HasFlag(PortType.StereoRCA)) yield return PortType.StereoRCA;
			if (this.Value.HasFlag(PortType.DviD)) yield return PortType.DviD;
			if (this.Value.HasFlag(PortType.PS2)) yield return PortType.PS2;
			if (this.Value.HasFlag(PortType.RJ45)) yield return PortType.RJ45;
		}

		readonly IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

		public override readonly string ToString() => this.Value != 0 ? string.Join(' ', this) : "nil";
	} 

	[Flags]
	public enum PortType {
		Parallel = 1,
		Serial = 2,
		StereoRCA = 4,
		DviD = 8,
		PS2 = 16,
		RJ45 = 32
	}
}
