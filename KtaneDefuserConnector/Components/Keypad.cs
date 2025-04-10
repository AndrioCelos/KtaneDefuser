using System;
using System.Collections.Generic;
using KtaneDefuserConnector.Properties;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace KtaneDefuserConnector.Components;
public class Keypad : ComponentReader<Keypad.ReadData> {
	private static readonly (Image<L8> image, Symbol symbol)[] ReferenceSymbols = [
		(LoadSampleImage(Resources.KeypadAe), Symbol.Ae),
		(LoadSampleImage(Resources.KeypadAT), Symbol.AT),
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
	];

	private static Image<L8> LoadSampleImage(byte[] bytes) {
		var image = Image.Load<L8>(bytes);
		image.Mutate(c => c.Resize(64, 64));
		return image;
	}

	public override string Name => "Keypad";
	protected internal override ComponentFrameType FrameType => ComponentFrameType.Solvable;

	protected internal override float IsModulePresent(Image<Rgba32> image) {
		// Keypad: look for white keys and gray bezel
		var referenceColour = new Rgba32(136, 140, 150);
		var referenceColour2 = new Rgba32(232, 217, 194);
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

	protected internal override ReadData Process(Image<Rgba32> image, LightsState lightsState, ref Image<Rgba32>? debugImage) {
		bool IsKeyBackground(Rgba32 c) => HsvColor.FromColor(ImageUtils.ColourCorrect(c, lightsState)) is { H: > 0 and <= 150, S: <= 0.6f, V: >= 0.35f };

		if (debugImage is not null) {
			debugImage.Mutate(c => c.Brightness(0.5f));
			for (var y = 0; y < image.Width; y++) {
				for (var x = 0; x < image.Width; x++) {
					var color = image[x, y];
					if (!IsKeyBackground(color)) continue;
					if (debugImage is not null) debugImage[x, y] = color;
				}
			}
		}

		var keypadCorners = ImageUtils.FindCorners(image, new(0, 48, 208, 208), IsKeyBackground, 12);
		debugImage?.DebugDrawPoints(keypadCorners);

		var keysBitmap = ImageUtils.PerspectiveUndistort(image, keypadCorners, InterpolationMode.Bilinear, new(256, 256));
		keysBitmap.ColourCorrect(lightsState);

		var baseRectangles = new Rectangle[] { new(12, 16, 104, 96), new(140, 16, 104, 96), new(8, 148, 104, 96), new(140, 148, 104, 96) };
		var keyRectangles = new Rectangle[4];

		// Find the symbol bounding boxes.
		for (var i = 0; i < 4; i++) {
			var rect = baseRectangles[i];

			// Skip over the light and key shadows.
			keysBitmap.ProcessPixelRows(a => {
				while (true) {
					var r = a.GetRowSpan(rect.Y);
					if (HsvColor.FromColor(r[(rect.Left + rect.Right) / 2]).V >= 0.5f) break;
					rect.Y++;
					rect.Height--;
				}
				rect.Y += 2;
				rect.Height -= 2;
				while (true) {
					var r = a.GetRowSpan(rect.Bottom - 1);
					if (HsvColor.FromColor(r[(rect.Left + rect.Right) / 2]) is HsvColor hsv && hsv.V >= 0.5f && hsv.S < 0.5f) break;
					rect.Height--;
				}
				rect.Height -= 2;
				while (true) {
					var found = false;
					for (var y = rect.Top; y < rect.Bottom; y++) {
						var hsv = HsvColor.FromColor(a.GetRowSpan(y)[rect.Right - 1]);
						if (hsv.V < 0.5f || hsv.S >= 0.5f) {
							found = true;
							break;
						}
					}
					if (!found) break;
					rect.Width--;
				}
				rect.Width -= 2;
			});

			rect = ImageUtils.FindEdges(keysBitmap, rect, c => HsvColor.FromColor(c).V < 0.5f);
			rect.Inflate(6, 6);

			keyRectangles[i] = rect;
		}

		if (debugImage is not null) {
			debugImage.Mutate(c => c.Resize(new ResizeOptions() { Size = new(512, 512), Mode = ResizeMode.BoxPad, Position = AnchorPositionMode.TopLeft, PadColor = Color.Black }).DrawImage(keysBitmap, new Point(0, 256), 1));
			foreach (var rect in keyRectangles) {
				rect.Offset(0, 256);
				debugImage.Mutate(c => c.Draw(Pens.Solid(Color.Lime, 1), rect));
			}
			foreach (var rect in baseRectangles) {
				rect.Offset(0, 256);
				debugImage.Mutate(c => c.Draw(Pens.Solid(Color.Cyan, 1), rect));
			}
		}

		var symbols = new Symbol[4];
		var symbolGuesses = new List<(Symbol symbol, int dist)>[4];
		for (var i = 0; i < 4; i++) {
			var box = keyRectangles[i];
			using var resizedKeyImage = keysBitmap.Clone(c => c.Crop(box).Resize(64, 64));

			symbolGuesses[i] = [];
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
				if (dist >= bestDist) continue;
				bestDist = dist;
				symbols[i] = symbol;
			}
		}

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
		AT,
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
		public override string ToString() => string.Join(' ', Symbols);
	}
}
