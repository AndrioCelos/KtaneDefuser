using System;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace BombDefuserConnector.Components;
public class PianoKeys : ComponentReader<PianoKeys.ReadData> {
	public override string Name => "Piano Keys";
	protected internal override bool UsesNeedyFrame => false;

	protected internal override float IsModulePresent(Image<Rgba32> image) {
		var noteRect = ImageUtils.FindEdges(image, new(24, 24, 160, 64), p => HsvColor.FromColor(p) is var hsv && hsv.H is >= 40 and <= 60 && hsv.S is >= 0.2f and <= 0.3f && hsv.V >= 0.8f);

		return Math.Min(1, Math.Max(0, 7500 - Math.Abs(noteRect.Width * noteRect.Height - 7500)) / 4500f);
	}
	protected internal override ReadData Process(Image<Rgba32> image, LightsState lightsState, ref Image<Rgba32>? debugImage) {
		throw new NotImplementedException();
	}

	public record ReadData();
}
