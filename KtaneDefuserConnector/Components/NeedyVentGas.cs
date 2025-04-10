using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace KtaneDefuserConnector.Components;
public class NeedyVentGas : ComponentReader<NeedyVentGas.ReadData> {
	public override string Name => "Needy Vent Gas";
	protected internal override ComponentFrameType FrameType => ComponentFrameType.Needy;

	private static readonly TextRecogniser displayRecogniser = new(new(TextRecogniser.Fonts.CabinMedium, 24), 0, 128, new(256, 64),
		"VENT GAS?", "DETONATE?");

	protected internal override float IsModulePresent(Image<Rgba32> image) {
		// Look for the brown lever frame.
		var count = 0;
		image.ProcessPixelRows(a => {
			for (var y = 80; y < 256; y++) {
				var row = a.GetRowSpan(y);
				for (var x = 16; x < 240; x++) {
					if (HsvColor.FromColor(row[x]) is { H: >= 15 and <= 60, S: >= 0.15f and <= 0.5f } or { H: >= 90 and <= 135, S: >= 0.25f })
						count++;
				}
			}
		});
		return count / 36000f;
	}
	protected internal override ReadData Process(Image<Rgba32> image, LightsState lightsState, ref Image<Rgba32>? debugImage) {
		var time = ReadNeedyTimer(image, lightsState, debugImage);
		if (time == null) return new(null, null);

		int top = 0, bottom = 0;
		image.ProcessPixelRows(a => {
			for (top = 64; top < 128; top++) {
				var r = a.GetRowSpan(top);
				for (var x = 112; x < 144; x++) {
					if (HsvColor.FromColor(r[x]) is { H: >= 90 and <= 135, S: >= 0.75f, V: >= 0.5f })
						return;
				}
			}
		});
		top--;  // For the height of the '?'.

		if (top >= 128) return new(null, null);

		image.ProcessPixelRows(a => {
			for (bottom = top + 8; bottom < 144; bottom++) {
				var r = a.GetRowSpan(bottom);
				var found = false;
				for (var x = 112; x < 144; x++) {
					if (HsvColor.FromColor(r[x]) is not { H: >= 90 and <= 135, S: >= 0.75f, V: >= 0.5f }) continue;
					found = true;
					break;
				}
				if (!found) return;
			}
		});
		bottom--;

		var textRect = ImageUtils.FindEdges(image, new(64, top, 128, bottom - top), c => HsvColor.FromColor(c) is { H: >= 90 and <= 135, V: >= 0.5f });
		debugImage?.Mutate(c => c.Draw(Color.Cyan, 1, textRect));
		return new(time, displayRecogniser.Recognise(image, textRect));
	}

	public record ReadData(int? Time, string? Message);
}
