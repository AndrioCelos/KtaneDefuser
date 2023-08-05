using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace BombDefuserConnector.Components;
public class NeedyKnob : ComponentReader<NeedyKnob.ReadData> {
	public override string Name => "Needy Knob";
	protected internal override bool UsesNeedyFrame => true;

	protected internal override float IsModulePresent(Image<Rgb24> image) {
		var knobCount = 0;
		var ledCount = 0;
		image.ProcessPixelRows(a => {
			for (var y = 120; y < 160; y++) {
				var row = a.GetRowSpan(y);
				for (var x = 108; x < 148; x++) {
					var hsv = HsvColor.FromColor(row[x]);
					if (hsv.H is >= 330 or <= 30 && hsv.S is >= 0.75f && hsv.V >= 0.3f)
						knobCount++;
				}
			}
			for (var y = 160; y < a.Height; y++) {
				var row = a.GetRowSpan(y);
				for (var x = 0; x < a.Width; x++) {
					var hsv = HsvColor.FromColor(row[x]);
					if ((hsv.H is >= 120 and <= 150 && hsv.S >= 0.5f && hsv.V <= 0.3f)
						|| (hsv.H is >= 75 and <= 120 && hsv.S >= 0.5f && hsv.V >= 0.8f))
						ledCount++;
				}
			}
		});
		return knobCount / 1200f + ledCount / 3000f;
	}

	// TODO: These rectangles were chosen with respect to viewing angles that can be seen on the vanilla bomb.
	// It should be fine with larger bombs, but should be tested.
	private static readonly Rectangle[] rectangles = new Rectangle[] {
		new(58, 169, 8, 8),
		new(73, 191, 8, 8),
		new(96, 206, 8, 8),
		new(148, 206, 8, 8),
		new(171, 191, 8, 8),
		new(185, 169, 8, 8),
		new(40, 178, 8, 8),
		new(59, 206, 8, 8),
		new(88, 226, 8, 8),
		new(156, 226, 8, 8),
		new(185, 206, 8, 8),
		new(203, 178, 8, 8)
	};
	protected internal override ReadData Process(Image<Rgb24> image, ref Image<Rgb24>? debugImage) {
		var time = ReadNeedyTimer(image, debugImage);
		var lights = new bool[12];

		bool isRectangleLit(PixelAccessor<Rgb24> a, Rectangle rectangle) {
			var count = 0;
			for (var dy = 0; dy < rectangle.Height; dy++) {
				var r = a.GetRowSpan(rectangle.Y + dy);
				for (var dx = 0; dx < rectangle.Width; dx++) {
					var p = r[rectangle.X + dx];
					//var hsv = HsvColor.FromColor(p);
					//if (hsv.H is >= 75 and <= 150 && hsv.S >= 0.5f && hsv.V >= 0.5f) {
					if (p.R >= 64 && p.G >= 176 && p.B is >= 16 and < 96) {
						count++;
						if (count >= 16) return true;
					}
				}
			}
			return false;
		}

		image.ProcessPixelRows(a => {
			for (var i = 0; i < 12; i++) {
				lights[i] = isRectangleLit(a, rectangles[i]);
			}
		});
		if (debugImage != null) {
			for (var i = 0; i < 12; i++) {
				debugImage.Mutate(p => p.Draw(lights[i] ? Color.Lime : Color.Grey, 1, rectangles[i]));
			}
		}

		return new(time, lights);
	}

	public record ReadData(int? Time, bool[] Lights) {
		public override string ToString() => $"ReadData {{ Time = {this.Time}, Lights = ({string.Join(", ", this.Lights)}) }}";
	}
}
