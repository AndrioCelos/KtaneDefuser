using System;
using KtaneDefuserConnector.DataTypes;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace KtaneDefuserConnector.Components;
public class SimonSays : ComponentReader<SimonSays.ReadData> {
	public override string Name => "Simon Says";

	protected internal override ReadData Process(Image<Rgba32> image, LightsState lightsState, ref Image<Rgba32>? debugImage) {
		int red = 0, yellow = 0, green = 0, blue = 0;
		image.ProcessPixelRows(a => {
			for (var y = 20; y < 224; y++) {
				var r = a.GetRowSpan(y);
				var left = 26 + Math.Abs(y - 122);
				var right = 230 - Math.Abs(y - 122);
				for (var x = left; x < right; x++) {
					var hsv = HsvColor.FromColor(r[x]);
					if (hsv is not { V: >= 0.75f, S: >= 0.55f }) continue;
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
		});
		return new(red >= 2500 ? SimonColour.Red : yellow >= 2500 ? SimonColour.Yellow : green >= 2500 ? SimonColour.Green : blue >= 2500 ? SimonColour.Blue : null);
	}

	public record ReadData(SimonColour? Colour);
}
