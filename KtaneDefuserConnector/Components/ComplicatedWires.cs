using System;
using System.Collections.Generic;
using System.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace KtaneDefuserConnector.Components;
public class ComplicatedWires : ComponentReader<ComplicatedWires.ReadData> {
	public override string Name => "Complicated Wires";

	protected internal override ReadData Process(Image<Rgba32> image, LightsState lightsState, ref Image<Rgba32>? debugImage) {
		debugImage?.Mutate(c => c.Brightness(0.5f));

		WireFlags? currentFlags = null;
		var inWire = 0;
		var highlight = -1;
		var highlightPixels = 0;
		var wires = new List<WireFlags>();
		foreach (var x in image.Width.MapRange(20, 220)) {
			var anyColours = false;
			foreach (var y in image.Height.MapRange(160, 176)) {
				var pixel = image[x, y];
				var hsv = HsvColor.FromColor(ImageUtils.ColourCorrect(pixel, lightsState));
				var colour = GetColour(hsv);
				if (colour == Colour.None) continue;
				
				anyColours = true;
				if (debugImage is not null) debugImage[x, y] = colour switch { Colour.White => Color.White, Colour.Red => Color.Red, Colour.Blue => Color.Blue, Colour.Highlight => Color.Orange, _ => default };
				if (colour == Colour.Highlight) {
					if (highlight >= 0 || currentFlags.HasValue) continue;
					highlightPixels++;
					if (highlightPixels >= 4)
						highlight = wires.Count;
				} else {
					highlightPixels = 0;
					if (!currentFlags.HasValue) {
						currentFlags = 0;

						var slotNumber = (x * 256 / image.Width) switch { < 53 => 0, < 86 => 1, < 118 => 2, < 154 => 3, < 180 => 4, _ => 5 };
						var x2 = slotNumber switch { 0 => 35, 1 => 61, 2 => 88, 3 => 118, 4 => 145, _ => 171 } * image.Width / 256;
						var y2 = 34 * image.Height / 256;
						if (HsvColor.FromColor(image[x2, y2]).V >= 0.9f) {
							currentFlags |= WireFlags.Light;
							if (debugImage is not null) debugImage[x2, y2] = Color.Yellow;
						} else
						if (debugImage is not null) debugImage[x2, y2] = Color.Blue;

						var stickerRect = image.Map(slotNumber switch { 0 => 29, 1 => 63, 2 => 95, 3 => 131, 4 => 165, _ => 198 }, 208, 16, 10);
						debugImage?.Mutate(c => c.Draw(Color.Lime, 1, stickerRect));
						image.ProcessPixelRows(a => {
							for (var y = stickerRect.Top; y < stickerRect.Bottom; y++) {
								var row = a.GetRowSpan(y);
								for (var x = stickerRect.Left; x < stickerRect.Right; x++) {
									if (!(HsvColor.FromColor(ImageUtils.ColourCorrect(row[x], lightsState)).V < 0.3f))
										continue;
									// ReSharper disable once AccessToModifiedClosure
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
