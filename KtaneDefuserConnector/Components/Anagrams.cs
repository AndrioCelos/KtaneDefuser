using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace KtaneDefuserConnector.Components;
public class Anagrams : WordScramble {
	public override string Name => "Anagrams";
	public override ModuleStatus GetStatus(Image<Rgba32> screenshot, Quadrilateral points, LightsState lightsState) => GetStatusFromCorner(screenshot, points, StatusLightLocation.TopLeft);
}
