using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace KtaneDefuserConnector.Components;
public class ColourFlash : ComponentReader<ColourFlash.ReadData> {
	public override string Name => "Colour Flash";

	private static readonly TextRecogniser DisplayRecogniser = new(new(TextRecogniser.Fonts.OstrichSansHeavy, 24), 0, 128, new(128, 64),
		"RED", "YELLOW", "GREEN", "BLUE", "MAGENTA", "WHITE");

	protected internal override ReadData Process(Image<Rgba32> image, LightsState lightsState, ref Image<Rgba32>? debugImage) {
		var displayRect = ImageUtils.FindEdges(image, image.Map(32, 32, 144, 96), p => ImageUtils.ColourCorrect(p, lightsState) is { R: < 48, G: < 48, B: < 48 });
		debugImage?.Mutate(c => c.Draw(Color.Lime, 1, displayRect));
		displayRect.Inflate(-6, -6);

		var highlight = FindSelectionHighlight(image, lightsState, 24, 120, 232, 224);
		Point? selection = highlight.Y != 0 ? new Point(highlight.X < 128 ? 0 : 1, 0) : null;

		if (!ImageUtils.TryFindEdges(image, displayRect, p => p.R >= 20 || p.G >= 20 || p.B >= 20, out var textRect))
			return new(selection, null, Colour.None);

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
		var word = DisplayRecogniser.Recognise(image, textRect, 0, colour == Colour.White ? (byte) 255 : (byte) 128);
		return new(selection, word, colour);
	}

	public record ReadData(Point? Selection, string? Word, Colour Colour) : ComponentReadData(Selection);

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
