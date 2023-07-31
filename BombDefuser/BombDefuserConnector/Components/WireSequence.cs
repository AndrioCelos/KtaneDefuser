using System;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace BombDefuserConnector.Components;
public class WireSequence : ComponentProcessor<object> {
	public override string Name => "Wire Sequence";
	protected internal override bool UsesNeedyFrame => false;

	protected internal override float IsModulePresent(Image<Rgb24> image) {
		// Wire Sequence: look for the stage indicator, wires and background
		var count = 0;
		for (var y = 96; y < 224; y++) {
			count += ImageUtils.ColorProximity(image[216, y], 32, 28, 36, 20);
		}

		var count2 = 0f;
		for (var y = 60; y < 200; y++) {
			var color = image[100, y];
			var hsv = HsvColor.FromColor(color);
			if (hsv.V < 0.1f)               // Black
				count2 += Math.Max(0, 1 - hsv.V / 0.05f);
			else if (hsv.S < 0.25) {
				// Backing
				count2 += ImageUtils.ColorProximity(color, 128, 124, 114, 96, 96, 96, 32) / 32f;
			} else if (hsv.H is >= 120 and < 300) {
				// Blue
				count2 += Math.Min(1, hsv.S * 1.25f) * Math.Max(0, 1 - Math.Abs(225 - hsv.H) * 0.05f);
			} else {
				// Red
				count2 += hsv.S * Math.Max(0, 1 - Math.Abs(hsv.H >= 180 ? hsv.H - 360 : hsv.H) * 0.05f);
			}
		}

		return count / 5120f + count2 / 280f;
	}

	protected internal override object Process(Image<Rgb24> image, ref Image<Rgb24>? debugBitmap) {
		throw new NotImplementedException();
	}
}
