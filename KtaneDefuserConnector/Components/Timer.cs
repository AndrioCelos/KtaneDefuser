using System;
using KtaneDefuserConnector.DataTypes;
using KtaneDefuserConnector.Properties;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace KtaneDefuserConnector.Components;
public class Timer : ComponentReader<Timer.ReadData> {
	private static readonly Image<Rgba32>[] Samples = [Image.Load<Rgba32>(Resources.Timer1), Image.Load<Rgba32>(Resources.Timer2)];

	public override string Name => "Timer";
	protected internal override ComponentFrameType FrameType => ComponentFrameType.Timer;

	protected internal override float IsModulePresent(Image<Rgba32> image)
		=> ImageUtils.TryFindCorners(image, new(16, 96, 224, 144), IsTimerBackground, 0, out _) ? ImageUtils.CheckSimilarity(image, Samples) * 1.5f : 0;

	private static bool IsTimerBackground(Rgba32 c) => c is { G: < 12, B: < 12 };

	protected internal override ReadData Process(Image<Rgba32> image, LightsState lightsState, ref Image<Rgba32>? debugImage) {
		var timerCorners = ImageUtils.FindCorners(image, new(16, 96, 224, 144), IsTimerBackground, 0);
		using var timerBitmap = ImageUtils.PerspectiveUndistort(image, timerCorners, InterpolationMode.NearestNeighbour, new(256, 128));

		using var strikesBitmap = ImageUtils.TryFindCorners(image, new(88, 16, 96, 64), IsTimerBackground, 0, out var strikesCorners)
			? ImageUtils.PerspectiveUndistort(image, strikesCorners, InterpolationMode.NearestNeighbour, new(128, 64))
			: null;

		if (debugImage is not null) {
			debugImage.DebugDrawPoints(timerCorners);
			debugImage.Mutate(c => c.Resize(new ResizeOptions() { Size = new(512, 512), Mode = ResizeMode.BoxPad, Position = AnchorPositionMode.TopLeft, PadColor = Color.Transparent }).DrawImage(timerBitmap, new Point(0, 256), 1));
			if (strikesBitmap is not null) {
				debugImage.DebugDrawPoints(strikesCorners);
				debugImage.Mutate(c => c.Brightness(1.5f).DrawImage(strikesBitmap, new Point(0, 384), 1));
			}
		}

		static bool IsOn(Rgba32 pixel, ref Rgba32 timerColour) {
			if (pixel is { R: < 128, G: < 128, B: < 128 }) return false;
			timerColour.R = pixel.R >= 128 ? byte.MaxValue : (byte) 0;
			timerColour.G = pixel.G >= 128 ? byte.MaxValue : (byte) 0;
			timerColour.B = pixel.B >= 128 ? byte.MaxValue : (byte) 0;
			return true;
		}

		static char ReadDigit(Image<Rgba32> image, int centreX, ref Rgba32 timerColour) {
			var segments =
				(IsOn(image[centreX + 0, 16], ref timerColour) ? (1 << 0) : 0) |
				(IsOn(image[centreX + 16, 40], ref timerColour) ? (1 << 1) : 0) |
				(IsOn(image[centreX + 16, 92], ref timerColour) ? (1 << 2) : 0) |
				(IsOn(image[centreX + 0, 114], ref timerColour) ? (1 << 3) : 0) |
				(IsOn(image[centreX - 18, 92], ref timerColour) ? (1 << 4) : 0) |
				(IsOn(image[centreX - 18, 40], ref timerColour) ? (1 << 5) : 0) |
				(IsOn(image[centreX + 0, 64], ref timerColour) ? (1 << 6) : 0);
			return segments switch {
				0b0111111 => '0',
				0b0000110 => '1',
				0b1011011 => '2',
				0b1001111 => '3',
				0b1100110 => '4',
				0b1101101 => '5',
				0b1111101 => '6',
				0b0000111 => '7',
				0b1111111 => '8',
				0b1101111 => '9',
				_ => throw new ArgumentException($"Couldn't read pattern: {segments:x}")
			};
		}

		var timerColour = default(Rgba32);
		var isMinutes = IsOn(timerBitmap[130, 38], ref timerColour);

		var n1 = int.Parse($"{ReadDigit(timerBitmap, 32, ref timerColour)}{ReadDigit(timerBitmap, 90, ref timerColour)}");
		var n2 = int.Parse($"{ReadDigit(timerBitmap, 170, ref timerColour)}{ReadDigit(timerBitmap, 228, ref timerColour)}");

		var strikes = strikesBitmap is null
			? 0
			: IsOn(strikesBitmap[34, 34], ref timerColour)
			? IsOn(strikesBitmap[94, 32], ref timerColour) ? 2 : 1
			: 0;

		var gameMode = timerColour.B > 0
			? timerColour.R > 0 ? GameMode.Training : GameMode.Zen
			: timerColour.G > 0 ? (timerColour.R > 0 ? GameMode.Time : GameMode.Steady)
			: GameMode.Normal;

		return new(gameMode, isMinutes ? n1 * 60 + n2 : n1, isMinutes ? 0 : n2, strikes);
	}

	public record ReadData(GameMode GameMode, int Time, int CS, int Strikes) {
		public override string ToString() => $"{GameMode} {Time} {CS} {Strikes}";
	}
}
