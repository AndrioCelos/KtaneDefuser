using System.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace KtaneDefuserConnector.Components;
public class LightsOut : ComponentReader<LightsOut.ReadData> {
	public override string Name => "Lights Out";

	protected internal override ReadData Process(Image<Rgba32> image, LightsState lightsState, ref Image<Rgba32>? debugImage) {
		var time = ReadNeedyTimer(image, lightsState, debugImage);

		var lights = new bool[9];
		for (var i = 0; i < 9; i++) {
			var pixel = image[(i % 3) switch { 0 => 60, 1 => 128, _ => 196 } * image.Width / 256, (i / 3) switch { 0 => 114, 1 => 157, _ => 200 } * image.Height / 256];
			lights[i] = pixel.G >= 64;
		}

		// Find the selection.
		Point point = default;
		image.ProcessPixelRows(p => {
			foreach (var y in image.Height.MapRange(80, 192, 8)) {
				var row = p.GetRowSpan(y);
				foreach (var x in image.Width.MapRange(32, 192)) {
					if (HsvColor.FromColor(row[x]) is not { H: < 40, S: >= 0.75f, V: >= 0.5f }) continue;
					point = new(x, y);
					return;
				}
			}
		});

		return new(time, lights, point.Y == 0 ? null : new((point.X * 256 / image.Width) switch { < 60 => 0, < 128 => 1, _ => 2 }, (point.Y * 256 / image.Width) switch { < 128 => 0, < 168 => 1, _ => 2 }));
	}

	public record ReadData(int? Time, bool[] Lights, Point? Selection) {
		public override string ToString() => $"ReadData {{ {nameof(Time)} = {Time}, {nameof(Lights)} = {string.Join(' ', from y in Enumerable.Range(0, 3) select string.Join(null, from x in Enumerable.Range(0, 3) select Lights[x + y * 3] ? '*' : '·'))}, {nameof(Selection)} = {Selection} }}";
	}
}
