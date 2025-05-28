using System;
using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace KtaneDefuserConnector.Components;
public class NeedyQuiz : ComponentReader<NeedyQuiz.ReadData> {
	public override string Name => "Needy Quiz";

	private static readonly TextRecogniser DisplayRecogniser = new(new(TextRecogniser.Fonts.NeedyQuizLcd, 16), 0, 128, new(512, 32),
		"1 strike?",
		"Abort?",
		"Are you a",
		"Do you have",
		"Does the parity of",
		"Does the",
		"Does this",
		"Have you previously",
		"SEGFAULT",
		"Was the last",
		"What was your",
		"What wasn't your",
		"about a previous",
		"answered No",
		"answered Yes",
		"answered a question",
		"answered not No",
		"answered not Yes",
		"answered question",
		"at least",
		"batteries",
		"characters?",
		"dirty cheater?",
		"duplicate",
		"incorrectly?",
		"less than",
		"match the parity of",
		"more strikes",
		"more than",
		"previous answer?",
		"question contain",
		"question or answer?",
		"serial contain",
		"serial number digits?",
		"six lines?",
		"six words?",
		"than batteries?",
		"three lines?",
		"three words?",
		"to a question?",
		"up to"
	);

	protected internal override ReadData Process(Image<Rgba32> image, LightsState lightsState, ref Image<Rgba32>? debugImage) {
		var highlight = FindSelectionHighlight(image, lightsState, 80, 184, 176, 240);
		Point? selection = highlight.Y != 0 ? new Point(highlight.X < 120 ? 0 : 1, 0) : null;

		var time = ReadNeedyTimer(image, lightsState, debugImage);

		string? result = null;
		image.ProcessPixelRows(a => {
			int leftLimit = 16 * a.Width / 256, rightLimit = 224 * a.Width / 256;
			int top = 0, left = int.MaxValue, right = 0;
			var builder = new StringBuilder();
			foreach (var y in a.Height.MapRange(96, 192)) {
				var row = a.GetRowSpan(y);
				var left2 = 0;
				for (var x = leftLimit; x < rightLimit; x++) {
					if (HsvColor.FromColor(row[x]) is not { H: >= 90 and <= 135, S: >= 0.75f, V: >= 0.5f }) continue;
					left2 = x;
					break;
				}

				if (left2 == 0) {
					if (top == 0) continue;

					var text = DisplayRecogniser.Recognise(image, new(left, top, right - left, y - top));
					if (builder.Length != 0) builder.Append(' ');
					builder.Append(text);
					if (builder[^1] == '?') break;

					top = 0;
					left = int.MaxValue;
					right = 0;
					continue;
				}

				for (var x = rightLimit - 1; x >= leftLimit; x--) {
					if (HsvColor.FromColor(row[x]) is not { H: >= 90 and <= 135, S: >= 0.75f, V: >= 0.5f }) continue;
					right = Math.Max(right, x + 1);
					break;
				}

				if (top == 0) top = y;
				left = Math.Min(left, left2);
			}

			result = builder.ToString();
		});

		return new(selection, time, result);
	}

	public record ReadData(Point? Selection, int? Time, string? Message) : ComponentReadData(Selection);
}
