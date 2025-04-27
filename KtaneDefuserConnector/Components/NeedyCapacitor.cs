using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace KtaneDefuserConnector.Components;
public class NeedyCapacitor : ComponentReader<NeedyCapacitor.ReadData> {
	public override string Name => "Needy Capacitor";

	protected internal override ReadData Process(Image<Rgba32> image, LightsState lightsState, ref Image<Rgba32>? debugImage) => new(ReadNeedyTimer(image, lightsState, debugImage));

	public record ReadData(int? Time);
}
