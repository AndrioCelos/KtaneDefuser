using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace KtaneDefuserConnector.Components;
public class MorseCode : ComponentReader<MorseCode.ReadData> {
	public override string Name => "Morse Code";

	protected internal override ReadData Process(Image<Rgba32> image, LightsState lightsState, ref Image<Rgba32>? debugImage) {
		Point? selection
			= FindSelectionHighlight(image, lightsState, 32, 128, 56, 172).Y != 0 ? new Point(0, 0)
			: FindSelectionHighlight(image, lightsState, 192, 128, 216, 172).Y != 0 ? new Point(2, 0)
			: FindSelectionHighlight(image, lightsState, 86, 180, 172, 234).Y != 0 ? new Point(1, 1)
			: null;

		return new(selection, image[89 * image.Width / 256, 39 * image.Height / 256].R >= 192);
	}

	public record ReadData(Point? Selection, bool IsLightOn) : ComponentReadData(Selection);
}
