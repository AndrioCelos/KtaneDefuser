
using BombDefuserConnector.Properties;

using System;
using System.Collections.Generic;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;

namespace BombDefuserConnector.Components;
internal class Button : ComponentProcessor<Button.ReadData> {

	private static readonly Dictionary<Label, Image<Rgba32>> referenceButtonLabels = new() {
		{ Label.Abort, Image.Load<Rgba32>(Resources.ButtonAbort) },
		{ Label.Detonate, Image.Load<Rgba32>(Resources.ButtonDetonate) },
		{ Label.Hold, Image.Load<Rgba32>(Resources.ButtonHold) },
		{ Label.Press, Image.Load<Rgba32>(Resources.ButtonPress) }
	};

	public override string Name => "The Button";
	public override bool UsesNeedyFrame => false;

	private bool isCoveredRed(HsvColor hsv) => hsv.H is >= 330 or <= 15 && hsv.S is >= 0.2f and <= 0.4f && hsv.V >= 0.5f;
	private bool isCoveredYellow(HsvColor hsv) => hsv.H is >= 45 and <= 90 && hsv.S is >= 0.2f and <= 0.4f && hsv.V >= 0.5f;
	private bool isCoveredBlue(HsvColor hsv) => hsv.H is >= 210 and <= 220 && hsv.S is >= 0.30f and <= 0.55f && hsv.V >= 0.5f;
	private bool isCoveredWhite(HsvColor hsv) => hsv.H is >= 150 and <= 180 && hsv.S <= 0.15f && hsv.V >= 0.5f;

	private (int red, int yellow, int blue, int white) getIsModulePresentColours1(Image<Rgb24> image) {
		int red = 0, yellow = 0, blue = 0, white = 0;
		for (var y = 90; y <= 210; y += 30) {
			for (var x = 40; x <= 160; x += 30) {
				var hsv = HsvColor.FromColor(image[x, y]);
				if (isCoveredRed(hsv)) red++;
				else if (isCoveredYellow(hsv)) yellow++;
				else if (isCoveredBlue(hsv)) blue++;
				else if (isCoveredWhite(hsv)) white++;
			}
		}
		return (red, yellow, blue, white);
	}

	private bool checkPixel(Image<Rgb24> image, int x, int y, Predicate<HsvColor> predicate) {
		return x is >= 0 and < 256 && y is >= 0 and < 256 && predicate(HsvColor.FromColor(image[x, y]));
	}

	public override float IsModulePresent(Image<Rgb24> image) {
		// The Button: look for the large circle.
		// This is only expected to work under normal lighting (not corrected buzzing/off/emergency lighting)

		// First do a quick check of what the colour is if this is a Button.
		var checkResult = getIsModulePresentColours1(image);
		
		var conditionsMet = (checkResult.red >= 10 ? 1 : 0) + (checkResult.yellow >= 10 ? 1 : 0) + (checkResult.blue >= 10 ? 1 : 0) + (checkResult.white >= 10 ? 1 : 0);
		if (conditionsMet != 1)
			return 0;

		Predicate<HsvColor> predicate = checkResult.red >= 10 ? isCoveredRed
			: checkResult.yellow >= 10 ? isCoveredYellow
			: checkResult.blue >= 10 ? isCoveredBlue
			: isCoveredWhite;

		const int ISOLATION_CHECK_RADIUS = 5;
		var count = 0;
		for (var y = 0; y < 256; y++) {
			for (var x = 0; x < 256; x++) {
				if (predicate(HsvColor.FromColor(image[x, y]))) {
					if (((checkPixel(image, x - ISOLATION_CHECK_RADIUS * 2, y, predicate) && checkPixel(image, x - ISOLATION_CHECK_RADIUS, y, predicate)) || (checkPixel(image, x + ISOLATION_CHECK_RADIUS * 2, y, predicate) && checkPixel(image, x + ISOLATION_CHECK_RADIUS, y, predicate)))
						&& ((checkPixel(image, x, y - ISOLATION_CHECK_RADIUS * 2, predicate) && checkPixel(image, x, y - ISOLATION_CHECK_RADIUS, predicate)) || (checkPixel(image, x, y + ISOLATION_CHECK_RADIUS * 2, predicate) && checkPixel(image, x, y + ISOLATION_CHECK_RADIUS, predicate)))) {
						var distSq = (x - 100) * (x - 100) + (y - 150) * (y - 150);
						if (distSq <= 70 * 70) count += 10;
						else if (distSq <= 110 * 110) count += 3;
						else count -= 200;
					}
				}
			}
		}

		return count / 75000f;
		/*
		// The Button: look for the indicator and hinge
		var count = 0;
		for (var y = 110; y < 224; y++) {
			count += BitmapUtils.ColorProximity(bitmap.GetPixel(220, y), 2, 3, refB: 2, 10);
		}

		var count2 = 0f;
		var count3 = 0f;
		var pixels3 = 0;
		for (var x = 20; x < 172; x++) {
			var color = bitmap.GetPixel(x, 56);
			var hsv = HsvColor.FromColor(color);
			if (hsv.S >= 0.3) count2 += Math.Max(0, 45 - Math.Abs(hsv.H - 225));
			else {
				if (pixels3 < 108)
					count3 += Math.Max(0, 10 - Math.Abs(hsv.S - 0.15f));
				pixels3++;
			}
		}

		return count / 1900f + count2 / 11250f + count3 / 5100f;
		*/
	}

