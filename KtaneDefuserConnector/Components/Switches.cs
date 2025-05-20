using System;
using System.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace KtaneDefuserConnector.Components;
public class Switches : ComponentReader<Switches.ReadData> {
	public override string Name => "Switches";

	protected internal override ReadData Process(Image<Rgba32> image, LightsState lightsState, ref Image<Rgba32>? debugImage) {
		var currentState = new bool[5];
		var targetState = new bool[5];
		for (var i = 0; i < 5; i++) {
			var x = i switch { 0 => 41, 1 => 84, 2 => 128, 3 => 172, _ => 218 } * image.Width / 256;
			if (ImageUtils.ColourCorrect(image[x, 112 * image.Height / 256], lightsState).G < 64)
				currentState[i] = true;
			else if (ImageUtils.ColourCorrect(image[x, 192 * image.Height / 256], lightsState).G >= 64)
				throw new ArgumentException($"Can't read switch {i + 1}.", nameof(image));

			if (image[x, 83 * image.Height / 256].G >= 224)
				targetState[i] = true;
			else if (image[x, 219 * image.Height / 256].G < 224)
				throw new ArgumentException($"Can't read lights {i + 1}.", nameof(image));
		}

		var highlight = FindSelectionHighlight(image, lightsState, 32, 112, 240, 208);
		Point? selection = highlight.Y == 0 ? null : new(highlight.X switch { < 56 => 0, < 98 => 1, < 142 => 2, < 186 => 3, _ => 4 }, 0);

		return new(selection, currentState, targetState);
	}

	public record ReadData(Point? Selection, bool[] CurrentState, bool[] TargetState) : ComponentReadData(Selection) {
		public override string ToString() => $"ReadData {{ {nameof(CurrentState)} = {string.Join(null, from b in CurrentState select b ? '^' : 'v')}, {nameof(TargetState)} = {string.Join(null, from b in TargetState select b ? '^' : 'v')}, {nameof(Selection)} = {Selection} }}";
	}
}
