using System;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace BombDefuserConnector.Components;
public class Password : ComponentProcessor<object> {
	public override string Name => "Password";
	protected internal override bool UsesNeedyFrame => false;

	protected internal override float IsModulePresent(Image<Rgb24> image) {
		// Password: look for the display in the correct Y range
		var count = 0f;
		var count2 = 0f;

		for (var y = 32; y < 224; y += 16) {
			for (var x = 24; x < 208; x += 4) {
				var pixel = image[x, y];
				var n = ImageUtils.ColorProximity(pixel, 165, 240, 10, 123, 205, 21, 60);
				if (y is > 80 and < 176)
					count += n;
				else
					count2 += n;
				count2 += Math.Max(0, count / 200 - count2 / 100);
			}
		}

		return Math.Min(1, Math.Max(0, count / 200 - count2 / 100));
	}

	protected internal override object Process(Image<Rgb24> image, ref Image<Rgb24>? debugBitmap) {
		throw new NotImplementedException();
	}
}