	public override ReadData Process(Image<Rgb24> image, ref Image<Rgb24> debugBitmap) {
		debugBitmap.Mutate(c => c.Brightness(0.5f));

		var checkResult = getIsModulePresentColours1(image);

		Predicate<HsvColor> predicate = checkResult.red >= 10 ? isCoveredRed
			: checkResult.yellow >= 10 ? isCoveredYellow
			: checkResult.blue >= 10 ? isCoveredBlue
			: isCoveredWhite;

		const int ISOLATION_CHECK_RADIUS = 5;
		var count = 0;
		for (var y = 0; y < 256; y++) {
			for (var x = 0; x < 256; x++) {
				if (predicate(HsvColor.FromColor(image[x, y]))) {
					if (((checkPixel(image, x - ISOLATION_CHECK_RADIUS * 2, y, predicate) && checkPixel(image, x - ISOLATION_CHECK_RADIUS, y, predicate)) || (checkPixel(image, x + ISOLATION_CHECK_RADIUS * 2, y, predicate) && checkPixel(image, x + ISOLATION_CHECK_RADIUS, y, predicate)))
						&& ((checkPixel(image, x, y - ISOLATION_CHECK_RADIUS * 2, predicate) && checkPixel(image, x, y - ISOLATION_CHECK_RADIUS, predicate)) || (checkPixel(image, x, y + ISOLATION_CHECK_RADIUS * 2, predicate) && checkPixel(image, x, y + ISOLATION_CHECK_RADIUS, predicate)))) {
						var distSq = (x - 100) * (x - 100) + (y - 150) * (y - 150);
						if (distSq <= 70 * 70) { count += 10; debugBitmap[x, y] = new(0, 255, 0); } else if (distSq <= 110 * 110) { count += 3; debugBitmap[x, y] = new(0, 128, 0); } else { count -= 200; debugBitmap[x, y] = new(255, 0, 0); }
					} else
						debugBitmap[x, y] = new(255, 255, 255);
				}
			}
		}

		// Check for the indicator.
		int iblack = 0, ired = 0, iyellow = 0, iblue = 0, iwhite = 0;
		for (var y = 110; y < 224; y++) {
			var color = image[220, y];
			var hsv = HsvColor.FromColor(color);

			if (hsv.S < 0.05f && hsv.V >= 0.75f)
				iwhite++;
			else if (hsv.S >= 0.75f && hsv.V >= 0.75f && hsv.H is >= 350 or <= 10)
				ired++;
			else if (hsv.S >= 0.75f && hsv.V >= 0.65f && hsv.H is >= 210 and <= 225)
				iblue++;
			else if (hsv.S >= 0.75f && hsv.V >= 0.65f && hsv.H is >= 45 and <= 60)
				iyellow++;
			else if (hsv.V <= 0.05f)
				iblack++;
		}

		Colour? indicatorColour =
			iblack >= 60 ? null : iwhite >= 60 ? Colour.White : ired >= 60 ? Colour.Red : iblue >= 60 ? Colour.Blue : iyellow >= 60 ? Colour.Yellow : null;

		bool isBlackColor(HsvColor hsv)
			=> hsv.H is >= 150 and <= 210 && hsv.S is >= 0.15f and <= 0.25f && hsv.V is >= 0.35f and <= 0.55f  // Black with cover
			|| (hsv.H is >= 40 and <= 90 || hsv.S < 0.2f) && hsv.V <= 0.2f;  // Black without cover; text is slightly semitransparent
		bool isRedColor(HsvColor hsv) => hsv.H >= 330;
		bool isYellowColor(HsvColor hsv) => hsv.H is >= 40 and <= 90 && hsv.S >= 0.2f && hsv.V >= 0.5f;
		bool isBlueColor(HsvColor hsv) => hsv.H is >= 210 and <= 232 && hsv.S >= 0.25f && hsv.V >= 0.5f;
		bool isWhiteColor(HsvColor hsv) => hsv.S < 0.2f && hsv.V >= 0.7f;  // This will have a lot of false positives.
		bool isWhiteLabelColor(HsvColor hsv) => hsv.S < 0.2f && hsv.V >= 0.775f;

		long black = 0, red = 0, yellow = 0, blue = 0, white = 0, total = 0;
		for (var y = 0; y < 256; y++) {
			for (var x = 0; x < 256; x++) {
				var dist = Math.Abs(x - 104) + Math.Abs(y - 136);
				var weight = dist switch { <= 80 => 10, <= 120 => 7, <= 160 => 4, _ => 1 };
				total += weight;

				var color = image[x, y];
				var hsv = HsvColor.FromColor(color);
				if (isRedColor(hsv))
					red += weight;
				else if (isYellowColor(hsv))
					yellow += weight;
				else if (isBlueColor(hsv))
					blue += weight;
				else if (isBlackColor(hsv))
					black += weight;
				else if (isWhiteColor(hsv))
					white += weight;
			}
		}

		var buttonColour = red > white ? Colour.Red : yellow > white ? Colour.Yellow : blue > white ? Colour.Blue : Colour.White;

		// Now try to read the label.
		// Find the bounding box of the button face.
		Predicate<HsvColor> facePredicate = buttonColour switch { Colour.Red => isRedColor, Colour.Yellow => isYellowColor, Colour.Blue => isBlueColor, _ => isWhiteColor };
		Predicate<HsvColor> labelPredicate = buttonColour switch { Colour.White or Colour.Yellow => isBlackColor, _ => isWhiteLabelColor };
		/*
		for (var y = 0; y < debugBitmap.Width; y++) {
			for (var x = 0; x < debugBitmap.Width; x++) {
				var color = bitmap.GetPixel(x, y);
				var hsv = HsvColor.FromColor(color);
				if (facePredicate(hsv))
					debugBitmap.SetPixel(x, y, color);
				else if (labelPredicate(hsv))
					debugBitmap.SetPixel(x, y, Color.Green);
			}
		}
		*/

		var bbox = new Rectangle(88, 136, 32, 32);
		var edgeIndex = 0;
		var misses = 0;
		while (true) {
			var pixels = 0;
			if (edgeIndex < 2) {
				if (edgeIndex == 0 ? bbox.Top >= 0 : bbox.Bottom < 256) {
					for (var dx = 0; dx < bbox.Width; dx++) {
						if (bbox.X + dx is >= 0 and < 256) {
							var hsv = HsvColor.FromColor(image[bbox.X + dx, edgeIndex == 0 ? bbox.Top : bbox.Bottom]);
							if (facePredicate(hsv) || labelPredicate(hsv))
								pixels++;
						}
					}
				}
			} else {
				if (edgeIndex == 2 ? bbox.Left >= 0 : bbox.Right < 256) {
					for (var dy = 0; dy < bbox.Height; dy++) {
						if (bbox.Y + dy is >= 0 and < 256) {
							var hsv = HsvColor.FromColor(image[edgeIndex == 2 ? bbox.Left : bbox.Right, bbox.Y + dy]);
							if (facePredicate(hsv) || labelPredicate(hsv))
								pixels++;
						}
					}
				}
			}
			if (pixels < 10) {
				misses++;
				if (misses >= 4) break;
			} else {
				misses = 0;
				switch (edgeIndex) {
					case 0: bbox.Y--; break;
					case 1: bbox.Height++; break;
					case 2: bbox.X--; break;
					default: bbox.Width++; break;
				}
			}
			edgeIndex++;
			edgeIndex %= 4;
		}
		bbox.Inflate(-1, -1);
		debugBitmap.Mutate(c => c.Draw(Pens.Solid(Color.Lime, 1), bbox));

		// Find the bounding box of the label by shrinking the face bounding box.
		// Shrink it in each direction until the edge contains one or more adjacent label pixels with directly adjacent face pixels on both sides.
		var labelBB = bbox;
		// Top and bottom
		for (var edge = 0; edge < 2; edge++) {
			while (labelBB.Height > 1) {
				var y = edge == 0 ? labelBB.Top : labelBB.Bottom;
				var validLabel = 0;
				var found = false;
				for (var dx = labelBB.Width; dx >= 0 || validLabel > 0; dx--) {
					var hsv = HsvColor.FromColor(image[Math.Max(0, labelBB.X + dx), y]);
					if (labelPredicate(hsv)) {
						if (validLabel == 0) {
							var hsv2 = HsvColor.FromColor(image[Math.Min(255, labelBB.X + dx + 3), y]);
							if (facePredicate(hsv2))
								validLabel = 3;
						} else
							validLabel = 3;
					} else if (validLabel > 0) {
						if (facePredicate(hsv)) {
							found = true;
							break;
						} else
							validLabel--;
					}
				}
				if (found)
					break;
				else {
					if (edge == 0) labelBB.Y++;
					labelBB.Height--;
				}
			}
		}
		// Left and right
		for (var edge = 0; edge < 2; edge++) {
			while (labelBB.Width > 1) {
				var x = edge == 0 ? labelBB.Left : labelBB.Right;
				var validLabel = 0;
				var found = false;
				for (var dy = labelBB.Height; dy >= 0 || validLabel > 0; dy--) {
					var hsv = HsvColor.FromColor(image[x, Math.Max(0, labelBB.Y + dy)]);
					if (labelPredicate(hsv)) {
						if (validLabel == 0) {
							var hsv2 = HsvColor.FromColor(image[x, Math.Min(255, labelBB.Y + dy + 3)]);
							if (facePredicate(hsv2))
								validLabel = 3;
						} else
							validLabel = 3;
					} else if (validLabel > 0) {
						if (facePredicate(hsv)) {
							found = true;
							break;
						} else
							validLabel--;
					}
				}
				if (found)
					break;
				else {
					if (edge == 0) labelBB.X++;
					labelBB.Width--;
				}
			}
		}

		debugBitmap.Mutate(c => c.Draw(Pens.Solid(Color.Cyan, 1), labelBB));

		var labelBitmap = image.Clone(c => c.Crop(labelBB).Resize(96, 32));
		debugBitmap.Mutate(c => c.DrawImage(labelBitmap, 1));

		var bestLabel = Label.Abort;
		var bestMatchScore = -1;
		foreach (var (label, refBitmap) in referenceButtonLabels) {
			var matchScore = 0;
			for (var y = 0; y < labelBitmap.Height; y++) {
				for (var x = 0; x < labelBitmap.Width; x++) {
					var hsv = HsvColor.FromColor(labelBitmap[x, y]);
					if (labelPredicate(hsv) == refBitmap[x, y].A > 0)
						matchScore++;
				}
			}
			if (matchScore > bestMatchScore) {
				bestLabel = label;
				bestMatchScore = matchScore;
			}
		}

		return new(buttonColour, bestLabel, indicatorColour, count);
	}

	public record ReadData(Colour Colour, Label Label, Colour? IndicatorColour, object debug) {
		public override string ToString() => $"{this.Colour} {this.Label} {this.IndicatorColour?.ToString() ?? "nil"}";
	}

	public enum Colour {
		Red,
		Yellow,
		Blue,
		White
	}

	public enum Label {
		Abort,
		Detonate,
		Hold,
		Press
	}
}
