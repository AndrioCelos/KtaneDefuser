using System;
using BombDefuserConnector.DataTypes;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace BombDefuserConnector.Components;
public class SimonSays : ComponentReader<SimonSays.ReadData> {
	public override string Name => "Simon Says";
	protected internal override bool UsesNeedyFrame => false;

	protected internal override float IsModulePresent(Image<Rgba32> image) {
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

	protected internal override ReadData Process(Image<Rgba32> image, ref Image<Rgba32>? debugImage) {
		int red = 0, yellow = 0, green = 0, blue = 0;
		image.ProcessPixelRows(a => {
			for (var y = 20; y < 224; y++) {
				var r = a.GetRowSpan(y);
				var left = 26 + Math.Abs(y - 122);
				var right = 230 - Math.Abs(y - 122);
				for (var x = left; x < right; x++) {
					var hsv = HsvColor.FromColor(r[x]);
					if (hsv.V >= 0.75f && hsv.S >= 0.55f) {
						var rx = y + x;
						var ry = y - x;
						if (ry < -6) {
							if (rx < 248) {
								if (hsv.H is >= 180 and <= 240) blue++;
							} else {
								if (hsv.H is >= 30 and <= 90) yellow++;
							}
						} else {
							if (rx < 248) {
								if (hsv.H is >= 330 or <= 30) red++;
							} else {
								if (hsv.H is >= 90 and <= 180) green++;
							}
						}
					}
				}
			}
		});
		return new(red >= 2500 ? SimonColour.Red : yellow >= 2500 ? SimonColour.Yellow : green >= 2500 ? SimonColour.Green : blue >= 2500 ? SimonColour.Blue : null);
	}

	public record ReadData(SimonColour? Colour);
}
