﻿using System;
using System.Collections.Generic;
using System.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace KtaneDefuserConnector.Components;
public class ComplicatedWires : ComponentReader<ComplicatedWires.ReadData> {
	public override string Name => "Complicated Wires";
	protected internal override ComponentFrameType FrameType => ComponentFrameType.Solvable;

	protected internal override float IsModulePresent(Image<Rgba32> image) {
		// Complicated Wires: look for vertical wires crossing the centre
		var inWire = 0;
		var numWires = 0;
		var numPixels = 0;
		var pixelScore = 0f;
		for (var x = 16; x < 224; x++) {
			var color = image[x, 128];
			var hsv = HsvColor.FromColor(color);
			var colour = GetColour(hsv);
			if (colour != Colour.None) {
				if (inWire == 0)
					numWires++;
				inWire = 4;
				numPixels++;
				pixelScore += colour switch {
					Colour.White => hsv.V,
					Colour.Red or Colour.Highlight => hsv.S * Math.Max(0, 1 - Math.Abs(hsv.H >= 180 ? hsv.H - 360 : hsv.H) * 0.05f),
					Colour.Blue => Math.Min(1, hsv.S * 1.25f) * (1 - Math.Abs(225 - hsv.H) * 0.05f),
					_ => 0
				};
			} else {
				// There shouldn't be any colour other than the backing here.
				numPixels++;
				if (ImageUtils.IsModuleBack(color, LightsState.On)) pixelScore++;
				if (inWire > 0)
					inWire--;
			}
		}
		return numWires is >= 3 and <= 6 ? pixelScore * 1.5f / numPixels : 0;
	}
	protected internal override ReadData Process(Image<Rgba32> image, LightsState lightsState, ref Image<Rgba32>? debugImage) {
		debugImage?.Mutate(c => c.Brightness(0.5f));

		WireFlags? currentFlags = null;
		var inWire = 0;
		var highlight = -1;
		var wires = new List<WireFlags>();
		for (var x = 20; x < 220; x++) {
			var anyColours = false;
			for (var y = 160; y < 176; y++) {
				var pixel = image[x, y];
				var hsv = HsvColor.FromColor(ImageUtils.ColourCorrect(pixel, lightsState));
				var colour = GetColour(hsv);
				if (colour == Colour.None) continue;
				
				anyColours = true;
				if (debugImage is not null) debugImage[x, y] = colour switch { Colour.White => Color.White, Colour.Red => Color.Red, Colour.Blue => Color.Blue, Colour.Highlight => Color.Orange, _ => default };
				if (colour == Colour.Highlight) {
					if (highlight < 0 && !currentFlags.HasValue)
						highlight = wires.Count;
				} else {
					if (!currentFlags.HasValue) {
						currentFlags = 0;

						var slotNumber = x switch { < 53 => 0, < 86 => 1, < 118 => 2, < 154 => 3, < 180 => 4, _ => 5 };
						var x2 = slotNumber switch { 0 => 35, 1 => 61, 2 => 88, 3 => 118, 4 => 145, _ => 171 };
						if (HsvColor.FromColor(image[x2, 34]).V >= 0.9f) {
							currentFlags |= WireFlags.Light;
							if (debugImage is not null) debugImage[x2, 34] = Color.Yellow;
						} else
						if (debugImage is not null) debugImage[x2, 34] = Color.Blue;

						var stickerRect = new Rectangle(slotNumber switch { 0 => 29, 1 => 63, 2 => 95, 3 => 131, 4 => 165, _ => 198 }, 208, 16, 10);
						debugImage?.Mutate(c => c.Draw(Color.Lime, 1, stickerRect));
						image.ProcessPixelRows(a => {
							for (var y = stickerRect.Top; y < stickerRect.Bottom; y++) {
								var row = a.GetRowSpan(y);
								for (var x = stickerRect.Left; x < stickerRect.Right; x++) {
									if (!(HsvColor.FromColor(ImageUtils.ColourCorrect(row[x], lightsState)).V < 0.3f))
										continue;
									currentFlags |= WireFlags.Star;
									return;
								}
							}
						});
					}

					switch (colour) {
						case Colour.Red:
							currentFlags |= WireFlags.Red;
							break;
						case Colour.Blue:
							currentFlags |= WireFlags.Blue;
							break;
					}
				}
			}
			if (anyColours)
				inWire = 4;
			else if (inWire > 0) {
				inWire--;
				if (inWire != 0) continue;
				wires.Add(currentFlags!.Value);
				currentFlags = null;
			}
		}
		if (currentFlags is not null)
			wires.Add(currentFlags.Value);

		return new(highlight >= 0 ? highlight : null, wires);
	}

	public record ReadData(int? CurrentWire, IReadOnlyList<WireFlags> Wires) {
		public override string ToString() => $"{(CurrentWire.HasValue ? (CurrentWire + 1).ToString() : "nil")} XS {string.Join("XS ", from w in Wires select w == 0 ? "nil " : $"{(w.HasFlag(WireFlags.Red) ? "red " : "")}{(w.HasFlag(WireFlags.Blue) ? "blue " : "")}{(w.HasFlag(WireFlags.Star) ? "star " : "")}{(w.HasFlag(WireFlags.Light) ? "light " : "")}")}";
	}

	private static Colour GetColour(HsvColor hsv) => hsv switch {
		{ H: >= 330 or <= 3, S: >= 0.8f, V: >= 0.5f } => Colour.Red,
		{ H: >= 330 or <= 30, S: >= 0.5f, V: >= 0.5f } => Colour.Highlight,
		{ H: >= 210 and < 240, S: >= 0.5f, V: >= 0.35f } => Colour.Blue,
		{ S: <= 0.15f, V: >= 0.7f } => Colour.White,
		_ => Colour.None
	};

	private enum Colour {
		None,
		White,
		Red,
		Blue,
		Highlight
	}

	[Flags]
	public enum WireFlags {
		None,
		Red = 1,
		Blue = 2,
		Star = 4,
		Light = 8
	}
}
