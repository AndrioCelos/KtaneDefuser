using System;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace BombDefuserConnector.Widgets;
internal class BatteryHolder : WidgetProcessor {
	public override string Name => "Battery Holder";

	public override float IsWidgetPresent(Image<Rgb24> image, LightsState lightsState, PixelCounts pixelCounts)
		// This is the only widget with yellow pixels.
		=> pixelCounts.Yellow / 2048f;

	private static bool IsRed(HsvColor hsv) => hsv.S >= 0.5f && hsv.H is >= 345 or <= 60;

	public override object Process(Image<Rgb24> image, LightsState lightsState, ref Image<Rgb24>? debugBitmap) {
		debugBitmap?.Mutate(c => c.Brightness(0.5f));

		var maxCount = 0;
		for (var i = 0; i < 3; i++) {
			// Do the check at three different locations to account for the holder possibly not being centred.
			var x = 80 + 48 * i;
			var inBattery = false;
			var batteryCount = 0;
			for (var y = 32; y < 240; y++) {
				var color = image[x, y];
				if (debugBitmap != null)
					debugBitmap[x, y] = color;
				var hsv = HsvColor.FromColor(color);

				if (IsRed(hsv)) {
					if (debugBitmap != null)
						debugBitmap[x, y] = new(255, 0, 0);
					if (!inBattery) {
						batteryCount++;
						inBattery = true;
					}
				} else
					inBattery = false;
			}
			maxCount = Math.Max(maxCount, batteryCount);
		}
		return maxCount is 1 or 2 ? (object) maxCount : throw new InvalidOperationException("Invalid battery count?!");
	}
}
