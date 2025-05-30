﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace KtaneDefuserConnector.Widgets;
public class SerialNumber : WidgetReader<string> {
	public override string Name => "SerialNumber";

	private static readonly Font Font;
	private static readonly (Image<Rgba32> image, char c)[] ReferenceImages;

	static SerialNumber() {
		using var ms = new MemoryStream(Properties.Resources.AnonymousProBold);
		var fontCollection = new FontCollection();
		var fontFamily = fontCollection.Add(ms);
		Font = new(fontFamily, 72);

		ReferenceImages = new (Image<Rgba32> image, char c)[36];
		for (var i = 0; i < 10; i++)
			ReferenceImages[i] = CreateSampleImage((char) ('0' + i));
		for (var i = 0; i < 26; i++)
			ReferenceImages[10 + i] = CreateSampleImage((char) ('A' + i));  // I know that the letters 'O' and 'Y' never appear in the serial number normally, but I'm including all letters for simplicity.
	}

	private static (Image<Rgba32> image, char c) CreateSampleImage(char c) {
		var image = new Image<Rgba32>(128, 128, Color.White);
		image.Mutate(ctx => ctx.DrawText(new(Font) { Dpi = 96, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Origin = new(64, 64) }, c.ToString(), Color.Black));
		var charBounds = ImageUtils.FindEdges(image, image.Bounds, c => c.B < 128);
		image.Mutate(ctx => ctx.Crop(charBounds).Resize(64, 64, KnownResamplers.NearestNeighbor));
		return (image, c);
	}

	private static bool IsBlack(HsvColor hsv) => hsv is { H: < 180, S: < 0.2f, V: <= 0.2f };

	[SuppressMessage("ReSharper", "AccessToModifiedClosure")]
	protected internal override string Process(Image<Rgba32> image, LightsState lightsState, ref Image<Rgba32>? debugImage) {
		debugImage?.Mutate(c => c.Resize(new ResizeOptions { Size = new(512, 512), Mode = ResizeMode.BoxPad, Position = AnchorPositionMode.TopLeft, PadColor = Color.Black }));

		// Find the text bounding box.
		var textBounds = ImageUtils.FindEdges(image, image.Bounds, c => IsBlack(HsvColor.FromColor(c)));

		// Find out whether the image is upside-down or not.
		bool? isUpsideDown = null;
		image.ProcessPixelRows(a => {
			for (var i = 1; i < 256; i++) {
				for (var rowNumber = 0; rowNumber < 2; rowNumber++) {
					var y = rowNumber == 0 ? textBounds.Top - i : textBounds.Bottom + i;
					if (y < 0 || y >= a.Height) continue;
					var r = a.GetRowSpan(y);
					for (var x = textBounds.Left; x <= textBounds.Right; x++) {
						if (r[x].R < 96 || r[x].G >= 32 || r[x].B >= 32) continue;
						isUpsideDown = rowNumber != 0;
						return;
					}
				}
			}
			throw new InvalidOperationException("Can't find the serial number heading");
		});

		debugImage?.Mutate(c => c.Draw(isUpsideDown!.Value ? Color.Yellow : Color.Lime, 1, textBounds));

		var charImages = new List<Image<Rgba32>>();
		var lastX = 0;
		int? charStart = null;
		for (var i = 0; i < textBounds.Width; i++) {
			for (; i < textBounds.Width; i++) {
				var x = isUpsideDown!.Value ? textBounds.Right - 1 - i : textBounds.Left + i;
				var anyPixels = false;
				for (var y = textBounds.Top; y < textBounds.Bottom; y++) {
					if (image[x, y].B >= 128) continue;
					anyPixels = true;
					break;
				}
				if (charStart.HasValue) {
					if (!anyPixels)
						break;
				} else {
					if (anyPixels) {
						charStart = x;
					}
				}
				lastX = x;
			}

			if (!charStart.HasValue) continue;
			var charImage = image.Clone(c => c.Crop(new(Math.Min(charStart.Value, lastX), textBounds.Y, Math.Abs(lastX - charStart.Value) + 1, textBounds.Height)).Resize(64, 64, KnownResamplers.NearestNeighbor));
			if (isUpsideDown == true)
				charImage.Mutate(c => c.Rotate(RotateMode.Rotate180));
			charImages.Add(charImage);
			if (charImages.Count == 6) break;
			charStart = null;
		}

		if (debugImage != null) {
			for (var i = 0; i < charImages.Count; i++) {
				debugImage?.Mutate(c => c.DrawImage(charImages[i], new Point(64 * i, 256), 1));
			}
		}
		if (charImages.Count < 6) throw new ArgumentException("Found wrong number of characters");

		for (var attempt = 0; ; attempt++) {
			var chars = new char[6];
			for (var i = 0; i < 6; i++) {
				var bestDist = int.MaxValue;
				foreach (var (refImage, c) in ReferenceImages) {
					var dist = 0;
					for (var y = 0; y < refImage.Height; y++) {
						for (var x = 0; x < refImage.Width; x++) {
							var cr = refImage[x, y];
							var cs = charImages[i][x, y];
							dist += Math.Abs(cr.B - cs.B);
						}
					}

					if (dist >= bestDist) continue;
					bestDist = dist;
					chars[i] = c;
				}
			}

			if (attempt != 0 || !chars.Contains('Q')) return new(chars);
			// The descender of the 'Q' introduces whitespace below all other letters. Account for that and restart.
			for (var i = 0; i < 6; i++) {
				var bottom = 64;
				charImages[i].ProcessPixelRows(a => {
					for (var y = a.Height - 1; y >= 32; y--) {
						var r = a.GetRowSpan(y);
						for (var x = 0; x < a.Width; x++) {
							if (r[x].R >= 128) continue;
							bottom = y + 1;
							return;
						}
					}
				});
				if (bottom >= 64) continue;
				charImages[i].Mutate(c => c.Crop(64, bottom).Resize(64, 64, KnownResamplers.NearestNeighbor));
				debugImage?.Mutate(c => c.DrawImage(charImages[i], new Point(64 * i, 256), 1));
			}
		}
	}
}
