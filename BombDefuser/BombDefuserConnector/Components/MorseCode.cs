using System;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace BombDefuserConnector.Components;
public class MorseCode : ComponentReader<MorseCode.ReadData> {
	public override string Name => "Morse Code";
	protected internal override bool UsesNeedyFrame => false;

	protected internal override float IsModulePresent(Image<Rgba32> image) {
		// Morse Code: look for the orange display pixels
		var count = 0f;
		for (var y = 144; y < 160; y++) {
			for (var x = 64; x < 192; x++) {
				var hsv = HsvColor.FromColor(image[x, y]);
				count += Math.Max(0, 1 - Math.Abs(hsv.H - 27) * 0.1f) * hsv.S * hsv.V;
			}
		}
		return Math.Min(count / 600, 1);
	}
	protected internal override ReadData Process(Image<Rgba32> image, ref Image<Rgba32>? debugImage)
		=> new(image[89, 39].R >= 192);

	public record ReadData(bool IsLightOn);
}
