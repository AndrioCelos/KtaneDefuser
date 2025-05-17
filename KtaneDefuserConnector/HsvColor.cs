using System;
using JetBrains.Annotations;
using SixLabors.ImageSharp.PixelFormats;

// ReSharper disable CompareOfFloatsByEqualityOperator

namespace KtaneDefuserConnector;
/// <summary>Represents a colour in HSVA coordinates.</summary>
public struct HsvColor(float h, float s, float v, byte a) {
	/// <summary>The hue in degrees, between 0 and 360.</summary>
	[PublicAPI] public float H = h;
	/// <summary>The saturation, between 0 and 1.</summary>
	[PublicAPI] public float S = s;
	/// <summary>The brightness, between 0 and 1.</summary>
	[PublicAPI] public float V = v;
	/// <summary>The opacity, between 0 and 255.</summary>
	[PublicAPI] public byte A = a;

	/// <summary>Converts a <see cref="Rgba32"/> pixel to a <see cref="HsvColor"/>.</summary>
	[PublicAPI]
	public static HsvColor FromColor(Rgba32 pixel) => FromArgb(pixel.A, pixel.R, pixel.G, pixel.B);
	/// <summary>Converts a colour in ARGB format to a <see cref="HsvColor"/>.</summary>
	[PublicAPI]
	public static HsvColor FromArgb(byte a, byte r, byte g, byte b) {
		var rf = r / 255f;
		var gf = g / 255f;
		var bf = b / 255f;

		var min = Math.Min(Math.Min(rf, gf), bf);
		var max = Math.Max(Math.Max(rf, gf), bf);
		var delta = max - min;

		var h = delta == 0 ? 0
			: rf == max ? 60 * (gf - bf) / delta
			: gf == max ? 60 * ((bf - rf) / delta + 2)
			: 60 * ((rf - gf) / delta + 4);
		if (h < 0) h += 360;

		return new(h, max == 0 ? 0 : delta / max, max, a);
	}

	public override readonly string ToString() => $"{nameof(HsvColor)} [ H={H:N1}, S={S:N2}, V={V:N2} ]";
}
