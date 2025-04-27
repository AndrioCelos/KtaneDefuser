using System.Diagnostics.CodeAnalysis;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace KtaneDefuserConnector.Components;
public class WhosOnFirst : ComponentReader<WhosOnFirst.ReadData> {
	public override string Name => "Who's on First";

	private static readonly TextRecogniser DisplayRecogniser = new(new(TextRecogniser.Fonts.CabinMedium, 24), 88, 255, new(256, 64),
		"YES", "FIRST", "DISPLAY", "OKAY", "SAYS", "NOTHING", "BLANK", "NO", "LED", "LEAD", "READ", "RED", "REED", "LEED",
		"HOLD ON", "YOU", "YOU ARE", "YOUR", "YOU'RE", "UR", "THERE", "THEY'RE", "THEIR", "THEY ARE", "SEE", "C", "CEE");
	private static readonly TextRecogniser KeyRecogniser = new(new(TextRecogniser.Fonts.OstrichSansHeavy, 24), 160, 44, new(128, 64),
		"READY", "FIRST", "NO", "BLANK", "NOTHING", "YES", "WHAT", "UHHH", "LEFT", "RIGHT", "MIDDLE", "OKAY", "WAIT", "PRESS",
		"YOU", "YOU ARE", "YOUR", "YOU’RE", "UR", "U", "UH HUH", "UH UH", "WHAT?", "DONE", "NEXT", "HOLD", "SURE", "LIKE");

	[SuppressMessage("ReSharper", "AccessToModifiedClosure")]
	protected internal override ReadData Process(Image<Rgba32> image, LightsState lightsState, ref Image<Rgba32>? debugImage) {
		var displayBorderRect = ImageUtils.FindEdges(image, new(28, 22, 140, 50), c => c is { R: < 44, G: < 44, B: < 44 });
		displayBorderRect.Inflate(-4, -4);

		string displayText;
		if (ImageUtils.TryFindEdges(image, displayBorderRect, c => c.B >= 192, out var textRect)) {
			debugImage?.Mutate(c => c.Draw(Color.Red, 1, displayBorderRect).Draw(Color.Lime, 1, textRect));
			displayText = DisplayRecogniser.Recognise(image, textRect);
		} else
			displayText = "";

		bool IsKeyBackground(Rgba32 c) => HsvColor.FromColor(ImageUtils.ColourCorrect(c, lightsState)) is { H: >= 30 and <= 45, S: >= 0.2f and <= 0.4f };

		var keyLabels = new string[6];
		for (var i = 0; i < 6; i++) {
			var keyRect = i switch {
				0 => new(24, 90, 74, 44),
				1 => new(104, 90, 74, 44),
				2 => new(24, 134, 74, 44),
				3 => new(104, 134, 74, 44),
				4 => new(24, 178, 74, 44),
				_ => new Rectangle(104, 178, 74, 44)
			};
			debugImage?.ColourCorrect(lightsState, keyRect);
			keyRect = ImageUtils.FindEdges(image, keyRect, IsKeyBackground);
			textRect = Rectangle.Inflate(keyRect, -3, -5);
			textRect = ImageUtils.FindEdges(image, textRect, c => ImageUtils.ColourCorrect(c, lightsState).R < 128);
			debugImage?.Mutate(c => c.Draw(Color.Yellow, 1, keyRect).Draw(Color.Green, 1, textRect));
			image.ColourCorrect(lightsState, textRect);
			keyLabels[i] = KeyRecogniser.Recognise(image, textRect);
		}

		return new(ReadStageIndicator(image), displayText, keyLabels);
	}

	public record ReadData(int StagesCleared, string Display, string[] Keys) {
		public override string ToString() => $"ReadData {{ StagesCleared = {StagesCleared}, Display = {Display}, Keys = {{ {string.Join(", ", Keys)} }} }}";
	}
}
