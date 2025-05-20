using System;
using JetBrains.Annotations;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace KtaneDefuserConnector;
/// <summary>Handles identification and reading of components from images.</summary>
[UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature, ImplicitUseTargetFlags.Itself | ImplicitUseTargetFlags.WithInheritors)]
public abstract class ComponentReader {
	/// <summary>When overridden, returns the name of the component handled by this class.</summary>
	public abstract string Name { get; }

	public virtual ModuleStatus GetStatus(Image<Rgba32> screenshot, Quadrilateral points, LightsState lightsState) => GetStatusFromCorner(screenshot, points, StatusLightLocation.TopRight);

	protected static ModuleStatus GetStatusFromCorner(Image<Rgba32> screenshot, Quadrilateral points, StatusLightLocation statusLightLocation) {
		var (point1, point2) = statusLightLocation switch {
			StatusLightLocation.TopLeft => (points.TopLeft, points.BottomRight),
			StatusLightLocation.TopRight => (points.TopRight, points.BottomLeft),
			StatusLightLocation.BottomLeft => (points.BottomLeft, points.TopRight),
			StatusLightLocation.BottomRight => (points.BottomRight, points.TopLeft),
			_ => throw new ArgumentOutOfRangeException(nameof(statusLightLocation)),
		};

		var x = (int) Math.Round(point1.X + (point2.X - point1.X) * 0.1015625);
		var y = (int) Math.Round(point1.Y + (point2.Y - point1.Y) * 0.1015625);
		return HsvColor.FromColor(screenshot[x, y]) switch {
			{ H: >= 105 and <= 150, S: >= 0.85f, V: >= 0.5f } => ModuleStatus.Solved,
			{ H: >= 330 or <= 30, S: >= 0.65f, V: >= 0.75f } => ModuleStatus.Strike,
			_ => ModuleStatus.Off
		};
	}

	protected static Point FindSelectionHighlight(Image<Rgba32> image, LightsState lightsState, int left256, int top256, int right256, int bottom256) {
		var left = left256 * image.Width / 256;
		var top = top256 * image.Height / 256;
		var right = right256 * image.Width / 256;
		var bottom = bottom256 * image.Height / 256;
		Point point = default;
		image.ProcessPixelRows(a => {
			for (var y = top; y < bottom; y += 4) {
				var row = a.GetRowSpan(y);
				for (var x = left; x < right; x++) {
					if (!row[x].IsSelectionHighlight(lightsState)) continue;
					point = new(x * 256 / a.Width, y * 256 / a.Height);
					return;
				}
			}
		});
		return point;
	}

	/// <summary>Returns the number of lit lights on the module's stage indicator.</summary>
	[Pure]
	protected static int ReadStageIndicator(Image<Rgba32> image) {
		var count = 0;
		var lastState = false;
		foreach (var y in image.Height.MapRange(80, 224)) {
			var hsv = HsvColor.FromColor(image[218 * image.Width / 256, y]);
			if (hsv.H is >= 60 and <= 135 && hsv.S >= 0.25f) {
				if (lastState) continue;
				lastState = true;
				count++;
			} else
				lastState = false;
		}
		return count;
	}

