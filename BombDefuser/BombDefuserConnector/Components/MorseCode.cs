using System;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace BombDefuserConnector.Components;
internal class MorseCode : ComponentProcessor<object> {
	public override string Name => "Morse Code";
	public override bool UsesNeedyFrame => false;

	public override float IsModulePresent(Image<Rgb24> image) {
		// Morse Code: look for the orange display pixels
		var count = 0f;
		for (var y = 144; y < 160; y++) {
			for (var x = 64; x < 192; x++) {
				var hsv = HsvColor.FromColor(image[x, y]);
				count += Math.Max(0, 1 - Math.Abs(hsv.H - 27) * 0.1f) * hsv.S * hsv.V;
			}
		}
		return Math.Min(count / 600, 1);
	}
	public override object Process(Image<Rgb24> image, ref Image<Rgb24> debugBitmap) {
		throw new NotImplementedException();
	}
}
