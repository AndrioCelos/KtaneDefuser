using System;
using System.Collections.Generic;
using System.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace KtaneDefuserConnector.Components;
public class PianoKeys : ComponentReader<PianoKeys.ReadData> {
	public override string Name => "Piano Keys";
	protected internal override ComponentFrameType FrameType => ComponentFrameType.Solvable;

	private static readonly Image<Rgba32>[] referenceImages = [
		LoadSampleImage(Properties.Resources.PianoKeysNatural),
		LoadSampleImage(Properties.Resources.PianoKeysFlat),
		LoadSampleImage(Properties.Resources.PianoKeysSharp),
		LoadSampleImage(Properties.Resources.PianoKeysMordent),
		LoadSampleImage(Properties.Resources.PianoKeysTurn),
		LoadSampleImage(Properties.Resources.PianoKeysCommonTime),
		LoadSampleImage(Properties.Resources.PianoKeysCutCommonTime),
		LoadSampleImage(Properties.Resources.PianoKeysFermata),
		LoadSampleImage(Properties.Resources.PianoKeysCClef)
	];

	private static Image<Rgba32> LoadSampleImage(byte[] bytes) => Image.Load<Rgba32>(bytes);

	protected internal override float IsModulePresent(Image<Rgba32> image) {
		return ImageUtils.TryFindEdges(image, new(24, 24, 160, 64), p => HsvColor.FromColor(p) is var hsv && hsv.H is >= 40 and <= 60 && hsv.S is >= 0.2f and <= 0.3f && hsv.V >= 0.8f, out var noteRect)
			? Math.Min(1, Math.Max(0, 7500 - Math.Abs(noteRect.Width * noteRect.Height - 7500)) / 4500f)
			: 0;
	}

	protected internal override ReadData Process(Image<Rgba32> image, LightsState lightsState, ref Image<Rgba32>? debugImage) {
		var labelPoints = ImageUtils.FindCorners(image, new(0, 0, 200, 100), lightsState switch {
			LightsState.Buzz => c => HsvColor.FromColor(c) is var hsv && hsv.H is >= 45 and <= 60 && hsv.S is >= 0.2f and <= 0.4f && hsv.V >= 0.1f,
			LightsState.Off => c => c.R == 7 && c.G == 7 && c.B == 7,
			LightsState.Emergency => c => HsvColor.FromColor(c) is var hsv && hsv.H is >= 15 and <= 40 && hsv.S is >= 0.2f and <= 0.4f && hsv.V >= 0.75f,
			_ => c => HsvColor.FromColor(c) is var hsv && hsv.H is >= 45 and <= 60 && hsv.S is >= 0.2f and <= 0.4f && hsv.V >= 0.75f
		}, 12);

		// Find the text bounding box.
		Predicate<Rgba32> textPredicate = lightsState switch
		{
			LightsState.Buzz => c => HsvColor.FromColor(c) is var hsv && hsv.V < 0.1f,
			LightsState.Off => c => c.R < 6,
			LightsState.Emergency => c => HsvColor.FromColor(c) is var hsv && hsv.V > 0.8f,
			_ => c => HsvColor.FromColor(c) is var hsv && hsv.V < 0.8f
		};
		var textBB = ImageUtils.FindEdges(image, new(labelPoints.TopLeft, new(labelPoints.TopRight.X - labelPoints.TopLeft.X, labelPoints.BottomLeft.Y - labelPoints.TopLeft.Y)), textPredicate);
		debugImage?.Mutate(c => c.Draw(Pens.Solid(Color.Cyan, 1), textBB));

		// Find each symbol.
		var symbols = new List<Symbol>();
		var x = textBB.Left;
		while (x < textBB.Right) {
			for (; x < textBB.Right; x++) {
				var anyPixels = Enumerable.Range(textBB.Top, textBB.Height).Any(y => textPredicate(image[x, y]));
				if (anyPixels) break;
			}
			var startX = x;
			var endX = x;
			var emptyColumnsRemaining = 8;
			// Look for 8 pixels of whitespace to avoid mistaking the space within the C clef for the gap.
			for (x++; x < textBB.Right; x++) {
				var anyPixels = Enumerable.Range(textBB.Top, textBB.Height).Any(y => textPredicate(image[x, y]));
				if (anyPixels) {
					endX = x;
					emptyColumnsRemaining = 8;
				} else {
					emptyColumnsRemaining--;
					if (emptyColumnsRemaining == 0) break;
				}
			}
			endX++;

			int top = textBB.Top, bottom = textBB.Bottom;
			image.ProcessPixelRows(p => {
				for (var y = top; y < bottom; y++) {
					var found = false;
					var r = p.GetRowSpan(y);
					for (var x = startX; x < endX; x++) {
						if (textPredicate(r[x])) {
							top = y;
							found = true;
						}
					}
					if (found) break;
				}
				for (var y = bottom - 1; y > top; y--) {
					var found = false;
					var r = p.GetRowSpan(y);
					for (var x = startX; x < endX; x++) {
						if (textPredicate(r[x])) {
							bottom = y + 1;
							found = true;
						}
					}
					if (found) break;
				}
			});

			var charImage = image.Clone(c => c.Crop(new Rectangle(startX, top, endX - startX, bottom - top)).Resize(32, 32, KnownResamplers.NearestNeighbor));
			debugImage?.Mutate(c => c.DrawImage(charImage, new Point(32 * symbols.Count, 0), 1));

			Symbol bestSymbol = 0; float bestSimilarity = 0;
			for (var i = 0; i < referenceImages.Length; i++) {
				var similarity = ImageUtils.CheckSimilarity(charImage, referenceImages[i]);
				if (similarity > bestSimilarity) {
					bestSimilarity = similarity;
					bestSymbol = (Symbol) i;
				}
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
		public override string ToString() => string.Join(' ', this.Symbols);
	}
}
