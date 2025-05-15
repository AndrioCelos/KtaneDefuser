using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Tesseract;

namespace KtaneDefuserConnector.Components;
public class CrazyTalk : ComponentReader<CrazyTalk.ReadData> {
	public override string Name => "Crazy Talk";

	private static readonly TextRecogniser DisplayRecogniser = new(new(TextRecogniser.Fonts.Arial, 36), 0, 128, new(512, 64),
		[.. from i in Enumerable.Range('A', 26) where i != 'Q' select ((char) i).ToString(), .. from i in Enumerable.Range(1, 4) select i.ToString(), ":", "?", "←", "→"]);

	protected internal override ReadData Process(Image<Rgba32> image, LightsState lightsState, ref Image<Rgba32>? debugImage) {
		using var ms = new MemoryStream();
		var rect = image.Map(32, 40, 158, 204);
		using (var image2 = image.Clone()) {
			image2.Mutate(p => p.Crop(rect));

			// Preprocess the image to keep the green text.
			image2.ProcessPixelRows(a => {
				for (var y = 0; y < a.Height; y++) {
					var row = a.GetRowSpan(y);
					for (var x = 0; x < a.Width; x++) {
						ref var p = ref row[x];
						if (p.G < p.R * 2 || p.G < p.B * 2)
							p = new(0, 0, 0, 255);
						else
							p.A = 255;
					}
				}
			});

			image2.SaveAsBmp(ms);
		}
		using var img = Pix.LoadFromMemory(ms.ToArray());
		using var page = Tesseract.TesseractEngine.Process(img);
		var text = page.GetText();
		if (!string.IsNullOrWhiteSpace(text) && !text.Any(c => c is '–' or '—' or '<' or '>' or '(' or ')' or (>= 'a' and <= 'z'))) {
			text = text.Trim().Replace('\n', ' ').Replace('|', 'I');
		} else {
			image.ProcessPixelRows(a => {
				var builder = new StringBuilder();
				var line = new StringBuilder();
				var gaps = new List<int>();

				var rowTop = 0;
				for (var y = a.Height / 8; y < a.Height * 7 / 8; y++) {
					var row = a.GetRowSpan(y);
					var found = false;
					foreach (var x in a.Width.MapRange(24, 176)) {
						if (!IsText(row[x])) continue;
						found = true;
						break;
					}

					if (found) {
						if (rowTop == 0) rowTop = y;
					} else if (rowTop != 0) {
						// Find characters within the row.
						var cellLeft = 0;
						var lastRight = 0;
						for (var x = a.Width / 11; x < a.Width * 11 / 16; x++) {
							found = false;
							for (var y2 = rowTop; y2 < y; y2++) {
								if (!IsText(image[x, y2])) continue;
								found = true;
								break;
							}

							if (found) {
								if (cellLeft == 0) cellLeft = x;
							} else if (cellLeft != 0) {
								if (lastRight != 0) gaps.Add(cellLeft - lastRight);
								lastRight = x;

								var rect = ImageUtils.FindEdges(image, new(cellLeft, rowTop, x - cellLeft, y - rowTop), IsText);
								if (rect.Width <= 4 && rect.Height < (y - rowTop) / 2) {
									// Punctuation mark
									line.Append(rect.Height <= rect.Width + 1 ? '.' : rect.Y < rowTop + 2 ? '\'' : ',');
								} else {
									line.Append(DisplayRecogniser.Recognise(image, rect));
								}

								cellLeft = 0;
							}
						}

						if (gaps.Count == 0) {
							if (builder.Length != 0) builder.Append(' ');
							builder.Append(line);
						} else {
							var minGap = gaps.Min();
							var threshold = minGap + (y - rowTop) / 4;
							for (var i = 0; i < line.Length; i++) {
								if (builder.Length != 0 && (i == 0 || gaps[i - 1] > threshold)) builder.Append(' ');
								builder.Append(line[i]);
							}
						}

						rowTop = 0;
						line.Clear();
						gaps.Clear();
					}
				}

				text = builder.ToString().Replace("←O", "TWO");
			});
		}

		return new(text, HsvColor.FromColor(image[image.Width * 216 / 256, image.Height * 160 / 256]).H is > 0 and < 90);

		static bool IsText(Rgba32 p) => p is { R: < 64, G: >= 112, B: 0 };
	}

	public record struct ReadData(string Display, bool SwitchIsDown);
}
