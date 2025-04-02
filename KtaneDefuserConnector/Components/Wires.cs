using System;
using System.Collections.Generic;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace KtaneDefuserConnector.Components;
public class Wires : ComponentReader<Wires.ReadData> {
	public override string Name => "Wires";
	protected internal override ComponentFrameType FrameType => ComponentFrameType.Solvable;

	protected internal override float IsModulePresent(Image<Rgba32> image) {
		// Wires: look for horizontal wires crossing the centre
		var inWire = 0;
		var numWires = 0;
		var numWirePixels = 0;
		var wirePixelTotal = 0f;
		for (var y = 32; y < 240; y++) {
			var color = image[128, y];
			var hsv = HsvColor.FromColor(color);
			var isBacking = hsv.V is >= 0.45f and <= 0.85f && hsv.S <= 0.2f && (hsv.S == 0 || hsv.H is >= 180 and <= 270);
			if (isBacking) {
				if (inWire > 0)
					inWire--;
			} else {
				if (inWire == 0)
					numWires++;
				inWire = 4;
				numWirePixels++;
				if (hsv.V < 0.1f)  // Black
					wirePixelTotal += Math.Max(0, 1 - hsv.V / 0.05f);
				else if (hsv.S < 0.15f) {
					// White
					wirePixelTotal += hsv.V;
				} else if (hsv.H is >= 30 and <= 90) {
					// Yellow
					wirePixelTotal += hsv.S * Math.Max(0, 1 - Math.Abs(60 - hsv.H) / 30);
				} else if (hsv.H is >= 120 and < 300) {
					// Blue
					wirePixelTotal += Math.Min(1, hsv.S * 1.25f) * (1 - Math.Abs(225 - hsv.H) * 0.05f);
				} else {
					// Red
					wirePixelTotal += hsv.S * Math.Max(0, 1 - Math.Abs(hsv.H >= 180 ? hsv.H - 360 : hsv.H) * 0.05f);
				}
			}
		}
		return numWires is >= 3 and <= 6 ? wirePixelTotal / numWirePixels : 0;
	}

	protected internal override ReadData Process(Image<Rgba32> image, LightsState lightsState, ref Image<Rgba32>? debugImage) {
		debugImage?.Mutate(c => c.Brightness(0.5f));

		var inWire = 0;
		var added = false;
		var colours = new List<Colour>();
		for (var y = 32; y < 232; y++) {
			var pixel = image[128, y];
			if (lightsState == LightsState.Emergency || pixel.R < 160) pixel = ImageUtils.ColourCorrect(pixel, lightsState);
			if (debugImage is not null)
				debugImage[128, y] = pixel;
			var hsv = HsvColor.FromColor(pixel);
			var colour = GetColour(hsv);
			if (colour is null) {
				if (inWire > 0) {
					inWire--;
					if (inWire == 0) {
						if (!added)
							colours.Add(Colour.Red);
						added = false;
					}
				}
			} else {
				if (!added && colour != Colour.Red) {
					added = true;
					colours.Add(colour.Value);
				}
				inWire = 4;
			}
		}
		return new([.. colours]);
	}

	private static Colour? GetColour(HsvColor hsv) {
		return hsv.V < 0.1f ? Colour.Black
			: hsv.H is >= 330 or <= 15 && hsv.S >= 0.8f && hsv.V >= 0.5f ? Colour.Red
			: hsv.H is >= 210 and < 255 && hsv.S >= 0.5f && hsv.V >= 0.35f ? Colour.Blue
			: hsv.H is >= 30 and < 90 && hsv.S >= 0.5f && hsv.V >= 0.35f ? Colour.Yellow
			: (hsv.S <= 0.2f && hsv.V >= 0.75f) || (hsv.H < 60 && hsv.S <= 0.2f) ? Colour.White
			: null;
	}

	public enum Colour {
		Red,
		Yellow,
		Blue,
		White,
		Black
	}

	public record ReadData(Colour[] Colours) {
		public override string ToString() => string.Join(' ', this.Colours);
	}
}
