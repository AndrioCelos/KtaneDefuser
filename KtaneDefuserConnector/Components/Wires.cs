using System.Collections.Generic;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace KtaneDefuserConnector.Components;
public class Wires : ComponentReader<Wires.ReadData> {
	public override string Name => "Wires";

	protected internal override ReadData Process(Image<Rgba32> image, LightsState lightsState, ref Image<Rgba32>? debugImage) {
		debugImage?.Mutate(c => c.Brightness(0.5f));

		var inWire = 0;
		var seenFirstValidPixel = false;
		var added = false;
		var colours = new List<Colour>();
		foreach (var y in image.Height.MapRange(24, 232)) {
			var pixel = image[image.Width / 2, y];
			if (lightsState == LightsState.Emergency || pixel.R < 160) pixel = ImageUtils.ColourCorrect(pixel, lightsState);
			if (debugImage is not null)
				debugImage[image.Width / 2, y] = pixel;
			var hsv = HsvColor.FromColor(pixel);
			var colour = GetColour(hsv);
			if (colour is null) {
				seenFirstValidPixel = false;
				if (inWire > 0) {
					inWire--;
					if (inWire == 0) {
						if (!added)
							colours.Add(Colour.Red);
						added = false;
					}
				}
			} else {
				if (!seenFirstValidPixel) {
					// Filter out single pixels.
					seenFirstValidPixel = true;
					continue;
				}
				if (!added && colour != Colour.Red) {
					added = true;
					colours.Add(colour.Value);
				}
				inWire = 4;
			}
		}
		return new([.. colours]);
	}

	private static Colour? GetColour(HsvColor hsv) => hsv switch {
		{ V: < 0.1f } => Colour.Black,
		{ H: >= 330 or <= 15, S: >= 0.8f, V: >= 0.5f } => Colour.Red,
		{ H: >= 210 and < 255, S: >= 0.5f, V: >= 0.35f } => Colour.Blue,
		{ H: >= 30 and < 90, S: >= 0.5f, V: >= 0.35f } => Colour.Yellow,
		{ S: <= 0.2f, V: >= 0.75f } or { H: < 60, S: <= 0.2f } => Colour.White,
		_ => null
	};

	public enum Colour {
		Red,
		Yellow,
		Blue,
		White,
		Black
	}

	public record ReadData(Colour[] Colours) {
		public override string ToString() => string.Join(' ', Colours);
	}
}
