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
			image.Map(24, 70, 32, 30),
			image.Map(24, 110, 32, 30),
			image.Map(24, 150, 32, 30)
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
		var highlightedButton
			= FindSelectionHighlight(image, lightsState, 72, 8, 136, 40).X != 0 ? -1
			: FindSelectionHighlight(image, lightsState, 72, 204, 136, 236).X != 0 ? 1
			: 0;

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
						if (!hsv.IsSelectionHighlightStrict(lightsState)) continue;
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

		Point? selection = highlightedWire is not null ? new(0, highlightedWire.From + 1) : highlightedButton switch { -1 => new Point(0, 0), 1 => new Point(0, colours.Length + 1), _ => null };

		return new(selection, stagesCleared, int.Parse(number), colours, highlightedWire);

		static (int x, bool isStrictMatch)? GetSelectionHighlight(Image<Rgba32> image, Rectangle textRect, LightsState lightsState) {
			var x = textRect.Right;
			var xFirstMatch = 0;
			while (true) {
				for (var y = textRect.Top; y < textRect.Bottom; y++) {
					var hsv = HsvColor.FromColor(image[x, y]);
					if (hsv.IsSelectionHighlight(lightsState)) {
						var isStrictMatch = hsv.IsSelectionHighlightStrict(lightsState);
						if (isStrictMatch) return (x, true);
						if (xFirstMatch == 0) xFirstMatch = x;
					}
					var isWireSocket = lightsState switch {
						LightsState.Emergency => hsv is { H: >= 330 or <= 30, S: <= 0.60f, V: >= 0.20f and <= 0.45f },
						LightsState.Buzz => hsv is { S: >= 0.05f and <= 0.2f, V: >= 0.01f and <= 0.07f },
						LightsState.Off => hsv.V <= 0.01f,
						_ => hsv is { H: >= 180 and <= 240, S: >= 0.05f and <= 0.2f, V: >= 0.2f and <= 0.4f }
					};
					if (isWireSocket)
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
					if (isHighlighted || !hsv.IsSelectionHighlight(lightsState)) continue;
					if (y * 256 / image.Height is < 106 or >= 144) return WireColour.Red;
					// An extra check to make sure this is a red wire and not a selection highlight crossing over the full search area from the top or bottom wire.
					// Red pixels shouldn't extend upward or downward out of the search area.
					// Also, use a narrower search area for the middle slot.
					if (x >= 72 * image.Width / 256) continue;
					int y2;
					if (y < 124 * image.Height / 256) {
						for (y2 = y; y2 >= textRect.Top; y2--) {
							if (!image[x, y2].IsSelectionHighlight(lightsState)) break;
						}
						if (y2 < textRect.Top) continue;
					} else {
						for (y2 = y; y2 < textRect.Bottom; y2++) {
							if (!image[x, y2].IsSelectionHighlight(lightsState)) break;
						}
						if (y2 >= textRect.Bottom) continue;
					}
					return WireColour.Red;
				}
			}
			return null;
		}
	}

	public record ReadData(Point? Selection, int StagesCleared, int CurrentPageFirstWireNum, WireColour?[] WireColours, HighlightedWireData? HighlightedWire) : ComponentReadData(Selection) {
		public override string ToString() => $"ReadData {{ Selection = {Selection}, StagesCleared = {StagesCleared}, CurrentPageFirstWireNum = {CurrentPageFirstWireNum}, WireColours = [ {string.Join(", ", WireColours)} ], HighlightedWireData = {HighlightedWire} }}";
	}
	public record HighlightedWireData(int From, char To);

	public enum WireColour {
		Red,
		Blue,
		Black
	}
}
