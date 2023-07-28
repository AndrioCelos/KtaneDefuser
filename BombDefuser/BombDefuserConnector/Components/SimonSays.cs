using System;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace BombDefuserConnector.Components;
internal class SimonnSays : ComponentProcessor<object> {
	public override string Name => "Simon Says";
	public override bool UsesNeedyFrame => false;

	public override float IsModulePresent(Image<Rgb24> image) {
		// Simon: look for the colours
		var count = 0f;
		var referenceHues = new float[] { 237, 356, 45, 108 };
		var sx = new[] { 84, 32, 140, 84 };
		var sy = new[] { 72, 125, 125, 180 };
		for (var c = 0; c < 4; c++) {
			for (var dy = 0; dy < 48; dy += 4) {
				for (var dx = 0; dx < 48; dx += 4) {
					var color = image[sx[c] + dx + dy, sy[c] - dx + dy];
					var hsv = HsvColor.FromColor(color);
					if (referenceHues[c] >= 300)
						count += Math.Max(0, 1 - Math.Abs((hsv.H < 128 ? hsv.H + 360 : hsv.H) - referenceHues[c]) / 15);
					else
						count += Math.Max(0, 1 - Math.Abs(hsv.H - referenceHues[c]) / 15);
				}
			}
		}
		return count / 576f;
	}

	public override object Process(Image<Rgb24> image, ref Image<Rgb24> debugBitmap) {
		throw new NotImplementedException();
	}
}
