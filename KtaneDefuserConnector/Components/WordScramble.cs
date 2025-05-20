using System.Linq;
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
		var rect2 = ImageUtils.FindEdges(image, baseRect2, lightsState switch {
			LightsState.Emergency => p => HsvColor.FromColor(p) is { H: >= 5 and < 30, S: >= 0.25f and < 0.48f, V: >= 0.88f } && !p.IsSelectionHighlight(LightsState.Emergency),
			LightsState.Buzz => p => HsvColor.FromColor(p) is { H: >= 15 and < 45, S: >= 0.05f and < 0.50f, V: >= 0.08f },
			LightsState.Off => p => HsvColor.FromColor(p) is { H: not 0, S: < 0.18f },
			_ => p => HsvColor.FromColor(p) is { H: >= 15 and < 75, S: >= 0.05f and < 0.50f, V: >= 0.3f }
		});
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
				var textRect = ImageUtils.FindEdges(image, new(keyX + xOffset, keyY + yOffset, keyWidth - xOffset * 2, keyHeight - yOffset * 2), lightsState switch {
					LightsState.Buzz => p => p.G < 28,
					LightsState.Off => p => p.G < 5,
					_ => p => p.G < 128
				});
				debugImage?.Mutate(p => p.Draw(Color.Magenta, 1, textRect));
				chars[y * 3 + x] = TextRecogniser.Recognise(image, textRect, lightsState switch { LightsState.Buzz => 50, LightsState.Off => 8, _ => 216 }, 0)[0];
			}
		}

		// Find the selection.
		var highlight = FindSelectionHighlight(image, lightsState, 20, 80, 192, 192);
		Point? selection = highlight.Y == 0 ? null : new(highlight.X switch { < 64 => 0, < 120 => 1, < 172 => 2, _ => 3 }, highlight.Y < 160 ? 0 : 1);

		return new(selection, chars);
	}

	public record ReadData(Point? Selection, char[] Letters) : ComponentReadData(Selection) {
		public override string ToString() => $"ReadData {{ Letters = {string.Join(null, Letters)}, Selection = {Selection} }}";
	}
}
