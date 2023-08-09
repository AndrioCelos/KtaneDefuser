using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using static BombDefuserConnector.LightsState;

namespace BombDefuserConnector;
/// <summary>Handles identification and reading of widgets from images.</summary>
public abstract class WidgetReader {
	/// <summary>When overridden, returns the name of the widget handled by this class.</summary>
	public abstract string Name { get; }

	/// <summary>Returns a value indicating how much the specified image looks like this widget type.</summary>
	/// <returns>Generally this will range from 0 to 1, though it isn't strictly bounded.</returns>
	protected internal abstract float IsWidgetPresent(Image<Rgba32> image, LightsState lightsState, PixelCounts pixelCounts);

	protected internal abstract object ProcessNonGeneric(Image<Rgba32> image, LightsState lightsState, ref Image<Rgba32>? debugImage);

	internal static PixelCounts GetPixelCounts(Image<Rgba32> image, LightsState lightsState) {
		int red = 0, yellow = 0, grey = 0, white = 0;
		for (var y = 0; y < image.Width; y++) {
			for (var x = 0; x < image.Height; x++) {
				var hsv = HsvColor.FromColor(image[x, y]);
				if (hsv.S <= lightsState switch { Off => 0.75f, Emergency => 0.6f, _ => 0.3f }) {
					switch (lightsState) {
						case Buzz:
							switch (hsv.V) {
								case >= 0.15f and <= 0.18f when hsv.H < 120: white++; break;  // Exclude non-white pixels lit by indicator lights
								case >= 0.04f and <= 0.06f when hsv.H < 180: grey++; break;
							}
							break;
						case Off:
							switch (hsv.V) {
								case >= 0.025f when hsv.S <= 0.25f: white++; break;
								case >= 0.01f and <= 0.02f when hsv.H < 210: grey++; break;
							}
							break;
						case Emergency:
							switch (hsv.V) {
								case >= 0.9f when hsv.H < 60: white++; break;
								case >= 0.3f and <= 0.5f when hsv.H < 60: grey++; break;
							}
							break;
						default:
							switch (hsv.V) {
								case >= 0.7f when hsv.S < 0.15f && hsv.H < 180: white++; break;
								case >= 0.25f and <= 0.4f when hsv.H < 180: grey++; break;
							}
							break;
					}
				} else {
					if (lightsState == Emergency) {
						switch (hsv.H) {
							case <= 10 when hsv.S >= 0.8f: red++; break;
							case >= 10 and <= 60 when hsv.S >= 0.8f: yellow++; break;
						}
					} else {
						switch (hsv.H) {
							case >= 345 or <= 15: red++; break;
							case >= 30 and <= 60 when hsv.S >= 0.7f: yellow++; break;
						}
					}
				}
			}
		}
		return new(red, yellow, grey, white);
	}

	public record PixelCounts(int Red, int Yellow, int Grey, int White);
}

/// <summary>A <see cref="WidgetReader"/> that represents widget data as the specified type.</summary>
public abstract class WidgetReader<T> : WidgetReader where T : notnull {
	/// <summary>When overridden, reads widget data from the specified image.</summary>
	/// <param name="image">The image to read widget data from.</param>
	/// <param name="debugImage">An image variable to draw debug annotations to. The image may be replaced with a larger one. May be a variable containing <see langword="null"/> to disable debug annotations.</param>
	protected internal abstract T Process(Image<Rgba32> image, LightsState lightsState, ref Image<Rgba32>? debugImage);

	protected internal override object ProcessNonGeneric(Image<Rgba32> image, LightsState lightsState, ref Image<Rgba32>? debugImage)
		=> this.Process(image, lightsState, ref debugImage);
}