using System;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace KtaneDefuserConnector.Components;
public class Button : ComponentReader<Button.ReadData> {
	private static readonly TextRecogniser LabelRecogniser = new(new(TextRecogniser.Fonts.OstrichSansHeavy, 24), 160, 44, new(128, 64),
		  "ABORT", "DETONATE", "HOLD", "PRESS");

	public override string Name => "The Button";

	protected internal override ReadData Process(Image<Rgba32> image, LightsState lightsState, ref Image<Rgba32>? debugImage) {
		var hsv = HsvColor.FromColor(ImageUtils.ColourCorrect(image[103 * image.Width / 256, 112 * image.Height / 256], lightsState));
		var colour = hsv switch {
			{ H: >= 345 or <= 15, S: >= 0.5f } => Colour.Red,
			{ H: >= 30 and <= 75, S: >= 0.7f } => Colour.Yellow,
			{ H: >= 210 and <= 240, S: >= 0.5f } => Colour.Blue,
			{ S: <= 0.15f, V: >= 0.5f } => Colour.White,
			_ => throw new ArgumentException("Couldn't recognise the button colour.")
		};

		// Check for the indicator.
		hsv = HsvColor.FromColor(image[218 * image.Width / 256, 164 * image.Height / 256]);
		Colour? indicatorColour = hsv switch {
			{ S: < 0.05f, V: >= 0.75f } => Colour.White,
			{ S: >= 0.75f, V: >= 0.75f, H: >= 350 or <= 10 } => Colour.Red,
			{ S: >= 0.75f, V: >= 0.65f, H: >= 210 and <= 225 } => Colour.Blue,
			{ S: >= 0.75f, V: >= 0.65f, H: >= 45 and <= 60 } => Colour.Yellow,
			{ V: <= 0.05f } => null,
			_ => throw new ArgumentException("Couldn't recognise the indicator colour")
		};

		if (lightsState == LightsState.Off && colour is Colour.Red or Colour.Blue)
			return new(colour, null, indicatorColour);  // The game hides a white label when the lights are off because of the use of an Unlit shader.

		Predicate<Rgba32> labelPredicate = colour is Colour.Red or Colour.Blue
			? p => HsvColor.FromColor(p) is { S: <= 0.25f, V: >= 0.75f }
			: p => HsvColor.FromColor(ImageUtils.ColourCorrect(p, lightsState)).V <= 0.25f;

		var textRect = ImageUtils.FindEdges(image,  image.Map(32, 128, 144, 48), labelPredicate);
		image.ColourCorrect(lightsState, textRect);
		debugImage?.ColourCorrect(lightsState, textRect);
		debugImage?.Mutate(c => c.Draw(Color.Lime, 1, textRect));

		var text = colour is Colour.Red or Colour.Blue
			? LabelRecogniser.Recognise(image, textRect, 100, 255)
			: LabelRecogniser.Recognise(image, textRect, 100, 20);
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
