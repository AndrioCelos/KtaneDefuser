using System;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace BombDefuserConnector.Components;
public class Button : ComponentReader<Button.ReadData> {
	private static readonly TextRecogniser labelRecogniser = new(new(TextRecogniser.Fonts.OSTRICH_SANS_HEAVY, 24), 160, 44, new(128, 64),
		  "ABORT", "DETONATE", "HOLD", "PRESS");

	public override string Name => "The Button";
	protected internal override bool UsesNeedyFrame => false;

	private static bool IsCoveredRed(HsvColor hsv) => hsv.H is >= 330 or <= 15 && hsv.S is >= 0.2f and <= 0.4f && hsv.V >= 0.5f;
	private static bool IsCoveredYellow(HsvColor hsv) => hsv.H is >= 45 and <= 90 && hsv.S is >= 0.2f and <= 0.4f && hsv.V >= 0.5f;
	private static bool IsCoveredBlue(HsvColor hsv) => hsv.H is >= 210 and <= 220 && hsv.S is >= 0.30f and <= 0.55f && hsv.V >= 0.5f;
	private static bool IsCoveredWhite(HsvColor hsv) => hsv.H is >= 150 and <= 180 && hsv.S <= 0.15f && hsv.V >= 0.5f;

	private static bool CheckPixel(Image<Rgba32> image, int x, int y, Predicate<HsvColor> predicate)
		=> x is >= 0 and < 256 && y is >= 0 and < 256 && predicate(HsvColor.FromColor(image[x, y]));

	protected internal override float IsModulePresent(Image<Rgba32> image) {
		// The Button: look for the large circle.
		// This is only expected to work under normal lighting (not corrected buzzing/off/emergency lighting)

		// First do a quick check of what the colour is if this is a Button.
		int red = 0, yellow = 0, blue = 0, white = 0;
		image.ProcessPixelRows(a => {
			for (var y = 90; y <= 210; y += 30) {
				var r = a.GetRowSpan(y);
				for (var x = 40; x <= 160; x += 30) {
					var hsv = HsvColor.FromColor(r[x]);
					if (IsCoveredRed(hsv)) red++;
					else if (IsCoveredYellow(hsv)) yellow++;
					else if (IsCoveredBlue(hsv)) blue++;
					else if (IsCoveredWhite(hsv)) white++;
				}
			}
		});

		var conditionsMet = (red >= 10 ? 1 : 0) + (yellow >= 10 ? 1 : 0) + (blue >= 10 ? 1 : 0) + (white >= 10 ? 1 : 0);
		if (conditionsMet != 1)
			return 0;

		Predicate<HsvColor> predicate = red >= 10 ? IsCoveredRed
			: yellow >= 10 ? IsCoveredYellow
			: blue >= 10 ? IsCoveredBlue
			: IsCoveredWhite;

		const int ISOLATION_CHECK_RADIUS = 5;
		var count = 0;
		for (var y = 0; y < 256; y++) {
			for (var x = 0; x < 256; x++) {
				if (predicate(HsvColor.FromColor(image[x, y]))) {
					if (((CheckPixel(image, x - ISOLATION_CHECK_RADIUS * 2, y, predicate) && CheckPixel(image, x - ISOLATION_CHECK_RADIUS, y, predicate)) || (CheckPixel(image, x + ISOLATION_CHECK_RADIUS * 2, y, predicate) && CheckPixel(image, x + ISOLATION_CHECK_RADIUS, y, predicate)))
						&& ((CheckPixel(image, x, y - ISOLATION_CHECK_RADIUS * 2, predicate) && CheckPixel(image, x, y - ISOLATION_CHECK_RADIUS, predicate)) || (CheckPixel(image, x, y + ISOLATION_CHECK_RADIUS * 2, predicate) && CheckPixel(image, x, y + ISOLATION_CHECK_RADIUS, predicate)))) {
						var distSq = (x - 100) * (x - 100) + (y - 150) * (y - 150);
						if (distSq <= 70 * 70) count += 10;
						else if (distSq <= 110 * 110) count += 3;
						else count -= 200;
					}
				}
			}
		}

		return count / 75000f;
	}

	protected internal override ReadData Process(Image<Rgba32> image, LightsState lightsState, ref Image<Rgba32>? debugImage) {
		var hsv = HsvColor.FromColor(ImageUtils.ColourCorrect(image[103, 112], lightsState));
		var colour = hsv.H is >= 345 or <= 15 && hsv.S >= 0.5f ? Colour.Red
			: hsv.H is >= 30 and <= 75 && hsv.S >= 0.7f ? Colour.Yellow
			: hsv.H is >= 210 and <= 240 && hsv.S >= 0.5f ? Colour.Blue
			: hsv.S <= 0.15f && hsv.V >= 0.5f ? Colour.White
			: throw new ArgumentException("Couldn't recognise the button colour.");

		// Check for the indicator.
		hsv = HsvColor.FromColor(image[218, 164]);
		Colour? indicatorColour =
			hsv.S < 0.05f && hsv.V >= 0.75f ? Colour.White
			: hsv.S >= 0.75f && hsv.V >= 0.75f && hsv.H is >= 350 or <= 10 ? Colour.Red
			: hsv.S >= 0.75f && hsv.V >= 0.65f && hsv.H is >= 210 and <= 225 ? Colour.Blue
			: hsv.S >= 0.75f && hsv.V >= 0.65f && hsv.H is >= 45 and <= 60 ? Colour.Yellow
			: hsv.V <= 0.05f ? null
			: throw new ArgumentException("Couldn't recognise the indicator colour");

		if (lightsState == LightsState.Off && colour is Colour.Red or Colour.Blue)
			return new(colour, null, indicatorColour);  // The game hides a white label when the lights are off because of the use of an Unlit shader.

		Predicate<Rgba32> labelPredicate = colour is Colour.Red or Colour.Blue
			? p => HsvColor.FromColor(p) is HsvColor hsv && hsv.S <= 0.25f && hsv.V >= 0.75f
			: p => HsvColor.FromColor(ImageUtils.ColourCorrect(p, lightsState)).V <= 0.25f;

		var textRect = ImageUtils.FindEdges(image, new(32, 128, 144, 48), labelPredicate);
		image.ColourCorrect(lightsState, textRect);
		debugImage?.ColourCorrect(lightsState, textRect);
		debugImage?.Mutate(c => c.Draw(Color.Lime, 1, textRect));

		var text = colour is Colour.Red or Colour.Blue
			? labelRecogniser.Recognise(image, textRect, 100, 255)
			: labelRecogniser.Recognise(image, textRect, 100, 20);
		return new(colour, text, indicatorColour);
	}

	public record ReadData(Colour Colour, string? Label, Colour? IndicatorColour);

	public enum Colour {
		Red,
		Yellow,
		Blue,
		White
	}
}
