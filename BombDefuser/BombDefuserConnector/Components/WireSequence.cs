using System;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace BombDefuserConnector.Components;
public class WireSequence : ComponentProcessor<WireSequence.ReadData> {
	public override string Name => "Wire Sequence";
	protected internal override bool UsesNeedyFrame => false;

	private static readonly TextRecogniser numberRecogniser = new(new(TextRecogniser.Fonts.OSTRICH_SANS_HEAVY, 12), 144, 0, new(64, 64),
		"1", "4", "7", "10");

	protected internal override float IsModulePresent(Image<Rgb24> image) {
		// Wire Sequence: look for the stage indicator, wires and background
		var count = 0;
		for (var y = 96; y < 224; y++) {
			count += ImageUtils.ColorProximity(image[216, y], 32, 28, 36, 20);
		}

		var count2 = 0f;
		for (var y = 60; y < 200; y++) {
			var color = image[100, y];
			var hsv = HsvColor.FromColor(color);
			if (hsv.V < 0.1f)
				// Black
				count2 += Math.Max(0, 1 - hsv.V / 0.05f);
			else if (hsv.S < 0.25) {
				// Backing
				count2 += ImageUtils.ColorProximity(color, 128, 124, 114, 96, 96, 96, 32) / 32f;
			} else if (hsv.H is >= 120 and < 300) {
				// Blue
				count2 += Math.Min(1, hsv.S * 1.25f) * Math.Max(0, 1 - Math.Abs(225 - hsv.H) * 0.05f);
			} else {
				// Red
				count2 += hsv.S * Math.Max(0, 1 - Math.Abs(hsv.H >= 180 ? hsv.H - 360 : hsv.H) * 0.05f);
			}
		}

		return count / 5120f + count2 / 280f;
	}

	protected internal override ReadData Process(Image<Rgb24> image, ref Image<Rgb24>? debugBitmap) {
		var textRects = new[] {
			new Rectangle(24, 60, 32, 48),
			new Rectangle(24, 100, 32, 48),
			new Rectangle(24, 140, 32, 48)
		};

		for (var i = 0; i < 3; i++) {
			textRects[i] = ImageUtils.FindEdges(image, textRects[i], c => HsvColor.FromColor(c) is HsvColor hsv && hsv.V < 0.05f);
			textRects[i].Inflate(1, 1);
			debugBitmap?.Mutate(p => p.Draw(Color.Red, 1, textRects[i]));
		}

		var stagesCleared = ReadStageIndicator(image);
		var number = numberRecogniser.Recognise(image, textRects[0]);

		// TODO: It turns out that telling the selection highlight apart from a red wire is hard.
		// This will use a fairly strict condition to check for the selection highlight, so it will sometimes fail to match.
		// It will be necessary to look at the module multiple times until the selection highlight opacity is high enough.
		static bool isSelectionHighlight(HsvColor hsv) => hsv.H is >= 5 and <= 15 && hsv.S >= 0.93f && hsv.V >= 0.9f;
		static int? getSelectionHighlight(Image<Rgb24> image, Rectangle textRect) {
			var x = textRect.Right;
			while (true) {
				for (var y = textRect.Top; y < textRect.Bottom; y++) {
					var hsv = HsvColor.FromColor(image[x, y]);
					if (isSelectionHighlight(hsv))
						return x;
					if (hsv.H is >= 180 and <= 240 && hsv.S is >= 0.05f and <= 0.15f && hsv.V is >= 0.2f and <= 0.4f)
						return null;
				}
				x++;
			}
		}
		static WireColour getHighlightedWireColour(Image<Rgb24> image, Rectangle textRect, int x) {
			for (; x < 80; x++) {
				for (var y = textRect.Top; y < textRect.Bottom; y++) {
					var hsv = HsvColor.FromColor(image[x, y]);
					if (hsv.V <= 0.05f)
						return WireColour.Black;
					if (hsv.H is >= 210 and <= 240 && hsv.S is >= 0.5f && hsv.V >= 0.4f)
						return WireColour.Blue;
					// No explicit check for a red wire until we can find a way to not get a false positive from the selection highlight.
					// We know there must be a wire in this slot to highlight, so assume the wire is red if we don't find a blue or black pixel.
				}
			}
			return WireColour.Red;
		}

		// For now, we can only read the highlighted wire.
		for (var i = 0; i < 3; i++) {
			// This is selected if we see a red (selection highlight) pixel to the left of a grey (socket) pixel.
			var r = getSelectionHighlight(image, textRects[i]);
			if (r is not null) {
				var x = r.Value;
				var colour = getHighlightedWireColour(image, textRects[i], x);

				for (x = 160; x >= 128; x--) {
					for (var y = textRects[0].Top; y < textRects[2].Bottom; y++) {
						var hsv = HsvColor.FromColor(image[x, y]);
						if (isSelectionHighlight(hsv)) {
							var to = y < (textRects[0].Bottom + textRects[1].Top) / 2 ? 'A'
								: y < (textRects[1].Bottom + textRects[2].Top) / 2 ? 'B'
								: 'C';
							return new(stagesCleared, int.Parse(number), new(colour, i, to));
						}
					}
				}
				throw new ArgumentException("Can't find the end terminal of the highlighted wire.");
			}
		}

		return new(stagesCleared, int.Parse(number), null);
	}

	public record ReadData(int StagesCleared, int CurrentPageFirstWireNum, SelectedWireData? SelectedWire);
	public record SelectedWireData(WireColour Colour, int From, char To);

	public enum WireColour {
		Red,
		Blue,
		Black
	}
}
