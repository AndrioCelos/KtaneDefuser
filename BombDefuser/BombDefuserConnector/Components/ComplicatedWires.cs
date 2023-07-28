using System;
using System.Collections.Generic;
using System.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace BombDefuserConnector.Components;
internal class ComplicatedWires : ComponentProcessor<object> {
	public override string Name => "Complicated Wires";
	public override bool UsesNeedyFrame => false;

	public override float IsModulePresent(Image<Rgb24> image) {
		// Complicated Wires: look for vertical wires crossing the centre
		var inWire = 0;
		var numWires = 0;
		var numWirePixels = 0;
		var wirePixelTotal = 0f;
		for (var x = 16; x < 224; x++) {
			var color = image[x, 128];
			var hsv = HsvColor.FromColor(color);
			var colour = GetColour(hsv);
			if (colour != Colour.None) {
				if (inWire == 0)
					numWires++;
				inWire = 4;
				numWirePixels++;
				wirePixelTotal += colour switch {
					Colour.White => hsv.V,
					Colour.Red or Colour.Highlight => hsv.S * Math.Max(0, 1 - Math.Abs(hsv.H >= 180 ? hsv.H - 360 : hsv.H) * 0.05f),
					Colour.Blue => Math.Min(1, hsv.S * 1.25f) * (1 - Math.Abs(225 - hsv.H) * 0.05f),
					_ => 0
				};
			} else if (inWire > 0)
				inWire--;
		}
		return numWires is >= 3 and <= 6 ? wirePixelTotal * 1.5f / numWirePixels : 0;
	}
	public override object Process(Image<Rgb24> image, ref Image<Rgb24>? debugBitmap) {
		debugBitmap?.Mutate(c => c.Brightness(0.5f));

		WireFlags? currentFlags = null;
		var inWire = 0;
		var highlight = -1;
		var wires = new List<WireFlags>();
		for (var x = 20; x < 220; x++) {
			var anyColours = false;
			for (var y = 160; y < 176; y++) {
				var pixel = image[x, y];
				var hsv = HsvColor.FromColor(pixel);
				var colour = GetColour(hsv);

				if (colour != Colour.None) {
					anyColours = true;
					if (debugBitmap is not null) debugBitmap[x, y] = colour switch { Colour.White => Color.White, Colour.Red => Color.Red, Colour.Blue => Color.Blue, Colour.Highlight => Color.Orange, _ => default };
					if (colour == Colour.Highlight) {
						if (highlight < 0 && !currentFlags.HasValue)
							highlight = wires.Count;
					} else {
						if (!currentFlags.HasValue) {
							currentFlags = 0;

							var slotNumber = x switch { < 53 => 0, < 86 => 1, < 118 => 2, < 154 => 3, < 180 => 4, _ => 5 };
							var x2 = slotNumber switch { 0 => 35, 1 => 61, 2 => 88, 3 => 118, 4 => 145, _ => 171 };
							var ledPixel = image[x2, 34];
							if (HsvColor.FromColor(image[x2, 34]).V >= 0.9f) {
								currentFlags |= WireFlags.Light;
								if (debugBitmap is not null) debugBitmap[x2, 34] = Color.Yellow;
							} else
								if (debugBitmap is not null) debugBitmap[x2, 34] = Color.Blue;

							var stickerRect = new Rectangle(slotNumber switch { 0 => 29, 1 => 63, 2 => 95, 3 => 131, 4 => 165, _ => 198 }, 208, 16, 10);
							debugBitmap?.Mutate(c => c.Draw(Color.Lime, 1, stickerRect));
							image.ProcessPixelRows(a => {
								for (var y = stickerRect.Top; y < stickerRect.Bottom; y++) {
									var row = a.GetRowSpan(y);
									for (var x = stickerRect.Left; x < stickerRect.Right; x++) {
										if (HsvColor.FromColor(row[x]).V < 0.25f) {
											currentFlags |= WireFlags.Star;
											return;
										}
									}
								}
							});
						}
						if (colour == Colour.Red) currentFlags |= WireFlags.Red;
						else if (colour == Colour.Blue) currentFlags |= WireFlags.Blue;
					}
				}
			}
			if (anyColours)
				inWire = 4;
			else if (inWire > 0) {
				inWire--;
				if (inWire == 0) {
					wires.Add(currentFlags!.Value);
					currentFlags = null;
				}
			}
		}

		return $"{(highlight >= 0 ? (highlight + 1).ToString() : "nil")} XS {string.Join("XS ", from w in wires select w == 0 ? "nil " : $"{(w.HasFlag(WireFlags.Red) ? "red " : "")}{(w.HasFlag(WireFlags.Blue) ? "blue " : "")}{(w.HasFlag(WireFlags.Star) ? "star " : "")}{(w.HasFlag(WireFlags.Light) ? "light " : "")}")}";
	}

	private static Colour GetColour(HsvColor hsv) {
		return hsv.H is >= 330 or <= 3 && hsv.S >= 0.8f && hsv.V >= 0.5f ? Colour.Red
			: hsv.H is >= 330 or <= 30 && hsv.S >= 0.5f && hsv.V >= 0.5f ? Colour.Highlight
			: hsv.H is >= 210 and < 240 && hsv.S >= 0.5f && hsv.V >= 0.35f ? Colour.Blue
			: hsv.H <= 60 && hsv.S <= 0.15f && hsv.V >= 0.5f ? Colour.White
			: Colour.None;
	}

	private enum Colour {
		None,
		White,
		Red,
		Blue,
		Highlight
	}

	[Flags]
	private enum WireFlags {
		Red = 1,
		Blue = 2,
		Star = 4,
		Light = 8
	}
}
