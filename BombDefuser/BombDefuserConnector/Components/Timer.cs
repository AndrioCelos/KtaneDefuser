using System;
using System.Text;
using BombDefuserConnector.Properties;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace BombDefuserConnector.Components;
public class Timer : ComponentProcessor<Timer.ReadData> {
	private static readonly Image<Rgba32>[] Samples = new[] { Image.Load<Rgba32>(Resources.Timer1), Image.Load<Rgba32>(Resources.Timer2) };

	public override string Name => "Timer";
	protected internal override bool UsesNeedyFrame => false;

	protected internal override float IsModulePresent(Image<Rgb24> image) {
		return ImageUtils.CheckSimilarity(image, Samples);
	}

	protected internal override ReadData Process(Image<Rgb24> image, ref Image<Rgb24>? debugBitmap) {
		static bool predicate(Rgb24 c) {
			return c.G < 12 && c.B < 12;
		}

		var timerCorners = ImageUtils.FindCorners(image, new(16, 96, 224, 144), predicate, false);
		using var timerBitmap = ImageUtils.PerspectiveUndistort(image, timerCorners, InterpolationMode.NearestNeighbour, new(256, 128));

		Point[]? strikesCorners = null;
		try {
			strikesCorners = ImageUtils.FindCorners(image, new(88, 16, 96, 64), predicate, false);
		} catch { }
		using var strikesBitmap = strikesCorners is not null ? ImageUtils.PerspectiveUndistort(image, strikesCorners, InterpolationMode.NearestNeighbour, new(128, 64)) : null;

		if (debugBitmap is not null) {
			ImageUtils.DebugDrawPoints(debugBitmap, timerCorners);
			debugBitmap.Mutate(c => c.Resize(new ResizeOptions() { Size = new(512, 512), Mode = ResizeMode.BoxPad, Position = AnchorPositionMode.TopLeft, PadColor = Color.Transparent }).DrawImage(timerBitmap, new Point(0, 256), 1));
			if (strikesBitmap is not null) {
				ImageUtils.DebugDrawPoints(debugBitmap, strikesCorners!);
				debugBitmap.Mutate(c => c.Brightness(1.5f).DrawImage(strikesBitmap, new Point(0, 384), 1));
			}
		}

		static bool isOn(Rgb24 pixel, ref Rgb24 timerColour) {
			if (pixel.R >= 128 || pixel.G >= 128 || pixel.B >= 128) {
				timerColour.R = pixel.R >= 128 ? byte.MaxValue : (byte) 0;
				timerColour.G = pixel.G >= 128 ? byte.MaxValue : (byte) 0;
				timerColour.B = pixel.B >= 128 ? byte.MaxValue : (byte) 0;
				return true;
			}
			return false;
		}

		static char readDigit(Image<Rgb24> image, int centreX, ref Rgb24 timerColour) {
			var segments =
				(isOn(image[centreX + 0, 16], ref timerColour) ? (1 << 0) : 0) |
				(isOn(image[centreX + 16, 40], ref timerColour) ? (1 << 1) : 0) |
				(isOn(image[centreX + 16, 92], ref timerColour) ? (1 << 2) : 0) |
				(isOn(image[centreX + 0, 114], ref timerColour) ? (1 << 3) : 0) |
				(isOn(image[centreX - 18, 92], ref timerColour) ? (1 << 4) : 0) |
				(isOn(image[centreX - 18, 40], ref timerColour) ? (1 << 5) : 0) |
				(isOn(image[centreX + 0, 64], ref timerColour) ? (1 << 6) : 0);
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

		var timerColour = default(Rgb24);
		var isMinutes = isOn(timerBitmap[130, 38], ref timerColour);

		var n1 = int.Parse($"{readDigit(timerBitmap, 32, ref timerColour)}{readDigit(timerBitmap, 90, ref timerColour)}");
		var n2 = int.Parse($"{readDigit(timerBitmap, 170, ref timerColour)}{readDigit(timerBitmap, 228, ref timerColour)}");

		var s = new StringBuilder();

		var strikes = strikesBitmap is null
			? 0
			: isOn(strikesBitmap[34, 34], ref timerColour)
			? isOn(strikesBitmap[94, 32], ref timerColour) ? 2 : 1
			: 0;

		var gameMode = timerColour.B > 0
			? timerColour.R > 0 ? GameMode.Training : GameMode.Zen
			: timerColour.G > 0 ? (timerColour.R > 0 ? GameMode.Time : GameMode.Steady)
			: GameMode.Normal;

		return new(gameMode, isMinutes ? (n1 * 60 + n2) : n1, isMinutes ? 0 : n2, strikes);
	}

	public record ReadData(GameMode gameMode, int time, int cs, int strikes) {
		public override string ToString() => $"{gameMode} {time} {cs} {strikes}";
	}

	public enum GameMode {
		Normal,
		Time,
		Zen,
		Steady,
		Training
	}
}
