using System.Linq;
using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace KtaneDefuserConnector.Components;
public class WordScramble : ComponentReader<WordScramble.ReadData> {
	public override string Name => "Word Scramble";

	private static readonly TextRecogniser TextRecogniser = new(new(TextRecogniser.Fonts.OstrichSansHeavy, 20), 216, 0, new(32, 64),
		[.. from i in Enumerable.Range('A', 26) where i is not ('Q' or 'Z') select ((char) i).ToString()]);

	protected internal override ReadData Process(Image<Rgba32> image, LightsState lightsState, ref Image<Rgba32>? debugImage) {
		// Find the keyboard.
		var baseRect2 = image.Map(24, 116, 214, 112);
		var rect2 = ImageUtils.FindEdges(image, baseRect2, p => HsvColor.FromColor(p) is { H: >= 15 and < 75, S: >= 0.05f and < 0.50f, V: >= 0.3f });
		debugImage?.Mutate(p => p.Draw(Color.Lime, 1, rect2));

		// Read the keys.
		var chars = new char[6];
		for (var y = 0; y < 2; y++) {
			var keyY = rect2.Y + rect2.Height * y / 2;
			var keyHeight = rect2.Height / 2;
			for (var x = 0; x < 3; x++) {
				var keyX = rect2.X + rect2.Width * x / 4;
				var keyWidth = rect2.Width / 4;
				var xOffset = 12 * image.Width / 256;
				var yOffset = 8 * image.Height / 256;
				var textRect = ImageUtils.FindEdges(image, new(keyX + xOffset, keyY + yOffset, keyWidth - xOffset * 2, keyHeight - yOffset * 2), p => p.G < 144);
				debugImage?.Mutate(p => p.Draw(Color.Magenta, 1, textRect));
				chars[y * 3 + x] = TextRecogniser.Recognise(image, textRect)[0];
			}
		}

		// Find the selection.
		Point point = default;
		image.ProcessPixelRows(p => {
			foreach (var y in image.Height.MapRange(80, 192, 8)) {
				var row = p.GetRowSpan(y);
				foreach (var x in image.Width.MapRange(20, 192, 2)) {
					if (HsvColor.FromColor(row[x]) is not { H: < 30, S: >= 0.65f, V: >= 0.5f }) continue;
					point = new(x, y);
					return;
				}
			}
		});

		return new(chars, point.Y == 0 ? null : new((point.X * 256 / image.Width) switch { < 64 => 0, < 120 => 1, < 172 => 2, _ => 3 }, (point.Y * 256 / image.Height) switch { < 160 => 0, _ => 1 }));
	}

	public record ReadData(char[] Letters, Point? Selection) {
		public override string ToString() => $"ReadData {{ Letters = {string.Join(null, Letters)}, Selection = {Selection} }}";
	}
}
