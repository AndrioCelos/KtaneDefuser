using System;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace BombDefuserConnector.Components;
public class Maze : ComponentProcessor<object> {
	public override string Name => "Maze";
	protected internal override bool UsesNeedyFrame => false;

	protected internal override float IsModulePresent(Image<Rgb24> image) {
		// Maze: look for the display
		var count = 0;
		for (var y = 60; y < 208; y++) {
			for (var x = 40; x < 180; x++) {
				var color = image[x, y];
				if (color.R >= 128) count += ImageUtils.ColorProximity(color, 214, 0, refB: 0, 40);
				else {
					count += ImageUtils.ColorProximity(color, 5, 12, 33, 24, 70, 90, 40);
				}
			}
		}

		return count / 828800f;
	}

	protected internal override object Process(Image<Rgb24> image, ref Image<Rgb24>? debugBitmap) {
		throw new NotImplementedException();
	}
}
