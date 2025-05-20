using System;
using System.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace KtaneDefuserConnector.Components;
public class Memory : ComponentReader<Memory.ReadData> {
	public override string Name => "Memory";

	private static readonly TextRecogniser DisplayRecogniser = new(new(TextRecogniser.Fonts.CabinMedium, 48), 70, 255, new(64, 64),
		"1", "2", "3", "4");
	private static readonly TextRecogniser KeyRecogniser = new(new(TextRecogniser.Fonts.OstrichSansHeavy, 48), 255, 0, new(64, 64),
		"1", "2", "3", "4");

	protected internal override ReadData Process(Image<Rgba32> image, LightsState lightsState, ref Image<Rgba32>? debugImage) {
		var displayBorderRect = ImageUtils.FindEdges(image, image.Map(50, 40, 108, 80), c => c is { R: < 44, G: < 44, B: < 44 });
		displayBorderRect.Inflate(-4, -4);
		var textRect = ImageUtils.FindEdges(image, displayBorderRect, c => c.G >= 192);

		debugImage?.Mutate(c => c.Draw(Color.Red, 1, displayBorderRect).Draw(Color.Lime, 1, textRect));
		var displayText = DisplayRecogniser.Recognise(image, textRect);

		var keypadRect = ImageUtils.FindEdges(image, image.Map(20, 148, 164, 72), lightsState switch {
			LightsState.Emergency => c => HsvColor.FromColor(c) is { H: <= 30, S: >= 0.50f and < 0.70f, V: >= 0.70f },
			LightsState.Off => c => HsvColor.FromColor(c) is { H: >= 0 and <= 60, S: <= 0.4f, V: >= 0.01f and < 0.10f },
			_ => c => HsvColor.FromColor(c) is { H: >= 30 and <= 45, S: >= 0.2f and <= 0.4f }
		});
		debugImage?.Mutate(c => c.Draw(Color.Yellow, 1, keypadRect));
		var keyLabels = new int[4];
		for (var i = 0; i < 4; i++) {
			var rect = new Rectangle(keypadRect.X + keypadRect.Width * i / 4, keypadRect.Y + 6, keypadRect.Width / 4, keypadRect.Height - 12);
			rect = Rectangle.Inflate(rect, -4, -4);
			rect = ImageUtils.FindEdges(image, rect, lightsState switch {
				LightsState.Buzz => p => HsvColor.FromColor(p) is { H: <= 60, V: < 0.04f },
				LightsState.Off => p => p is { R: 1, G: 1, B: 1 },
				LightsState.Emergency => p => HsvColor.FromColor(p) is { H: <= 15, V: < 0.30f },
				_ => p => HsvColor.FromColor(p) is { H: <= 60, V: < 0.25f }
			});
			debugImage?.Mutate(c => c.Draw(Color.Lime, 1, rect));
			keyLabels[i] = KeyRecogniser.Recognise(image, rect, lightsState switch { LightsState.Buzz => 50, LightsState.Off => 8, _ => 216 }, 0)[0] - '0';
		}

		var highlight = FindSelectionHighlight(image, lightsState, 16, 151, 188, 172);
		Point? selection = highlight.Y == 0 ? null : new(highlight.X switch { < 48 => 0, < 88 => 1, < 128 => 2, _ => 3 }, 0);

		return Enumerable.Range(1, 4).All(keyLabels.Contains)
			? new(selection, ReadStageIndicator(image), displayText[0] - '0', keyLabels)
			: throw new ArgumentException("Invalid key labels found.");
	}

	public record ReadData(Point? Selection, int StagesCleared, int Display, int[] Keys) : ComponentReadData(Selection) {
		public override string ToString() => $"ReadData {{ StagesCleared = {StagesCleared}, Display = {Display}, Keys = {{ {string.Join(", ", Keys)} }} }}";
	}
}
