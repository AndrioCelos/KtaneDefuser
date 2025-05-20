using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace KtaneDefuserConnector.Components;
public class NeedyVentGas : ComponentReader<NeedyVentGas.ReadData> {
	public override string Name => "Needy Vent Gas";

	private static readonly TextRecogniser DisplayRecogniser = new(new(TextRecogniser.Fonts.CabinMedium, 24), 0, 128, new(256, 64),
		"VENT GAS?", "DETONATE?");

	protected internal override ReadData Process(Image<Rgba32> image, LightsState lightsState, ref Image<Rgba32>? debugImage) {
		var highlight = FindSelectionHighlight(image, lightsState, 88, 172, 172, 216);
		Point? selection = highlight.Y != 0 ? new Point(highlight.X < 112 ? 0 : 1, 0) : null;

		var time = ReadNeedyTimer(image, lightsState, debugImage);

		int top = 0, bottom = 0;
		image.ProcessPixelRows(a => {
			for (top = image.Height / 4; top < image.Height / 2; top++) {
				var r = a.GetRowSpan(top);
				foreach (var x in image.Width.MapRange(112, 144)) {
					if (HsvColor.FromColor(r[x]) is { H: >= 90 and <= 135, S: >= 0.75f, V: >= 0.5f })
						return;
				}
			}
		});

		if (top >= image.Height / 2) return new(selection, null, null);
		top--;  // For the height of the '?'.

		image.ProcessPixelRows(a => {
			for (bottom = top + 8; bottom < 144 * image.Height / 256; bottom++) {
				var r = a.GetRowSpan(bottom);
				var found = false;
				foreach (var x in image.Width.MapRange(112, 144)) {
					if (HsvColor.FromColor(r[x]) is not { H: >= 90 and <= 135, S: >= 0.75f, V: >= 0.5f }) continue;
					found = true;
					break;
				}
				if (!found) return;
			}
		});
		bottom--;

		var textRect = ImageUtils.FindEdges(image, new(image.Height / 4, top, image.Height / 2, bottom - top), c => HsvColor.FromColor(c) is { H: >= 90 and <= 135, V: >= 0.5f });
		debugImage?.Mutate(c => c.Draw(Color.Cyan, 1, textRect));

		return new(selection, time, DisplayRecogniser.Recognise(image, textRect));
	}

	public record ReadData(Point? Selection, int? Time, string? Message) : ComponentReadData(Selection);
}
