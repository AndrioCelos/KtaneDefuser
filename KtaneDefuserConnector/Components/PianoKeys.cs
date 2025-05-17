using System;
using System.Collections.Generic;
using System.Linq;
using KtaneDefuserConnector.Properties;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace KtaneDefuserConnector.Components;
public class PianoKeys : ComponentReader<PianoKeys.ReadData> {
	public override string Name => "Piano Keys";

	private static readonly Image<Rgba32>[] ReferenceImages = [
		LoadSampleImage(Resources.PianoKeysNatural),
		LoadSampleImage(Resources.PianoKeysFlat),
		LoadSampleImage(Resources.PianoKeysSharp),
		LoadSampleImage(Resources.PianoKeysMordent),
		LoadSampleImage(Resources.PianoKeysTurn),
		LoadSampleImage(Resources.PianoKeysCommonTime),
		LoadSampleImage(Resources.PianoKeysCutCommonTime),
		LoadSampleImage(Resources.PianoKeysFermata),
		LoadSampleImage(Resources.PianoKeysCClef)
	];

	private static Image<Rgba32> LoadSampleImage(byte[] bytes) => Image.Load<Rgba32>(bytes);

	protected internal override ReadData Process(Image<Rgba32> image, LightsState lightsState, ref Image<Rgba32>? debugImage) {
		var labelPoints = ImageUtils.FindCorners(image, image.Map(0, 0, 200, 100), lightsState switch {
			LightsState.Buzz => c => HsvColor.FromColor(c) is { H: >= 45 and <= 60, S: >= 0.2f and <= 0.4f, V: >= 0.1f },
			LightsState.Off => c => c is { R: 7, G: 7, B: 7 },
			LightsState.Emergency => c => HsvColor.FromColor(c) is { H: >= 15 and <= 40, S: >= 0.2f and <= 0.4f, V: >= 0.75f },
			_ => c => HsvColor.FromColor(c) is { H: >= 45 and <= 60, S: >= 0.2f and <= 0.4f, V: >= 0.75f }
		}, 12);

		// Find the text bounding box.
		Predicate<Rgba32> textPredicate = lightsState switch
		{
			LightsState.Buzz => c => HsvColor.FromColor(c) is { V: < 0.1f },
			LightsState.Off => c => c.R < 6,
			LightsState.Emergency => c => HsvColor.FromColor(c) is { V: > 0.8f },
			_ => c => HsvColor.FromColor(c) is { V: < 0.8f }
		};
		var textBounds = ImageUtils.FindEdges(image, new(labelPoints.TopLeft, new(labelPoints.TopRight.X - labelPoints.TopLeft.X, labelPoints.BottomLeft.Y - labelPoints.TopLeft.Y)), textPredicate);
		debugImage?.Mutate(c => c.Draw(Pens.Solid(Color.Cyan, 1), textBounds));

		// Find each symbol.
		var symbols = new List<Symbol>();
		var x = textBounds.Left;
		while (x < textBounds.Right) {
			for (; x < textBounds.Right; x++) {
				var anyPixels = Enumerable.Range(textBounds.Top, textBounds.Height).Any(y => textPredicate(image[x, y]));
				if (anyPixels) break;
			}
			var startX = x;
			var endX = x;
			var emptyColumnsRemaining = 8;
			// Look for 8 pixels of whitespace to avoid mistaking the space within the C clef for the gap.
			for (x++; x < textBounds.Right; x++) {
				var anyPixels = Enumerable.Range(textBounds.Top, textBounds.Height).Any(y => textPredicate(image[x, y]));
				if (anyPixels) {
					endX = x;
					emptyColumnsRemaining = 8;
				} else {
					emptyColumnsRemaining--;
					if (emptyColumnsRemaining == 0) break;
				}
			}
			endX++;

			int top = textBounds.Top, bottom = textBounds.Bottom;
			image.ProcessPixelRows(p => {
				for (var y = top; y < bottom; y++) {
					var found = false;
					var r = p.GetRowSpan(y);
					for (var x = startX; x < endX; x++) {
						if (!textPredicate(r[x])) continue;
						top = y;
						found = true;
					}
					if (found) break;
				}
				for (var y = bottom - 1; y > top; y--) {
					var found = false;
					var r = p.GetRowSpan(y);
					for (var x = startX; x < endX; x++) {
						if (!textPredicate(r[x])) continue;
						bottom = y + 1;
						found = true;
					}
					if (found) break;
				}
			});

			var charImage = image.Clone(c => c.Crop(new(startX, top, endX - startX, bottom - top)).Resize(32, 32, KnownResamplers.NearestNeighbor));
			debugImage?.Mutate(c => c.DrawImage(charImage, new Point(32 * symbols.Count, 0), 1));

			Symbol bestSymbol = 0; float bestSimilarity = 0;
			for (var i = 0; i < ReferenceImages.Length; i++) {
				var similarity = ImageUtils.CheckSimilarity(charImage, ReferenceImages[i]);
				if (similarity <= bestSimilarity) continue;
				bestSimilarity = similarity;
				bestSymbol = (Symbol) i;
			}
			symbols.Add(bestSymbol);
		}

		return new([.. symbols]);
	}

	public enum Symbol {
		Natural,
		Flat,
		Sharp,
		Mordent,
		Turn,
		CommonTime,
		CutCommonTime,
		Fermata,
		CClef
	}

	public record ReadData(Symbol[] Symbols) {
		public override string ToString() => string.Join(' ', Symbols);
	}
}