	[MustDisposeResource]
	protected static Image<Rgba32> GetNeedyDisplayImage(Image<Rgba32> image, LightsState lightsState, Image<Rgba32>? debugImage) {
		var bezelCorners = ImageUtils.FindCorners(image, image.Map(72, 16, 120, 64), c => HsvColor.FromColor(ImageUtils.ColourCorrect(c, lightsState)) is var hsv && hsv.H >= (lightsState == LightsState.Emergency ? 15 : 45) && hsv is { H: <= 150, S: <= 0.25f, V: >= 0.3f and <= 0.9f }, 4);
		debugImage?.DebugDrawPoints(bezelCorners);
		var left = Math.Min(bezelCorners.TopLeft.X, bezelCorners.BottomLeft.X);
		var top = Math.Min(bezelCorners.TopLeft.Y, bezelCorners.TopRight.Y);
		var right = Math.Max(bezelCorners.TopRight.X, bezelCorners.BottomRight.X);
		var bottom = Math.Max(bezelCorners.BottomLeft.Y, bezelCorners.BottomRight.Y);
		var bezelRectangle = new Rectangle(left, top, right - left, bottom - top);
		bezelRectangle.Inflate(-4, -4);
		var displayCorners = ImageUtils.FindCorners(image, bezelRectangle, c => HsvColor.FromColor(ImageUtils.ColourCorrect(c, lightsState)) is { H: >= 345 or <= 15, S: >= 0.75f } or { V: <= 0.10f }, 4);
		debugImage?.DebugDrawPoints(displayCorners);

		var displayImage = ImageUtils.PerspectiveUndistort(image, displayCorners, InterpolationMode.NearestNeighbour, new(128, 64));
		debugImage?.Mutate(p => p.DrawImage(displayImage, 1));
		return displayImage;
	}

	/// <summary>Returns the number displayed on the module's needy timer, or null if it is blank.</summary>
	[MustUseReturnValue]
	protected static int? ReadNeedyTimer(Image<Rgba32> image, LightsState lightsState, Image<Rgba32>? debugImage) {
		using var displayImage = GetNeedyDisplayImage(image, lightsState, debugImage);

		var d0 = ReadDigit(88);
		var d1 = ReadDigit(42);
		return d0 is null && d1 is null
			? null
			: d0 is not null
			? d1 is not null ? d0 + d1 * 10 : d0
			: throw new ArgumentException("Can't read needy timer");

		bool CheckRectangle(Rectangle rectangle) {
			for (var dy = 0; dy < rectangle.Height; dy++) {
				for (var dx = 0; dx < rectangle.Width; dx++) {
					var p = displayImage[rectangle.Left + dx, rectangle.Top + dy];
					if (p.R >= 128) return true;
				}
			}
			return false;
		}
		int? ReadDigit(int x) {
			var segments =
				(CheckRectangle(new(x + 0, 0, 1, 16)) ? (1 << 0) : 0) |
				(CheckRectangle(new(x + 0, 18, 16, 1)) ? (1 << 1) : 0) |
				(CheckRectangle(new(x + 0, 46, 16, 1)) ? (1 << 2) : 0) |
				(CheckRectangle(new(x + 0, 48, 1, 16)) ? (1 << 3) : 0) |
				(CheckRectangle(new(x - 16, 46, 16, 1)) ? (1 << 4) : 0) |
				(CheckRectangle(new(x - 16, 18, 16, 1)) ? (1 << 5) : 0) |
				(CheckRectangle(new(x + 0, 25, 1, 16)) ? (1 << 6) : 0);
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
	}

	protected enum StatusLightLocation {
		TopLeft,
		TopRight,
		BottomLeft,
		BottomRight
	}
}

/// <summary>A <see cref="ComponentReader"/> that represents component data as the specified type.</summary>
public abstract class ComponentReader<T> : ComponentReader where T : ComponentReadData {
	/// <inheritdoc cref="ComponentReader{T}.Process(Image{Rgba32}, LightsState, ref Image{Rgba32}?)"/>/>
	[MustUseReturnValue]
	protected internal T Process(Image<Rgba32> image, LightsState lightsState) {
		Image<Rgba32>? debugImage = null;
		return Process(image, lightsState, ref debugImage);
	}
	/// <summary>When overridden, reads component data from the specified image.</summary>
	/// <param name="image">The image to read component data from.</param>
	/// <param name="lightsState">The lights state in the provided image.</param>
	/// <param name="debugImage">An image variable to draw debug annotations to. The image may be replaced with a larger one. It may be a variable containing <see langword="null"/> to disable debug annotations.</param>
	[MustUseReturnValue]
	protected internal abstract T Process(Image<Rgba32> image, LightsState lightsState, ref Image<Rgba32>? debugImage);
}
