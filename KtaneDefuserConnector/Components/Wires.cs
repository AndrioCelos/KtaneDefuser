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
		var highlight = -1;
		foreach (var y in image.Height.MapRange(24, 232)) {
			var pixel = image[image.Width / 2, y];
			var hsv = HsvColor.FromColor(pixel);
			var colour = GetColour(hsv, lightsState);
			if (colour == Colour.None) {
				seenFirstValidPixel = false;
				if (inWire <= 0) continue;
				inWire--;
				if (inWire != 0) continue;
				if (!added)
					colours.Add(Colour.Red);
				added = false;
			} else if (colour == Colour.Highlight) {
				if (debugImage is not null) debugImage[debugImage.Width / 2, y] = Color.Orange;
				if (highlight < 0) highlight = colours.Count;
			} else {
				if (debugImage is not null) debugImage[debugImage.Width / 2, y] = colour switch { Colour.Black => Color.DimGrey, Colour.White => Color.White, Colour.Red => Color.Red, Colour.Yellow => Color.Yellow, Colour.Blue => Color.Blue, _ => default };
				if (!seenFirstValidPixel) {
					// Filter out single pixels.
					seenFirstValidPixel = true;
					continue;
				}
				if (!added && colour != Colour.Red) {
					added = true;
					colours.Add(colour);
				}
				inWire = 4;
			}
		}
		return new(highlight < 0 ? null : new(0, highlight), [.. colours]);
	}

	private static Colour GetColour(HsvColor hsv, LightsState lightsState) => lightsState switch {
		LightsState.Emergency => hsv switch {
			{ V: < 0.05f } => Colour.Black,
			{ H: >= 6 and <= 15, S: >= 0.8f, V: >= 0.7f } => Colour.Highlight,
			{ H: >= 345 or <= 15, S: >= 0.8f, V: >= 0.5f } => Colour.Red,
			{ H: >= 17 and < 60, S: >= 0.5f, V: >= 0.5f } => Colour.Yellow,
			{ H: >= 210 and < 315, S: >= 0.35f, V: >= 0.15f } => Colour.Blue,
			{ S: < 0.60f, V: >= 0.85f } => Colour.White,
			_ => Colour.None
		},
		LightsState.Buzz => hsv switch {
			{ V: < 0.02f } => Colour.Black,
			{ H: >= 5 and < 36, S: >= 0.60f, V: >= 0.60f } => Colour.Highlight,
			{ H: >= 330 or <= 30, S: >= 0.65f, V: >= 0.05f and < 0.30f } => Colour.Red,
			{ H: >= 45 and < 75, S: >= 0.5f } => Colour.Yellow,
			{ H: >= 210 and <= 240, S: >= 0.5f } => Colour.Blue,
			{ S: < 0.25f, V: >= 0.15f } => Colour.White,
			_ => Colour.None
		},
		LightsState.Off => hsv switch {
			{ V: < 0.01f } => Colour.Black,
			{ H: >= 5 and < 36, S: >= 0.60f, V: >= 0.60f } => Colour.Highlight,
			{ H: >= 300 or <= 60, S: >= 0.65f, V: >= 0.01f and < 0.10f } => Colour.Red,
			{ H: >= 45 and < 75, S: >= 0.5f } => Colour.Yellow,
			{ H: >= 210 and <= 240, S: >= 0.70f, V: >= 0.01f } => Colour.Blue,
			{ S: < 0.25f, V: >= 0.02f } => Colour.White,
			_ => Colour.None
		},
		_ => hsv switch {
			{ V: < 0.05f } => Colour.Black,
			{ H: >= 330 or <= 3, S: >= 0.8f, V: >= 0.5f } => Colour.Red,
			{ H: >= 330 or <= 30, S: >= 0.5f, V: >= 0.5f } => Colour.Highlight,
			{ H: >= 45 and < 75, S: >= 0.5f, V: >= 0.5f } => Colour.Yellow,
			{ H: >= 210 and < 240, S: >= 0.5f, V: >= 0.25f } => Colour.Blue,
			{ S: < 0.15f, V: >= 0.7f } => Colour.White,
			_ => Colour.None
		}
	};

	public enum Colour {
		None,
		Black,
		White,
		Red,
		Yellow,
		Blue,
		Highlight
	}
	public record ReadData(Point? Selection, Colour[] Colours) : ComponentReadData(Selection) {
		public override string ToString() => $"ReadData {{ Selection = {Selection}, Colours = {string.Join(' ', Colours)} }}";
	}
}
