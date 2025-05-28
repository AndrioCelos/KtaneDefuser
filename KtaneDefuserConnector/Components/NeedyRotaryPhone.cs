using System;
using System.Security.Principal;
using KtaneDefuserConnector.Properties;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace KtaneDefuserConnector.Components;
public class NeedyRotaryPhone : ComponentReader<NeedyRotaryPhone.ReadData> {
	public override string Name => "Needy Rotary Phone";

	private static readonly Image<Rgba32>[] ReferenceImages = [
		LoadSampleImage(Resources.NeedyRotaryPhone0),
		LoadSampleImage(Resources.NeedyRotaryPhone1),
		LoadSampleImage(Resources.NeedyRotaryPhone2),
		LoadSampleImage(Resources.NeedyRotaryPhone3),
		LoadSampleImage(Resources.NeedyRotaryPhone4),
		LoadSampleImage(Resources.NeedyRotaryPhone5),
		LoadSampleImage(Resources.NeedyRotaryPhone6),
		LoadSampleImage(Resources.NeedyRotaryPhone7),
		LoadSampleImage(Resources.NeedyRotaryPhone8),
		LoadSampleImage(Resources.NeedyRotaryPhone9)
	];

	private static Image<Rgba32> LoadSampleImage(byte[] bytes) {
		var image = Image.Load<Rgba32>(bytes);
		image.Mutate(p => p.Resize(new(32, 32), KnownResamplers.NearestNeighbor, false));
		return image;
	}

	protected internal override ReadData Process(Image<Rgba32> image, LightsState lightsState, ref Image<Rgba32>? debugImage) {
		var highlight = FindSelectionHighlight(image, lightsState, 88, 172, 172, 216);
		var time = ReadNeedyTimer(image, lightsState, debugImage);

		var digits = new int[3];
		var debugImage2 = debugImage;
		image.ProcessPixelRows(a => {
			Predicate<HsvColor> predicate = lightsState switch {
				LightsState.Buzz or LightsState.Off => hsv => hsv is { H: >= 15 and <= 40, S: >= 0.40f and < 0.80f, V: >= 0.10f },
				LightsState.Emergency => hsv => hsv is { H: >= 8 and <= 40, S: >= 0.60f, V: >= 0.30f },
				_ => hsv => hsv is { H: >= 15 and <= 60, S: >= 0.40f, V: >= 0.30f }
			};

			var index = 2;
			int leftLimit = 16 * a.Width / 256, rightLimit = 72 * a.Width / 256;
			int left = int.MaxValue, right = 0, bottom = 0, top = 0, misses = 0;
			for (var y = 232 * a.Height / 256; y > 0; y--) {
				var row = a.GetRowSpan(y);
				var left2 = 0;
				for (var x = leftLimit; x < rightLimit; x++) {
					if (!predicate(HsvColor.FromColor(row[x]))) continue;
					left2 = x;
					break;
				}

				if (left2 == 0) {
					if (bottom == 0) continue;
					misses++;
					if (misses < 4) continue;


					var rect = new Rectangle(left, top, right - left, bottom - top);
					debugImage2?.Mutate(p => p.Draw(Color.Lime, 1, rect));

					var bestSymbol = 0;
					if (rect.Height >= rect.Width * 4) {
						bestSymbol = 1;
					} else {
						float bestSimilarity = 0;
						var charImage = image.Clone(c => c.Crop(rect).Resize(32, 32, KnownResamplers.NearestNeighbor));
						for (var i = 0; i < ReferenceImages.Length; i++) {
							if (i == 1) continue;
							var similarity = ImageUtils.CheckSimilarity(charImage, ReferenceImages[i]);
							if (similarity <= bestSimilarity) continue;
							bestSimilarity = similarity;
							bestSymbol = i;
						}
					}

					digits[index--] = bestSymbol;

					if (index < 0) return;

					bottom = 0;
					left = int.MaxValue;
					right = 0;
					misses = 0;
					continue;
				}

				for (var x = rightLimit - 1; x >= leftLimit; x--) {
					if (!predicate(HsvColor.FromColor(row[x]))) continue;
					right = Math.Max(right, x + 1);
					break;
				}

				if (bottom == 0) bottom = y + 1;
				top = y;
				left = Math.Min(left, left2);
				misses = 0;
			}
		});

		return new(null, time, digits[2] + digits[1] * 10 + digits[0] * 100);  // For now, don't bother reading the selection highlight for this module.
	}

	public record ReadData(Point? Selection, int? Time, int Number) : ComponentReadData(Selection);
}
