using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using BombDefuserConnector.Properties;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace BombDefuserConnector.Components;
public class Keypad : ComponentProcessor<Keypad.ReadData> {
	private static readonly (Image<L8> image, Symbol symbol)[] ReferenceSymbols = new[] {
		(LoadSampleImage(Resources.KeypadAe), Symbol.Ae),
		(LoadSampleImage(Resources.KeypadBalloon), Symbol.Balloon),
		(LoadSampleImage(Resources.KeypadBT), Symbol.BT),
		(LoadSampleImage(Resources.KeypadCircle), Symbol.Circle),
		(LoadSampleImage(Resources.KeypadCircle2), Symbol.Circle),
		(LoadSampleImage(Resources.KeypadCircle3), Symbol.Circle),
		(LoadSampleImage(Resources.KeypadCopyright), Symbol.Copyright),
		(LoadSampleImage(Resources.KeypadCursive), Symbol.Cursive),
		(LoadSampleImage(Resources.KeypadDoubleK), Symbol.DoubleK),
		(LoadSampleImage(Resources.KeypadDragon), Symbol.Dragon),
		(LoadSampleImage(Resources.KeypadEuro), Symbol.Euro),
		(LoadSampleImage(Resources.KeypadFilledStar), Symbol.FilledStar),
		(LoadSampleImage(Resources.KeypadHollowStar), Symbol.HollowStar),
		(LoadSampleImage(Resources.KeypadHookN), Symbol.HookN),
		(LoadSampleImage(Resources.KeypadLeftC), Symbol.LeftC),
		(LoadSampleImage(Resources.KeypadMeltedThree), Symbol.MeltedThree),
		(LoadSampleImage(Resources.KeypadNWithHat), Symbol.NWithHat),
		(LoadSampleImage(Resources.KeypadOmega), Symbol.Omega),
		(LoadSampleImage(Resources.KeypadParagraph), Symbol.Paragraph),
		(LoadSampleImage(Resources.KeypadPumpkin), Symbol.Pumpkin),
		(LoadSampleImage(Resources.KeypadPyramid), Symbol.Pyramid),
		(LoadSampleImage(Resources.KeypadQuestionMark), Symbol.QuestionMark),
		(LoadSampleImage(Resources.KeypadRightC), Symbol.RightC),
		(LoadSampleImage(Resources.KeypadSix), Symbol.Six),
		(LoadSampleImage(Resources.KeypadSmileyFace), Symbol.SmileyFace),
		(LoadSampleImage(Resources.KeypadSquidKnife), Symbol.SquidKnife),
		(LoadSampleImage(Resources.KeypadSquigglyN), Symbol.SquigglyN),
		(LoadSampleImage(Resources.KeypadTeepee), Symbol.Teepee),
		(LoadSampleImage(Resources.KeypadTracks), Symbol.Tracks),
		(LoadSampleImage(Resources.KeypadTrident), Symbol.Trident),
		(LoadSampleImage(Resources.KeypadTripod), Symbol.Tripod),
		(LoadSampleImage(Resources.KeypadUpsideDownY), Symbol.UpsideDownY),
		(LoadSampleImage(Resources.KeypadWeirdNose), Symbol.WeirdNose)
	};

	private static Image<L8> LoadSampleImage(byte[] bytes) {
		var image = Image.Load<L8>(bytes);
		image.Mutate(c => c.Resize(64, 64));
		return image;
	}

	public override string Name => "Keypad";
	protected internal override bool UsesNeedyFrame => false;

	protected internal override float IsModulePresent(Image<Rgb24> image) {
		// Keypad: look for white keys and gray bezel
		var referenceColour = new Rgb24(136, 140, 150);
		var referenceColour2 = new Rgb24(232, 217, 194);
		var count = 0f;

		for (var y = 32; y <= 224; y += 16) {
			for (var x = 32; x <= 224; x += 16) {
				var pixel = image[x, y];
				if (y < 60 || x > 200) {
					var dist = Math.Abs(pixel.R - referenceColour.R) + Math.Abs(pixel.G - referenceColour.G) + Math.Abs(pixel.B - referenceColour.B);
					count += Math.Max(0, 1 - dist / 80f);
				} else {
					var dist = Math.Abs(pixel.R - referenceColour2.R) + Math.Abs(pixel.G - referenceColour2.G) + Math.Abs(pixel.B - referenceColour2.B);
					count += Math.Max(0, 1 - dist / 80f);
				}
			}
		}

		return Math.Min(1, count / 100f);
	}

