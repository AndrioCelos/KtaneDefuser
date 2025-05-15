using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace KtaneDefuserConnector.Components;
public class LetterKeys : ComponentReader<LetterKeys.ReadData> {
	public override string Name => "Letter Keys";

	private static readonly TextRecogniser LetterRecogniser = new(new(TextRecogniser.Fonts.OstrichSansHeavy, 48), 210, 0, new(64, 64), ["A", "B", "C", "D"]);
	private static readonly TextRecogniser NumberRecogniser = new(new(TextRecogniser.Fonts.OstrichSansHeavy, 48), 10, 128, new(64, 64), [.. from i in Enumerable.Range('0', 10) select ((char) i).ToString()]);

	protected internal override ReadData Process(Image<Rgba32> image, LightsState lightsState, ref Image<Rgba32>? debugImage) {
		// Find the display bounding box.
		var baseRect = image.Map(80, 12, 64, 52);
		var rect = ImageUtils.FindEdges(image, baseRect, p => p is { R: >= 160, G: < 16, B: < 16 });

		// Find the character bounding boxes.
		var display = 0;
		for (var x = rect.Left; x < rect.Right; x++) {
			if (!Enumerable.Range(rect.Top, rect.Height).Any(y => image[x, y] is { R: >= 160, G: < 16, B: < 16 })) continue;

			var x1 = x;
			for (x++; x < rect.Right; x++) {
				if (!Enumerable.Range(rect.Top, rect.Height).Any(y => image[x, y] is { R: >= 160, G: < 16, B: < 16 })) break;
			}

			int y1 = rect.Top, y2 = rect.Bottom;
			while (y1 < y2 && !Enumerable.Range(x1, x - x1).Any(x => image[x, y1] is { R: >= 160, G: < 16, B: < 16 }))
				y1++;
			while (y1 < y2 && !Enumerable.Range(x1, x - x1).Any(x => image[x, y2 - 1] is { R: >= 160, G: < 16, B: < 16 }))
				y2--;

			var charRect = new Rectangle(x1, y1, x - x1, y2 - y1);
			debugImage?.Mutate(c => c.Draw(Color.Magenta, 1, charRect));
			var c = NumberRecogniser.Recognise(image, charRect)[0];
			display = display * 10 + c - '0';
		}

		var labels = new char[4];
		for (var i = 0; i < 4; i++) {
			var keyBaseRect = image.Map(i % 2 == 0 ? 36 : 122, i < 2 ? 81 : 162, 61, 58);
			var keyRect = ImageUtils.FindEdges(image, keyBaseRect, p => p.R < 128);
			debugImage?.Mutate(c => c.Draw(Color.Magenta, 1, keyRect));
			labels[i] = LetterRecogniser.Recognise(image, keyRect)[0];
		}

		// Find the selection.
		Point point = default;
		image.ProcessPixelRows(p => {
			foreach (var y in image.Height.MapRange(64, 236, 8)) {
				var row = p.GetRowSpan(y);
				foreach (var x in image.Width.MapRange(20, 200, 2)) {
					if (HsvColor.FromColor(row[x]) is { H: < 30, S: >= 0.65f, V: >= 0.5f }) {
						point = new(x, y);
						return;
					}
				}
			}
		});

		return new(display, labels, point.Y == 0 ? null : new((point.X * 256 / image.Width) < 96 ? 0 : 1, (point.Y * 256 / image.Height) < 140 ? 0 : 1));
	}

	public record ReadData(int Display, char[] ButtonLabels, Point? Selection) {
		public override string ToString() => $"ReadData {{ {nameof(Display)} = {Display}, {nameof(ButtonLabels)} = {string.Join(null, ButtonLabels)}, {nameof(Selection)} = {Selection} }}";

	}
}
