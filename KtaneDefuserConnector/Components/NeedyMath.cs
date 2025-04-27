using System.Linq;
using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace KtaneDefuserConnector.Components;
public class NeedyMath : ComponentReader<NeedyMath.ReadData> {
	public override string Name => "Needy Math";

	private static readonly TextRecogniser TextRecogniser = new(new(TextRecogniser.Fonts.OstrichSansHeavy, 48), 10, 128, new(64, 64), [.. from i in Enumerable.Range(0, 10) select i.ToString(), "+", "-"]);

	protected internal override ReadData Process(Image<Rgba32> image, LightsState lightsState, ref Image<Rgba32>? debugImage) {
		var time = ReadNeedyTimer(image, lightsState, debugImage);

		// Find the expression display bounding box.
		var baseRect = new Rectangle(8, 8, 88, 64);
		var rect = ImageUtils.FindEdges(image, baseRect, p => p is { R: >= 192, G: < 16, B: < 16 });

		// Find the character bounding boxes.
		var builder = new StringBuilder();
		for (var x = rect.Left; x < rect.Right; x++) {
			if (!Enumerable.Range(rect.Top, rect.Height).Any(y => image[x, y] is { R: >= 192, G: < 16, B: < 16 })) continue;

			var x1 = x;
			for (x++; x < rect.Right; x++) {
				if (!Enumerable.Range(rect.Top, rect.Height).Any(y => image[x, y] is { R: >= 192, G: < 16, B: < 16 })) break;
			}

			int y1 = rect.Top, y2 = rect.Bottom;
			while (y1 < y2 && !Enumerable.Range(x1, x - x1).Any(x => image[x, y1] is { R: >= 192, G: < 16, B: < 16 }))
				y1++;
			while (y1 < y2 && !Enumerable.Range(x1, x - x1).Any(x => image[x, y2 - 1] is { R: >= 192, G: < 16, B: < 16 }))
				y2--;

			var charRect = new Rectangle(x1, y1, x - x1, y2 - y1);
			debugImage?.Mutate(c => c.Draw(Color.Magenta, 1, charRect));
			builder.Append(TextRecogniser.Recognise(image, charRect));
		}

		// Find the selection.
		Point point = default;
		image.ProcessPixelRows(p => {
			for (var y = 80; y < 192; y += 8) {
				var row = p.GetRowSpan(y);
				for (var x = 32; x < 192; x += 2) {
					if (HsvColor.FromColor(row[x]) is not { H: < 30, S: >= 0.75f, V: >= 0.5f }) continue;
					point = new(x, y);
					return;
				}
			}
		});

		return new(time, builder.ToString(), point.Y == 0 ? null : new(point.X switch { < 66 => 0, < 108 => 1, < 150 => 2, _ => 3 }, point.Y switch { < 128 => 0, < 168 => 1, _ => 2 }));
	}

	public record ReadData(int? Time, string Display, Point? Selection);
}
