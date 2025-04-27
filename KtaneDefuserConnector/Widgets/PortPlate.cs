using System;
using System.Collections;
using System.Collections.Generic;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace KtaneDefuserConnector.Widgets;
public class PortPlate : WidgetReader<PortPlate.Ports> {
	public override string Name => "Port Plate";

	private static bool IsGrey(HsvColor hsv) => hsv is { S: < 0.08f, V: < 0.5f } or { H: < 120, S: < 0.12f, V: >= 0.8f };  // Also include the pale cream bezel on RJ-45 ports.
	private static bool IsPink(HsvColor hsv) => hsv is { H: >= 330, S: >= 0.4f and < 0.6f, V: >= 0.65f };
	private static bool IsTeal(HsvColor hsv) => hsv is { H: >= 180 and < 210, S: >= 0.4f and < 0.7f, V: >= 0.4f };
	private static bool IsRcaRed(HsvColor hsv) => hsv is { H: < 15, S: >= 0.75f, V: >= 0.25f };
	private static bool IsDviRed(HsvColor hsv) => hsv is { H: >= 345 or < 30, S: >= 0.4f and < 0.75f, V: >= 0.25f and < 0.75f };
	private static bool IsGreen(HsvColor hsv) => hsv is { H: >= 120 and < 150, S: >= 0.3f and < 0.6f, V: >= 0.5f and < 0.75f };

	protected internal override Ports Process(Image<Rgba32> image, LightsState lightsState, ref Image<Rgba32>? debugImage) {
		var corners = ImageUtils.FindCorners(image, new(8, 8, 240, 240), c => IsGrey(HsvColor.FromColor(c)), 12);
		var plateImage = ImageUtils.PerspectiveUndistort(image, corners, InterpolationMode.NearestNeighbour, new(256, 128));
		debugImage?.DebugDrawPoints(corners);
		debugImage?.Mutate(c => c.Resize(new ResizeOptions { Size = new(512, 512), Mode = ResizeMode.BoxPad, Position = AnchorPositionMode.TopLeft, PadColor = Color.Black }).DrawImage(plateImage, new Point(0, 256), 1));

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
		if (redCountEdge >= 200) ports |= PortType.StereoRca;
		if (pinkCount < 600 && redCountMiddle >= 1000) ports |= PortType.DviD;
		if (greenCount >= 200) ports |= PortType.PS2;
		if (blackCount >= 200) ports |= PortType.RJ45;
		return new(ports);
	}

	public readonly struct Ports(PortType value) : IReadOnlyCollection<PortType> {
		public readonly PortType Value = value;

		public bool Contains(PortType portType) => Value.HasFlag(portType);

		public int Count {
			get {
				var count = 0;
				using var enumerator = GetEnumerator();
				while (enumerator.MoveNext()) count++;
				return count;
			}
		}

		public IEnumerator<PortType> GetEnumerator() {
			if (Value.HasFlag(PortType.Parallel)) yield return PortType.Parallel;
			if (Value.HasFlag(PortType.Serial)) yield return PortType.Serial;
			if (Value.HasFlag(PortType.StereoRca)) yield return PortType.StereoRca;
			if (Value.HasFlag(PortType.DviD)) yield return PortType.DviD;
			if (Value.HasFlag(PortType.PS2)) yield return PortType.PS2;
			if (Value.HasFlag(PortType.RJ45)) yield return PortType.RJ45;
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		public override string ToString() => Value != 0 ? string.Join(' ', this) : "nil";
	} 

	[Flags]
	public enum PortType {
		Parallel = 1,
		Serial = 2,
		StereoRca = 4,
		DviD = 8,
		PS2 = 16,
		RJ45 = 32
	}
}
