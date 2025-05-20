using System;
using System.Collections.Generic;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace KtaneDefuserConnector.Components;
public class ComplicatedWires : ComponentReader<ComplicatedWires.ReadData> {
	public override string Name => "Complicated Wires";

	protected internal override ReadData Process(Image<Rgba32> image, LightsState lightsState, ref Image<Rgba32>? debugImage) {
		debugImage?.Mutate(c => c.Brightness(0.5f));

		WireFlags currentFlags = 0;
		var selectionX = -1;
		var wires = new List<WireFlags>();
		var redPixels = 0;
		var bluePixels = 0;
		var wireCols = 0;
		var wireSeen = false;
		var seenHighlightBeforeWire = 0;
		var seenHighlightAfterWire = 0;
		foreach (var x in image.Width.MapRange(20, 220)) {
			var wirePixelsThisCol = 0;
			var highlightPixelsThisCol = 0;
			foreach (var y in image.Height.MapRange(160, 176)) {
				var pixel = image[x, y];
				var hsv = HsvColor.FromColor(pixel);
				var colour = GetColour(hsv, lightsState);
				if (debugImage is not null) debugImage[x, y] = colour switch { Colour.White => Color.White, Colour.Red => Color.Red, Colour.Blue => Color.Blue, Colour.Highlight => Color.Orange, _ => default };
				switch (colour) {
					case Colour.White:
						wirePixelsThisCol++;
						break;
					case Colour.Red:
						wirePixelsThisCol++;
						redPixels++;
						break;
					case Colour.Blue:
						wirePixelsThisCol++;
						bluePixels++;
						break;
					case Colour.Highlight:
						highlightPixelsThisCol++;
						break;
				}
			}

			if (wirePixelsThisCol >= 4) {
				wireCols++;
			} else {
				if (wireCols >= 4) {
					wireSeen = true;

					var slotNumber = (x * 256 / image.Width) switch { < 53 => 0, < 86 => 1, < 118 => 2, < 154 => 3, < 180 => 4, _ => 5 };
					
					if (redPixels >= 16) currentFlags |= WireFlags.Red;
					if (bluePixels >= 16) currentFlags |= WireFlags.Blue;
					
					// Read the light.
					var x2 = slotNumber switch { 0 => 35, 1 => 61, 2 => 88, 3 => 118, 4 => 145, _ => 171 } * image.Width / 256;
					var y2 = 34 * image.Height / 256;
					if (HsvColor.FromColor(image[x2, y2]).V >= 0.9f) {
						currentFlags |= WireFlags.Light;
						if (debugImage is not null) debugImage[x2, y2] = Color.Yellow;
					} else
					if (debugImage is not null) debugImage[x2, y2] = Color.Blue;

					// Read the sticker.
					var stickerRect = image.Map(slotNumber switch { 0 => 29, 1 => 63, 2 => 95, 3 => 131, 4 => 165, _ => 198 }, 208, 16, 10);
					debugImage?.Mutate(c => c.Draw(Color.Lime, 1, stickerRect));
					image.ProcessPixelRows(a => {
						for (var y = stickerRect.Top; y < stickerRect.Bottom; y++) {
							var row = a.GetRowSpan(y);
							for (var x3 = stickerRect.Left; x3 < stickerRect.Right; x3++) {
								if (!(HsvColor.FromColor(ImageUtils.ColourCorrect(row[x3], lightsState)).V < 0.3f))
									continue;
								// ReSharper disable once AccessToModifiedClosure
								currentFlags |= WireFlags.Star;
								return;
							}
						}
					});
				} else {
					wireCols = 0;
				}

				if (highlightPixelsThisCol != 0) {
					if (selectionX >= 0) continue;
					if (!wireSeen) {
						seenHighlightBeforeWire++;
					} else {
						seenHighlightAfterWire++;
						if (seenHighlightBeforeWire >= 4 && seenHighlightAfterWire >= 4)
							selectionX = wires.Count;
					}
				} else if (wireCols != 0) {
					wires.Add(currentFlags);
					currentFlags = 0;
					redPixels = 0;
					bluePixels = 0;
					wireCols = 0;
					wireSeen = false;
					seenHighlightBeforeWire = 0;
					seenHighlightAfterWire = 0;
				}
			}
		}

		return new(selectionX >= 0 ? new(selectionX, 0) : null, wires);
	}

	public record ReadData(Point? Selection, IReadOnlyList<WireFlags> Wires) : ComponentReadData(Selection) {
		public override string ToString() => $"ReadData {{ Selection = {Selection}, Wires = {string.Join("; ", Wires)} }}";
	}

	private static Colour GetColour(HsvColor hsv, LightsState lightsState) => lightsState switch {
		LightsState.Emergency => hsv switch {
			{ H: >= 6 and <= 15, S: >= 0.8f, V: >= 0.7f } => Colour.Highlight,
			{ H: >= 345 or <= 15, S: >= 0.8f, V: >= 0.5f } => Colour.Red,
			{ H: >= 210 and < 315, S: >= 0.35f, V: >= 0.15f } => Colour.Blue,
			{ S: < 0.60f, V: >= 0.85f } => Colour.White,
			_ => Colour.None
		},
		LightsState.Buzz => hsv switch {
			{ H: >= 5 and < 36, S: >= 0.60f, V: >= 0.60f } => Colour.Highlight,
			{ H: >= 330 or <= 30, S: >= 0.65f, V: >= 0.05f and < 0.30f } => Colour.Red,
			{ H: >= 210 and <= 240, S: >= 0.5f, V: >= 0.03f } => Colour.Blue,
			{ S: < 0.25f, V: >= 0.15f } => Colour.White,
			_ => Colour.None
		},
		LightsState.Off => hsv switch {
			{ H: >= 5 and < 36, S: >= 0.60f, V: >= 0.60f } => Colour.Highlight,
			{ H: >= 300 or <= 60, S: >= 0.65f, V: >= 0.01f and < 0.10f } => Colour.Red,
			{ H: >= 210 and <= 240, S: >= 0.70f, V: >= 0.01f } => Colour.Blue,
			{ S: < 0.25f, V: >= 0.02f } => Colour.White,
			_ => Colour.None
		},
		_ => hsv switch {
			{ H: >= 330 or <= 6, S: >= 0.8f, V: >= 0.5f } => Colour.Red,
			{ H: >= 330 or <= 30, S: >= 0.5f, V: >= 0.5f } => Colour.Highlight,
			{ H: >= 210 and < 240, S: >= 0.5f, V: >= 0.25f } => Colour.Blue,
			{ S: < 0.15f, V: >= 0.7f } => Colour.White,
			_ => Colour.None
		}
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