	protected internal override ReadData Process(Image<Rgb24> image, ref Image<Rgb24>? debugBitmap) {
		static bool predicate(Rgb24 c) {
			var hsv = HsvColor.FromColor(c);
			return hsv.H is >= 30 and <= 60 && hsv.S <= 0.4f && hsv.V >= 0.35f;
		}

		debugBitmap?.Mutate(c => c.Brightness(0.5f));
		for (var y = 0; y < image.Width; y++) {
			for (var x = 0; x < image.Width; x++) {
				var color = image[x, y];
				if (predicate(color)) {
					if (debugBitmap is not null) debugBitmap[x, y] = color;
				}
			}
		}

		var keypadCorners = ImageUtils.FindCorners(image, new(0, 48, 208, 208), predicate, 12) ?? throw new ArgumentException("Can't find keypad corners");
		if (debugBitmap is not null) ImageUtils.DebugDrawPoints(debugBitmap, keypadCorners);

		var keysBitmap = ImageUtils.PerspectiveUndistort(image, keypadCorners, InterpolationMode.Bilinear, new(256, 256));

		var keyRectangles = new Rectangle[] { new(0, 0, 128, 128), new(128, 0, 128, 128), new(0, 128, 128, 128), new(128, 128, 128, 128) };

		// Find the symbol bounding boxes.
		for (var i = 0; i < 4; i++) {
			// Find approximately the nearest dark pixel to the centre.
			var startPoint = new Point(i % 2 == 0 ? 64 : 192, i / 2 == 0 ? 80 : 208);

			var d = Size.Empty;
			var quadrant = -1;
			while (true) {
				var p = startPoint + d;
				if (p.X is >= 0 and < 256 && p.Y is >= 0 and < 256 &&
					HsvColor.FromColor(keysBitmap[startPoint.X + d.Width, startPoint.Y + d.Height]).V < 0.5f)
					break;

				if (d == Size.Empty)
					d.Height--;
				switch (quadrant) {
					case 0:
						d.Width++;
						d.Height++;
						if (d.Height == 0) quadrant = 1;
						break;
					case 1:
						d.Width--;
						d.Height++;
						if (d.Width == 0) quadrant = 2;
						break;
					case 2:
						d.Width--;
						d.Height--;
						if (d.Height == 0) quadrant = 3;
						break;
					case 3:
						d.Width++;
						d.Height--;
						if (d.Width == 0) goto default;
						break;
					default:
						d.Height--;
						quadrant = 0;
						break;
				}
			}

			startPoint += d;
			var bbox = new Rectangle(startPoint, Size.Empty);
			bbox.Inflate(5, 5);

			// Find the symbol bounding box.
			var clearLines = 0;
			var edge = 0;
			while (clearLines < 32) {
				if (edge < 2) {
					var y = edge == 0 ? bbox.Top : bbox.Bottom;
					var found = false;
					for (var dx = 0; dx < bbox.Width; dx++) {
						if (HsvColor.FromColor(keysBitmap[bbox.X + dx, y]).V < 0.5f) {
							found = true;
							break;
						}
					}
					if (found && edge == 0) {
						// Try to filter out the LED; it consists of about 32 black pixels all in a row and 4 other dark pixels.
						int blackCount = 0, blackInARowCount = 0, darkCount = 0;
						var lastWasBlack = false;
						for (var dx = 0; dx < bbox.Width; dx++) {
							var hsv = HsvColor.FromColor(keysBitmap[bbox.X + dx, y]);
							if (hsv.V < 0.2f) {
								if (!lastWasBlack) {
									lastWasBlack = true;
									blackInARowCount = 0;
								}
								darkCount++;
								blackCount++;
								blackInARowCount++;
							} else if (hsv.V < 0.5f) {
								darkCount++;
								lastWasBlack = false;
							} else {
								lastWasBlack = false;
							}
						}
						if (blackCount is >= 30 and <= 40 && blackInARowCount == blackCount && darkCount is >= 35 and <= 50)
							found = false;
					}
					if (found) {
						clearLines = 0;
						if (edge == 0) bbox.Y--;
						bbox.Height++;
					} else clearLines++;
				} else {
					var x = edge == 2 ? bbox.Left : bbox.Right;
					var found = false;
					for (var dy = 0; dy < bbox.Height; dy++) {
						if (HsvColor.FromColor(keysBitmap[x, bbox.Y + dy]).V < 0.5f) {
							found = true;
							break;
						}
					}
					if (found) {
						clearLines = 0;
						if (edge == 2) bbox.X--;
						bbox.Width++;
					} else clearLines++;
				}
				edge = (edge + 1) % 4;
				if (clearLines != 0 && clearLines % 4 == 0)
					bbox.Inflate(1, 1);
			}

			//bbox.Inflate(-4, -4);
			if (bbox.X < 0) bbox.X = 0;
			if (bbox.Y < 0) bbox.Y = 0;
			keyRectangles[i] = bbox;
		}

		if (debugBitmap is not null) {
			debugBitmap?.Mutate(c => c.Resize(new ResizeOptions() { Size = new(512, 512), Mode = ResizeMode.BoxPad, Position = AnchorPositionMode.TopLeft, PadColor = Color.Black }).DrawImage(keysBitmap, new Point(0, 256), 1));
			foreach (var rect in keyRectangles) {
				rect.Offset(0, 256);
				debugBitmap?.Mutate(c => c.Draw(Pens.Solid(Color.Lime, 1), rect));
			}
		}

		var symbols = new Symbol[4];
		var symbolGuesses = new List<(Symbol symbol, int dist)>[4];
		for (var i = 0; i < 4; i++) {
			var box = keyRectangles[i];
			using var resizedKeyImage = keysBitmap.Clone(c => c.Crop(box).Resize(64, 64));

			symbolGuesses[i] = new List<(Symbol symbol, int dist)>();
			var bestDist = int.MaxValue;
			foreach (var (refBitmap, symbol) in ReferenceSymbols) {
				var dist = 0;
				for (var y = 0; y < refBitmap.Height; y++) {
					for (var x = 0; x < refBitmap.Width; x++) {
						var cr = refBitmap[x, y];
						var vr = cr.PackedValue;
						var ck = resizedKeyImage[x, y];
						var vk = Math.Max(Math.Max(ck.R, ck.G), ck.B);
						dist += Math.Abs(vr - vk);
					}
				}
				symbolGuesses[i].Add((symbol, dist));
				if (dist < bestDist) {
					bestDist = dist;
					symbols[i] = symbol;
				}
			}
		}
		var keysSummary = string.Join("\r\n", symbolGuesses.Select((l, i) => $"{i + 1}: {string.Join(", ", l.OrderBy(g => g.dist).Take(3).Select(g => $"{g.symbol} ({g.dist:#,##0})"))}"));
		return new(symbols);
	}

	public enum Symbol {
		Ae,
		Balloon,
		BT,
		Circle,
		Copyright,
		Cursive,
		DoubleK,
		Dragon,
		Euro,
		FilledStar,
		HollowStar,
		HookN,
		LeftC,
		MeltedThree,
		NWithHat,
		Omega,
		Paragraph,
		Pumpkin,
		Pyramid,
		QuestionMark,
		RightC,
		Six,
		SmileyFace,
		SquidKnife,
		SquigglyN,
		Teepee,
		Tracks,
		Trident,
		Tripod,
		UpsideDownY,
		WeirdNose
	}

	public record ReadData(Symbol[] Symbols) {
		public override string ToString() => string.Join(' ', this.Symbols);
	}
}
