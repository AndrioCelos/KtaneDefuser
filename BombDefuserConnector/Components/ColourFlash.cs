using System;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace BombDefuserConnector.Components;
public class ColourFlash : ComponentReader<ColourFlash.ReadData> {
	public override string Name => "Colour Flash";
	protected internal override ComponentFrameType FrameType => ComponentFrameType.Solvable;

	private static readonly TextRecogniser displayRecogniser = new(new(TextRecogniser.Fonts.OSTRICH_SANS_HEAVY, 24), 0, 128, new(128, 64),
		"RED", "YELLOW", "GREEN", "BLUE", "MAGENTA", "WHITE");

	protected internal override float IsModulePresent(Image<Rgba32> image) {
		// Colour Flash: Try to find the display and keys.
		return ImageUtils.TryFindEdges(image, new(32, 32, 160, 96), p => p.R < 32 && p.G < 32 && p.B < 32, out var displayRect)
			&& ImageUtils.TryFindEdges(image, new(24, 128, 100, 100), p => HsvColor.FromColor(p) is var hsv && hsv.H is >= 30 and <= 60 && hsv.S <= 0.25f && hsv.V >= 0.75f, out var keyRect1)
			&& ImageUtils.TryFindEdges(image, new(128, 128, 100, 100), p => HsvColor.FromColor(p) is var hsv && hsv.H is >= 30 and <= 60 && hsv.S <= 0.25f && hsv.V >= 0.75f, out var keyRect2)
			? Math.Min(1, Math.Max(0, 9000 - Math.Abs(displayRect.Width * displayRect.Height - 9000)) / 6000f)
				* Math.Min(1, Math.Max(0, 5000 - Math.Abs(keyRect1.Width * keyRect1.Height - 5000)) / 3000f)
				* Math.Min(1, Math.Max(0, 5000 - Math.Abs(keyRect2.Width * keyRect2.Height - 5000)) / 3000f)
			: 0;
	}
	protected internal override ReadData Process(Image<Rgba32> image, LightsState lightsState, ref Image<Rgba32>? debugImage) {
		var displayRect = ImageUtils.FindEdges(image, new(32, 32, 144, 96), p => ImageUtils.ColourCorrect(p, lightsState) is var p2 && p2.R < 48 && p2.G < 48 && p2.B < 48);
		debugImage?.Mutate(c => c.Draw(Color.Lime, 1, displayRect));
		displayRect.Inflate(-6, -6);

		if (!ImageUtils.TryFindEdges(image, displayRect, p => p.R >= 20 || p.G >= 20 || p.B >= 20, out var textRect))
			return new(null, Colour.None);

		debugImage?.Mutate(c => c.Draw(Color.Cyan, 1, textRect));
		displayRect.Inflate(-6, -6);

		int r = 0, g = 0, b = 0;
		image.ProcessPixelRows(a => {
			for (var y = textRect.Top; y < textRect.Bottom; y++) {
				var row = a.GetRowSpan(y);
				for (var x = textRect.Left; x < textRect.Right; x++) {
					var p = row[x];
					r += p.R;
					g += p.G;
					b += p.B;
				}
			}
		});
		var threshold = (r + g + b) / 6;
		var colour =
			r >= threshold
			? g >= threshold
				? b >= threshold ? Colour.White : Colour.Yellow
				: b >= threshold ? Colour.Magenta : Colour.Red
			: g >= threshold ? Colour.Green : Colour.Blue;
		var word = displayRecogniser.Recognise(image, textRect, 0, colour == Colour.White ? (byte) 255 : (byte) 128);
		return new(word, colour);
	}

	public record ReadData(string? Word, Colour Colour);

	public enum Colour {
		None,
		Red,
		Yellow,
		Green,
		Blue,
		Magenta,
		White
	}
}
