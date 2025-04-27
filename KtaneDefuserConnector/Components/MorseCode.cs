using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace KtaneDefuserConnector.Components;
public class MorseCode : ComponentReader<MorseCode.ReadData> {
	public override string Name => "Morse Code";

	protected internal override ReadData Process(Image<Rgba32> image, LightsState lightsState, ref Image<Rgba32>? debugImage)
		=> new(image[89, 39].R >= 192);

	public record ReadData(bool IsLightOn);
}
