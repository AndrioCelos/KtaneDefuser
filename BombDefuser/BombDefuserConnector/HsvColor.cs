
using System;
using System.Drawing;
using SixLabors.ImageSharp.PixelFormats;

namespace BombDefuserConnector;
public struct HsvColor {
	public byte A;
	public float H;
	public float S;
	public float V;

	public HsvColor(byte a, float h, float s, float v) {
		this.A = a;
		this.H = h;
		this.S = s;
		this.V = v;
	}

	public static HsvColor FromColor(Color color)
		=> FromArgb(color.A, color.R, color.G, color.B);
	public static HsvColor FromColor(Rgba32 pixel)
		=> FromArgb(pixel.A, pixel.R, pixel.G, pixel.B);
	public static HsvColor FromArgb(byte a, byte r, byte g, byte b) {
		var rf = r / 255f;
		var gf = g / 255f;
		var bf = b / 255f;

		var min = Math.Min(Math.Min(r, g), b) / 255f;
		var max = Math.Max(Math.Max(r, g), b) / 255f;
		var delta = max - min;

		var H = delta == 0 ? 0
			: rf == max ? 60 * (gf - bf) / delta
			: gf == max ? 60 * ((bf - rf) / delta + 2)
			: 60 * ((rf - gf) / delta + 4);
		if (H < 0) H += 360;
		var S = max == 0 ? 0 : delta / max;
		var V = max;

		return new(a, H, S, V);
	}
}
