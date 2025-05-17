using System;
using System.Diagnostics.CodeAnalysis;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace KtaneDefuserConnector.Components;
public class WireSequence : ComponentReader<WireSequence.ReadData> {
	public override string Name => "Wire Sequence";

	private static readonly TextRecogniser NumberRecogniser = new(new(TextRecogniser.Fonts.OstrichSansHeavy, 12), 144, 0, new(64, 64),
		"1", "4", "7", "10");

	[SuppressMessage("ReSharper", "AccessToModifiedClosure")]
	protected internal override ReadData Process(Image<Rgba32> image, LightsState lightsState, ref Image<Rgba32>? debugImage) {
		debugImage?.ColourCorrect(lightsState);
		var textRects = new[] {
			image.Map(24, 60, 32, 48),
			image.Map(24, 100, 32, 48),
			image.Map(24, 140, 32, 48)
		};

		for (var i = 0; i < 3; i++) {
			textRects[i] = ImageUtils.FindEdges(image, textRects[i], c => HsvColor.FromColor(ImageUtils.ColourCorrect(c, lightsState)) is { V: < 0.05f });
			textRects[i].Inflate(1, 1);
			debugImage?.Mutate(p => p.Draw(Color.Red, 1, textRects[i]));
		}

		var stagesCleared = ReadStageIndicator(image);
		image.ColourCorrect(lightsState, textRects[0]);
		var number = NumberRecogniser.Recognise(image, textRects[0]);

		// Find out whether either of the buttons is highlighted.
		var highlightedButton = 0;
		foreach (var y in image.Height.MapRange(8, 32)) {
			var pixel = image[106 * image.Width / 256, y];
			if (lightsState == LightsState.Emergency) pixel = ImageUtils.ColourCorrect(pixel, lightsState);
			if (!IsSelectionHighlight(HsvColor.FromColor(pixel))) continue;
			highlightedButton = -1;
			break;
		}
		if (highlightedButton == 0) {
			foreach (var y in image.Height.MapRange(216, 240)) {
				var pixel = image[106 * image.Width / 256, y];
				if (lightsState == LightsState.Emergency) pixel = ImageUtils.ColourCorrect(pixel, lightsState);
				if (!IsSelectionHighlight(HsvColor.FromColor(pixel))) continue;
				highlightedButton = 1;
				break;
			}
		}

		// For now, we can only fully read the highlighted wire.
		var colours = new WireColour?[3];
		HighlightedWireData? highlightedWire = null;
		for (var i = 0; i < 3; i++) {
			// This is highlighted if we see a red (selection highlight) pixel to the left of a grey (socket) pixel.
			var r = highlightedButton == 0 ? GetSelectionHighlight(image, textRects[i], lightsState) : null;
			if (r is not null) {
				var (x, isStrictMatch) = r.Value;
				colours[i] = GetWireColour(image, textRects[i], x, true, lightsState) ?? WireColour.Red;

				if (!isStrictMatch) continue;
				for (x = 160 * image.Width / 256; x >= image.Width / 2; x--) {
					for (var y = textRects[0].Top; y < textRects[2].Bottom; y++) {
						var hsv = HsvColor.FromColor(image[x, y]);
						if (!IsSelectionHighlightStrict(hsv, lightsState)) continue;
						var to = y < (textRects[0].Bottom + textRects[1].Top) / 2 ? 'A'
							: y < (textRects[1].Bottom + textRects[2].Top) / 2 ? 'B'
							: 'C';
						highlightedWire = new(i, to);
						break;
					}
					if (highlightedWire is not null) break;
				}
				if (highlightedWire is null) throw new ArgumentException("Can't find the end terminal of the highlighted wire.");
			} else
				colours[i] = GetWireColour(image, textRects[i], textRects[i].Right, false, lightsState);
		}

		return new(stagesCleared, int.Parse(number), colours, highlightedButton, highlightedWire);

		static bool IsSelectionHighlight(HsvColor hsv) => hsv is { H: <= 25, S: >= 0.65f, V: >= 0.5f };
		// TODO: It turns out that telling the selection highlight apart from a red wire is hard.
		// This will use a fairly strict condition to check for the selection highlight, so it will sometimes fail to match.
		// It will be necessary to look at the module multiple times until the selection highlight opacity is high enough.
		static bool IsSelectionHighlightStrict(HsvColor hsv, LightsState lightsState)
			=> lightsState is LightsState.Buzz or LightsState.Off
				? IsSelectionHighlight(hsv)
				: hsv is { H: >= 5 and <= 15, S: >= 0.95f, V: >= 0.9f };
		static (int x, bool isStrictMatch)? GetSelectionHighlight(Image<Rgba32> image, Rectangle textRect, LightsState lightsState) {
			var x = textRect.Right;
			var xFirstMatch = 0;
			while (true) {
				for (var y = textRect.Top; y < textRect.Bottom; y++) {
					var hsv = HsvColor.FromColor(image[x, y]);
					var hsvCorrected = HsvColor.FromColor(ImageUtils.ColourCorrect(image[x, y], lightsState));
					if (IsSelectionHighlight(hsv)) {
						var isStrictMatch = IsSelectionHighlightStrict(hsv, lightsState);
						if (isStrictMatch) return (x, true);
						if (xFirstMatch == 0) xFirstMatch = x;
					}
					if (lightsState == LightsState.Off || hsvCorrected is { H: >= 180 and <= 240, S: >= 0.05f and <= 0.2f, V: >= 0.2f and <= 0.4f })
						return xFirstMatch > 0 ? (xFirstMatch, false) : null;
				}
				x++;
			}
		}
		static WireColour? GetWireColour(Image<Rgba32> image, Rectangle textRect, int x, bool isHighlighted, LightsState lightsState) {
			for (; x < 76 * image.Width / 256; x++) {
				for (var y = textRect.Top; y < textRect.Bottom; y++) {
					var hsv = HsvColor.FromColor(ImageUtils.ColourCorrect(image[x, y], lightsState));
					if (hsv.V <= 0.05f)
						return WireColour.Black;
					if (hsv is { H: >= 210 and <= 240, S: >= 0.5f, V: >= 0.4f })
						return WireColour.Blue;
					// No explicit check for a red wire in the highlighted slot until we can find a way to not get a false positive from the selection highlight.
					// We know there must be a wire in this slot to highlight, so assume the wire is red if we don't find a blue or black pixel.
					if (isHighlighted || !IsSelectionHighlight(hsv)) continue;
					if (y * 256 / image.Height is < 106 or >= 144) return WireColour.Red;
					// An extra check to make sure this is a red wire and not a selection highlight crossing over the full search area from the top or bottom wire.
					// Red pixels shouldn't extend upward or downward out of the search area.
					// Also, use a narrower search area for the middle slot.
					if (x >= 72 * image.Width / 256) continue;
					int y2;
					if (y < 124 * image.Height / 256) {
						for (y2 = y; y2 >= textRect.Top; y2--) {
							if (!IsSelectionHighlight(HsvColor.FromColor(image[x, y2]))) break;
						}
						if (y2 < textRect.Top) continue;
					} else {
						for (y2 = y; y2 < textRect.Bottom; y2++) {
							if (!IsSelectionHighlight(HsvColor.FromColor(image[x, y2]))) break;
						}
						if (y2 >= textRect.Bottom) continue;
					}
					return WireColour.Red;
				}
			}
			return null;
		}
	}

	public record ReadData(int StagesCleared, int CurrentPageFirstWireNum, WireColour?[] WireColours, int HighlightedButton, HighlightedWireData? HighlightedWire) {
		public override string ToString() => $"ReadData {{ StagesCleared = {StagesCleared}, CurrentPageFirstWireNum = {CurrentPageFirstWireNum}, WireColours = [ {string.Join(", ", WireColours)} ], HighlightedButton = {HighlightedButton}, HighlightedWireData = {HighlightedWire} }}";
	}
	public record HighlightedWireData(int From, char To);

	public enum WireColour {
		Red,
		Blue,
		Black
	}
}
