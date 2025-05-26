using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace KtaneDefuserConnector.Components;
public class NeedyKnob : ComponentReader<NeedyKnob.ReadData> {
	public override string Name => "Needy Knob";

	// TODO: These rectangles were chosen with respect to viewing angles that can be seen on the vanilla bomb.
	// It should be fine with larger bombs, but should be tested.
	private static readonly Rectangle[] Rectangles = [
		new( 57, 168, 10, 10),
		new( 72, 190, 10, 10),
		new( 95, 205, 10, 10),
		new(147, 205, 10, 10),
		new(170, 190, 10, 10),
		new(184, 168, 10, 10),
		new( 39, 177, 10, 10),
		new( 58, 205, 10, 10),
		new( 87, 225, 10, 10),
		new(155, 225, 10, 10),
		new(184, 205, 10, 10),
		new(202, 177, 10, 10)
	];
	protected internal override ReadData Process(Image<Rgba32> image, LightsState lightsState, ref Image<Rgba32>? debugImage) {
		var time = ReadNeedyTimer(image, lightsState, debugImage);
		var lights = new bool[12];
		
		// Find the centre of the knob.
		var centre = new Point();
		image.ProcessPixelRows(a => {
			int maxY = 0, maxYVal = 0, topMid = 0, bottomMid = 0;
			foreach (var y in a.Height.MapRange(96, 192)) {
				var row = a.GetRowSpan(y);
				int leftLimit = 80 * a.Width / 256, rightLimit = 176 * a.Width / 256;
				int left = 0, right = 0;
				for (var x = leftLimit; x < rightLimit; x++) {
					if (HsvColor.FromColor(row[x]) is not { H: <= 60, S: >= 0.75f }) continue;
					left = x;
					break;
				}

				if (left == 0) {
					if (topMid != 0) {
						break;
					}
					continue;
				}

				for (var x = rightLimit - 1; x >= leftLimit; x--) {
					if (HsvColor.FromColor(row[x]) is not { H: <= 60, S: >= 0.75f }) continue;
					right = x;
					break;
				}
				
				bottomMid = (left + right) / 2;
				if (topMid == 0) topMid = bottomMid;

				var dist = right - left;
				if (dist <= maxYVal) continue;
				maxY = y;
				maxYVal = dist;
			}
			centre = new((topMid + bottomMid) / 2, maxY);
		});

		if (debugImage is not null) debugImage[centre.X, centre.Y] = Color.Green;

		var debugImage2 = debugImage;
		image.ProcessPixelRows(a => {
			for (var i = 0; i < 12; i++) {
				var rectangle = image.Map(Rectangles[i]);
				rectangle.Offset(centre.X - 124 * a.Width / 256, centre.Y - 144 * a.Height / 256);
				lights[i] = IsRectangleLit(a, rectangle);
				// ReSharper disable once AccessToModifiedClosure
				debugImage2?.Mutate(p => p.Draw(lights[i] ? Color.Lime : Color.Grey, 1, rectangle));
			}
		});

		return new(time, lights);

		static bool IsRectangleLit(PixelAccessor<Rgba32> a, Rectangle rectangle) {
			var count = 0;
			for (var dy = 0; dy < rectangle.Height; dy++) {
				var r = a.GetRowSpan(rectangle.Y + dy);
				for (var dx = 0; dx < rectangle.Width; dx++) {
					var p = r[rectangle.X + dx];
					if (p is not { R: >= 64, G: >= 176, B: >= 16 and < 96 }) continue;
					count++;
					if (count >= 16) return true;
				}
			}
			return false;
		}
	}

	public record ReadData(int? Time, bool[] Lights) : ComponentReadData(default(Point)) {
		public override string ToString() => $"ReadData {{ Time = {Time}, Lights = ({string.Join(", ", Lights)}) }}";
	}
}
