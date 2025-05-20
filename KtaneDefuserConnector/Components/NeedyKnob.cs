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

		static bool IsRectangleLit(PixelAccessor<Rgba32> a, Rectangle rectangle) {
			var count = 0;
			rectangle = a.Map(rectangle);
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

		image.ProcessPixelRows(a => {
			for (var i = 0; i < 12; i++)
				lights[i] = IsRectangleLit(a, Rectangles[i]);
		});
		debugImage?.Mutate(p => {
			for (var i = 0; i < 12; i++)
				p.Draw(lights[i] ? Color.Lime : Color.Grey, 1, Rectangles[i]);
		});

		return new(time, lights);
	}

	public record ReadData(int? Time, bool[] Lights) : ComponentReadData(default(Point)) {
		public override string ToString() => $"ReadData {{ Time = {Time}, Lights = ({string.Join(", ", Lights)}) }}";
	}
}
