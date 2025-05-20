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

		var highlight = FindSelectionHighlight(image, lightsState, 16, 80, 240, 224);
		Point? selection = highlight.Y == 0 ? null : new(highlight.X switch { < 72 => 0, < 144 => 1, _ => 2 }, highlight.Y switch { < 120 => 0, < 164 => 1, _ => 2 });

		return new(selection, time, lights);
	}

	public record ReadData(Point? Selection, int? Time, bool[] Lights) : ComponentReadData(Selection) {
		public override string ToString() => $"ReadData {{ {nameof(Selection)} = {Selection}, {nameof(Time)} = {Time}, {nameof(Lights)} = {string.Join(' ', from y in Enumerable.Range(0, 3) select string.Join(null, from x in Enumerable.Range(0, 3) select Lights[x + y * 3] ? '*' : '·'))} }}";
	}
}
