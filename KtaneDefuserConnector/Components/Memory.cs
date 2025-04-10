﻿using System;
using System.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace KtaneDefuserConnector.Components;
public class Memory : ComponentReader<Memory.ReadData> {
	public override string Name => "Memory";
	protected internal override ComponentFrameType FrameType => ComponentFrameType.Solvable;

	private static readonly TextRecogniser displayRecogniser = new(new(TextRecogniser.Fonts.CabinMedium, 48), 70, 255, new(64, 64),
		"1", "2", "3", "4");
	private static readonly TextRecogniser keyRecogniser = new(new(TextRecogniser.Fonts.OstrichSansHeavy, 48), 160, 44, new(64, 64),
		"1", "2", "3", "4");

	protected internal override float IsModulePresent(Image<Rgba32> image) {
		var minDist = int.MaxValue;
		var referenceColour = new Rgba32(55, 95, 81);
		for (var x = image.Width / 4 - 10; x < image.Width / 4 + 10; x++) {
			for (var y = image.Height / 4 - 10; y < image.Height / 4 + 10; y++) {
				var pixel = image[x, y];
				var dist = Math.Abs(pixel.R - referenceColour.R) + Math.Abs(pixel.G - referenceColour.G) + Math.Abs(pixel.B - referenceColour.B);
				minDist = Math.Min(minDist, dist);
			}
		}

		// Keypad should be at about Y = 192
		var count = 0f;
		var referenceColour2 = new Rgba32(220, 196, 155);
		var referenceColour3 = new Rgba32(51, 46, 37);
		for (var x = 24; x < 172; x++) {
			var pixel = image[x, 192];
			var dist = pixel.R < 128
				? Math.Abs(pixel.R - referenceColour3.R) + Math.Abs(pixel.G - referenceColour3.G) + Math.Abs(pixel.B - referenceColour3.B)
				: Math.Abs(pixel.R - referenceColour2.R) + Math.Abs(pixel.G - referenceColour2.G) + Math.Abs(pixel.B - referenceColour2.B);
			count += Math.Max(0, 1 - dist / 20);
		}

		// And not at about Y = 128
		var count2 = 0f;
		var referenceColour4 = new Rgba32(170, 150, 120);
		for (var x = 24; x < 172; x++) {
			var pixel = image[x, 128];
			var dist = Math.Abs(pixel.R - referenceColour4.R) + Math.Abs(pixel.G - referenceColour4.G) + Math.Abs(pixel.B - referenceColour4.B);
			count2 += Math.Max(0, 1 - dist / 20);
		}

		return Math.Max(1 - (float) minDist / 50, 0) * 0.5f + Math.Max(0, count / 148 - count2 / 148) * 0.5f;
	}

	protected internal override ReadData Process(Image<Rgba32> image, LightsState lightsState, ref Image<Rgba32>? debugImage) {
		var displayBorderRect = ImageUtils.FindEdges(image, new(50, 40, 108, 80), c => c is { R: < 44, G: < 44, B: < 44 });
		displayBorderRect.Inflate(-4, -4);
		var textRect = ImageUtils.FindEdges(image, displayBorderRect, c => c.G >= 192);

		debugImage?.Mutate(c => c.Draw(Color.Red, 1, displayBorderRect).Draw(Color.Lime, 1, textRect));
		var displayText = displayRecogniser.Recognise(image, textRect);

		var keypadRect = ImageUtils.FindEdges(image, new(20, 148, 164, 72), c => HsvColor.FromColor(ImageUtils.ColourCorrect(c, lightsState)) is { H: >= 30 and <= 45, S: >= 0.2f and <= 0.4f });
		image.ColourCorrect(lightsState, keypadRect);
		debugImage?.ColourCorrect(lightsState, keypadRect);
		debugImage?.Mutate(c => c.Draw(Color.Yellow, 1, keypadRect));
		var keyLabels = new int[4];
		for (var i = 0; i < 4; i++) {
			var rect = new Rectangle(keypadRect.X + keypadRect.Width * i / 4, keypadRect.Y, keypadRect.Width / 4, keypadRect.Height);
			rect = Rectangle.Inflate(rect, -4, -4);

			// Skip over a shadow at the right edge.
			while (HsvColor.FromColor(image[rect.Right - 1, rect.Top]) is var hsv && !(hsv.H is >= 30 and <= 45 && hsv.S is >= 0.2f and <= 0.4f))
				rect.Width--;

			rect = ImageUtils.FindEdges(image, rect, c => HsvColor.FromColor(c) is { H: <= 60, V: < 0.25f });
			debugImage?.Mutate(c => c.Draw(Color.Lime, 1, rect));
			keyLabels[i] = keyRecogniser.Recognise(image, rect)[0] - '0';
		}

		return Enumerable.Range(1, 4).All(keyLabels.Contains)
			? new(ReadStageIndicator(image), displayText[0] - '0', keyLabels)
			: throw new ArgumentException("Invalid key labels found.");
	}

	public record ReadData(int StagesCleared, int Display, int[] Keys) {
		public override string ToString() => $"ReadData {{ StagesCleared = {StagesCleared}, Display = {Display}, Keys = {{ {string.Join(", ", Keys)} }} }}";
	}
}
