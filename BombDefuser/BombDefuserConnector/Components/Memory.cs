using System;
using System.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace BombDefuserConnector.Components;
public class Memory : ComponentProcessor<Memory.ReadData> {
	public override string Name => "Memory";
	protected internal override bool UsesNeedyFrame => false;

	private static readonly TextRecogniser displayRecogniser = new(new(TextRecogniser.Fonts.CABIN_MEDIUM, 48), 70, 255, new(64, 64),
		"1", "2", "3", "4");
	private static readonly TextRecogniser keyRecogniser = new(new(TextRecogniser.Fonts.OSTRICH_SANS_HEAVY, 48), 160, 44, new(64, 64),
		"1", "2", "3", "4");

	protected internal override float IsModulePresent(Image<Rgb24> image) {
		var minDist = int.MaxValue;
		var referenceColour = new Rgb24(55, 95, 81);
		for (int x = image.Width / 4 - 10; x < image.Width / 4 + 10; x++) {
			for (int y = image.Height / 4 - 10; y < image.Height / 4 + 10; y++) {
				var pixel = image[x, y];
				var dist = Math.Abs(pixel.R - referenceColour.R) + Math.Abs(pixel.G - referenceColour.G) + Math.Abs(pixel.B - referenceColour.B);
				minDist = Math.Min(minDist, dist);
			}
		}

		// Keypad should be at about Y = 192
		var count = 0f;
		var referenceColour2 = new Rgb24(220, 196, 155);
		var referenceColour3 = new Rgb24(51, 46, 37);
		for (var x = 24; x < 172; x++) {
			var pixel = image[x, 192];
			var dist = pixel.R < 128
				? Math.Abs(pixel.R - referenceColour3.R) + Math.Abs(pixel.G - referenceColour3.G) + Math.Abs(pixel.B - referenceColour3.B)
				: Math.Abs(pixel.R - referenceColour2.R) + Math.Abs(pixel.G - referenceColour2.G) + Math.Abs(pixel.B - referenceColour2.B);
			count += Math.Max(0, 1 - dist / 20);
		}

		// And not at about Y = 128
		var count2 = 0f;
		var referenceColour4 = new Rgb24(170, 150, 120);
		for (var x = 24; x < 172; x++) {
			var pixel = image[x, 128];
			var dist = Math.Abs(pixel.R - referenceColour4.R) + Math.Abs(pixel.G - referenceColour4.G) + Math.Abs(pixel.B - referenceColour4.B);
			count2 += Math.Max(0, 1 - dist / 20);
		}

		return Math.Max(1 - (float) minDist / 50, 0) * 0.5f + Math.Max(0, count / 148 - count2 / 148) * 0.5f;
	}

	protected internal override ReadData Process(Image<Rgb24> image, ref Image<Rgb24>? debugBitmap) {
		var displayBorderRect = ImageUtils.FindEdges(image, new(50, 40, 108, 80), c => c.R < 44 && c.G < 44 && c.B < 44);
		displayBorderRect.Inflate(-4, -4);
		var textRect = ImageUtils.FindEdges(image, displayBorderRect, c => c.G >= 192);

		debugBitmap?.Mutate(c => c.Draw(Color.Red, 1, displayBorderRect).Draw(Color.Lime, 1, textRect));
		var displayText = displayRecogniser.Recognise(image, textRect);

		var keypadRect = ImageUtils.FindEdges(image, new(20, 148, 164, 72), c => HsvColor.FromColor(c) is HsvColor hsv && hsv.H is >= 30 and <= 45 && hsv.S is >= 0.2f and <= 0.4f);
		debugBitmap?.Mutate(c => c.Draw(Color.Yellow, 1, keypadRect));
		var keyLabels = new int[4];
		for (int i = 0; i < 4; i++) {
			var rect = new Rectangle(keypadRect.X + keypadRect.Width * i / 4, keypadRect.Y, keypadRect.Width / 4, keypadRect.Height);
			rect = Rectangle.Inflate(rect, -2, -2);
			rect = ImageUtils.FindEdges(image, rect, c => HsvColor.FromColor(c) is HsvColor hsv && hsv.H <= 60 && hsv.V < 0.25f);
			debugBitmap?.Mutate(c => c.Draw(Color.Green, 1, rect));
			keyLabels[i] = keyRecogniser.Recognise(image, rect)[0] - '0';
		}

		return Enumerable.Range(1, 4).All(keyLabels.Contains)
			? new(ReadStageIndicator(image), displayText[0] - '0', keyLabels)
			: throw new ArgumentException("Invalid key labels found.");
	}

	public record ReadData(int StagesCleared, int Display, int[] Keys) {
		public override string ToString() => $"ReadData {{ StagesCleared = {this.StagesCleared}, Display = {this.Display}, Keys = {{ {string.Join(", ", this.Keys)} }} }}";
	}
}
