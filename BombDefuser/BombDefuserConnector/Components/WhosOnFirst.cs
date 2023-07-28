using System;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace BombDefuserConnector.Components;
internal class WhosOnFirst : ComponentProcessor<object> {
	public override string Name => "Who's on First";
	public override bool UsesNeedyFrame => false;

	public override float IsModulePresent(Image<Rgb24> image) {
		// Who's on First: look for the display and keys
		var referenceColour = new Rgb24(71, 91, 104);
		var referenceColour2 = new Rgb24(170, 150, 120);
		var referenceColour3 = new Rgb24(51, 46, 37);
		var count = 0f;
		var count2 = 0f;

		for (var x = 48; x < 144; x++) {
			var pixel = image[x, 48];
			var dist = Math.Abs(pixel.R - referenceColour.R) + Math.Abs(pixel.G - referenceColour.G) + Math.Abs(pixel.B - referenceColour.B);
			count += Math.Max(0, 1 - dist / 40f);
		}

		for (var y = 96; y < 224; y += 4) {
			for (var x = 32; x < 172; x += 4) {
				var pixel = image[x, y];
				var dist = pixel.R < 64
					? Math.Abs(pixel.R - referenceColour3.R) + Math.Abs(pixel.G - referenceColour3.G) + Math.Abs(pixel.B - referenceColour3.B)
					: Math.Abs(pixel.R - referenceColour2.R) + Math.Abs(pixel.G - referenceColour2.G) + Math.Abs(pixel.B - referenceColour2.B);
				count2 += Math.Max(0, 1 - dist / 80f);
			}
		}

		return count / 192 + count2 / 2240;
	}
	public override object Process(Image<Rgb24> image, ref Image<Rgb24> debugBitmap) {
		throw new NotImplementedException();
	}
}
