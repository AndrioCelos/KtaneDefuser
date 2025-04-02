using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace KtaneDefuserConnector.Components;
public class NeedyCapacitor : ComponentReader<NeedyCapacitor.ReadData> {
	public override string Name => "Needy Capacitor";
	protected internal override ComponentFrameType FrameType => ComponentFrameType.Needy;

	protected internal override float IsModulePresent(Image<Rgba32> image) {
		// Look for the brown lever frame.
		var count = 0;
		image.ProcessPixelRows(a => {
			for (var y = 80; y < 256; y++) {
				var row = a.GetRowSpan(y);
				for (var x = 144; x < 240; x++) {
					var hsv = HsvColor.FromColor(row[x]);
					if (hsv.H is >= 15 and <= 60 && hsv.S is >= 0.15f and <= 0.5f)
						count++;
				}
				for (var x = 16; x < 112; x++) {
					var hsv = HsvColor.FromColor(row[x]);
					if (hsv.H is >= 15 and <= 60 && hsv.S is >= 0.15f and <= 0.5f)
						count--;  // Prevent Needy Vent Gas from matching.
				}
			}
		});
		return count / 6000f;
	}
	protected internal override ReadData Process(Image<Rgba32> image, LightsState lightsState, ref Image<Rgba32>? debugImage) => new(ReadNeedyTimer(image, lightsState, debugImage));

	public record ReadData(int? Time);
}
