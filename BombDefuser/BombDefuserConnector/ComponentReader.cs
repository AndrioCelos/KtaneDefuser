using System;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace BombDefuserConnector;
/// <summary>Handles identification and reading of components from images.</summary>
public abstract class ComponentReader {
	/// <summary>When overridden, returns the name of the component handled by this class.</summary>
	public abstract string Name { get; }
	/// <summary>When overridden, returns a value indicating whether the component handled by this class uses the needy module frame.</summary>
	protected internal abstract bool UsesNeedyFrame { get; }

	/// <summary>Returns a value indicating how much the specified image looks like this component type.</summary>
	/// <returns>Generally this will range from 0 to 1, though it isn't strictly bounded. The image should always be under normal lighting because this is used at the start of the game.</returns>
	protected internal abstract float IsModulePresent(Image<Rgba32> image);

	protected internal abstract object ProcessNonGeneric(Image<Rgba32> image, LightsState lightsState, ref Image<Rgba32>? debugImage);

	/// <summary>Returns the number of lit lights on the module's stage indicator.</summary>
	protected static int ReadStageIndicator(Image<Rgba32> image) {
		var count = 0;
		var lastState = false;
		for (var y = 80; y < 224; y++) {
			var hsv = HsvColor.FromColor(image[218, y]);
			if (hsv.H is >= 60 and <= 135 && hsv.S >= 0.25f) {
				if (!lastState) {
					lastState = true;
					count++;
				}
			} else
				lastState = false;
		}
		return count;
	}

	/// <summary>Returns the number displayed on the module's needy timer, or null if it is blank.</summary>
	protected static int? ReadNeedyTimer(Image<Rgba32> image, LightsState lightsState, Image<Rgba32>? debugImage) {
		var bezelCorners = ImageUtils.FindCorners(image, new(80, 16, 96, 64), c => HsvColor.FromColor(ImageUtils.ColourCorrect(c, lightsState)) is var hsv && hsv.H >= (lightsState == LightsState.Emergency ? 15 : 45) && hsv.H <= 150 && hsv.S <= 0.25f && hsv.V is >= 0.3f and <= 0.9f, 4) ?? throw new ArgumentException("Can't find needy timer bezel corners");
		debugImage?.DebugDrawPoints(bezelCorners);
		var left = Math.Min(bezelCorners[0].X, bezelCorners[2].X);
		var top = Math.Min(bezelCorners[0].Y, bezelCorners[1].Y);
		var right = Math.Max(bezelCorners[1].X, bezelCorners[3].X) + 1;
		var bottom = Math.Max(bezelCorners[2].Y, bezelCorners[3].Y) + 1;
		var bezelRectangle = new Rectangle(left, top, right - left, bottom - top);
		bezelRectangle.Inflate(-4, -4);
		var displayCorners = ImageUtils.FindCorners(image, bezelRectangle, c => HsvColor.FromColor(ImageUtils.ColourCorrect(c, lightsState)) is var hsv && (hsv.H is >= 345 or <= 15 && hsv.S >= 0.75f) || hsv.V <= 0.10f, 4) ?? throw new ArgumentException("Can't find needy timer corners");
		debugImage?.DebugDrawPoints(displayCorners);

		var displayImage = ImageUtils.PerspectiveUndistort(image, displayCorners, InterpolationMode.NearestNeighbour, new(128, 64));
		debugImage?.Mutate(p => p.DrawImage(displayImage, 0));

		bool checkRectangle(Image<Rgba32> image, Rectangle rectangle) {
			for (var dy = 0; dy < rectangle.Height; dy++) {
				for (var dx = 0; dx < rectangle.Width; dx++) {
					var p = image[rectangle.Left + dx, rectangle.Top + dy];
					if (p.R >= 128) return true;
				}
			}
			return false;
		}
		int? readDigit(Image<Rgba32> image, int x) {
			var segments =
				(checkRectangle(image, new(x + 0, 0, 1, 16)) ? (1 << 0) : 0) |
				(checkRectangle(image, new(x + 0, 18, 16, 1)) ? (1 << 1) : 0) |
				(checkRectangle(image, new(x + 0, 46, 16, 1)) ? (1 << 2) : 0) |
				(checkRectangle(image, new(x + 0, 48, 1, 16)) ? (1 << 3) : 0) |
				(checkRectangle(image, new(x - 16, 46, 16, 1)) ? (1 << 4) : 0) |
				(checkRectangle(image, new(x - 16, 18, 16, 1)) ? (1 << 5) : 0) |
				(checkRectangle(image, new(x + 0, 25, 1, 16)) ? (1 << 6) : 0);
			return segments switch {
				0b0111111 => 0,
				0b0000110 => 1,
				0b1011011 => 2,
				0b1001111 => 3,
				0b1100110 => 4,
				0b1101101 => 5,
				0b1111101 => 6,
				0b0000111 => 7,
				0b1111111 => 8,
				0b1101111 => 9,
				0 => null,
				_ => throw new ArgumentException($"Couldn't read pattern: {segments:x}")
			};
		}

		var d0 = readDigit(displayImage, 88);
		var d1 = readDigit(displayImage, 42);
		return d0 is null && d1 is null
			? null
			: d0 is not null
			? d1 is not null ? d0 + d1 * 10 : d0
			: throw new ArgumentException("Can't read needy timer");
	}
}

/// <summary>A <see cref="ComponentReader"/> that represents component data as the specified type.</summary>
public abstract class ComponentReader<T> : ComponentReader where T : notnull {
	/// <summary>When overridden, reads component data from the specified image.</summary>
	/// <param name="image">The image to read component data from.</param>
	/// <param name="debugImage">An image variable to draw debug annotations to. The image may be replaced with a larger one. May be a variable containing <see langword="null"/> to disable debug annotations.</param>
	protected internal abstract T Process(Image<Rgba32> image, LightsState lightsState, ref Image<Rgba32>? debugImage);

	protected internal override object ProcessNonGeneric(Image<Rgba32> image, LightsState lightsState, ref Image<Rgba32>? debugImage)
		=> this.Process(image, lightsState, ref debugImage);
}
