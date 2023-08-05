using System;
using System.Collections.Generic;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace BombDefuserConnector.Components;
public class Wires : ComponentReader<Wires.ReadData> {
	public override string Name => "Wires";
	protected internal override bool UsesNeedyFrame => false;

	protected internal override float IsModulePresent(Image<Rgb24> image) {
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

	protected internal override ReadData Process(Image<Rgb24> image, ref Image<Rgb24>? debugImage) {
		debugImage?.Mutate(c => c.Brightness(0.5f));

		var inWire = 0;
		var added = false;
		var colours = new List<Colour>();
		for (var y = 32; y < 240; y++) {
			var color = image[128, y];
			if (debugImage is not null)
				debugImage[128, y] = color;
			var hsv = HsvColor.FromColor(color);
			var isBacking = hsv.V is >= 0.45f and <= 0.85f && hsv.S <= 0.2f && (hsv.S == 0 || hsv.H is >= 180 and <= 270);
			if (isBacking) {
				if (inWire > 0) {
					inWire--;
					if (inWire == 0) {
						if (!added)
							colours.Add(Colour.Red);
						added = false;
					}
				}
			} else {
				if (!added) {
					if (hsv.V < 0.1f) {
						added = true;
						colours.Add(Colour.Black);
					} else if (hsv.S < 0.15f) {
						added = true;
						colours.Add(Colour.White);
					} else if (hsv.H is >= 30 and <= 90) {
						added = true;
						colours.Add(Colour.Yellow);
					} else if (hsv.H is >= 120 and < 300) {
						added = true;
						colours.Add(Colour.Blue);
					}
				}
				inWire = 4;
			}
		}
		return new(colours.ToArray());
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
