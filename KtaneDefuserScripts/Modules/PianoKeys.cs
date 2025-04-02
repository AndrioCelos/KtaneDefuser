using System.Text.RegularExpressions;
using KtaneDefuserConnectorApi;
using static KtaneDefuserConnector.Components.PianoKeys;

namespace KtaneDefuserScripts.Modules;
[AimlInterface("PianoKeys")]
internal partial class PianoKeys : ModuleScript<KtaneDefuserConnector.Components.PianoKeys> {
	private enum Note {
		C,
		Cs,
		D,
		Ds,
		E,
		F,
		Fs,
		G,
		Gs,
		A,
		As,
		B
	}

	public override string IndefiniteDescription => "Piano Keys";

	[GeneratedRegex(@"\G\s*(?:($)|([A-G])\w*(?:\s+(?:(s\w*|#)|(flat)))?(?:\s+(\d+)\s*times)?)", RegexOptions.IgnoreCase)]
	private static partial Regex PlayParseRegex();

	private Note highlight;

	public static Dictionary<Symbol, string> SymbolDescriptions { get; } = new() {
		{ Symbol.Natural      , "natural" },
		{ Symbol.Flat         , "flat" },
		{ Symbol.Sharp        , "sharp" },
		{ Symbol.Mordent      , "mordent" },
		{ Symbol.Turn         , "turn" },
		{ Symbol.CommonTime   , "common" },
		{ Symbol.CutCommonTime, "cut common" },
		{ Symbol.Fermata      , "fermata" },
		{ Symbol.CClef        , "alto clef" },
	};

	protected internal override void Started(AimlAsyncContext context) => context.AddReply("ready");

	[AimlCategory("read")]
	internal static async Task Read(AimlAsyncContext context) {
		using var interrupt = await CurrentModuleInterruptAsync(context);
		var data = interrupt.Read(Reader);
		interrupt.Context.Reply($"{string.Join(", ", from s in data.Symbols select SymbolDescriptions[s])}.");
	}

	[AimlCategory("*")]
	internal static async Task Play(AimlAsyncContext context, string input) {
		var matches = PlayParseRegex().Matches(input);
		if (matches.Count == 0 || !matches[^1].Groups[1].Success) {
			context.Reply("That is not a valid melody.");
			return;
		}

		var script = GameState.Current.CurrentScript<PianoKeys>();
		var buttons = new List<Button>();
		var newHighlight = script.highlight;
		foreach (var m in matches.Cast<Match>()) {
			if (m.Groups[1].Success) break;
			var note = m.Groups[2].Value[0] switch {
				'A' or 'a' => Note.A,
				'B' or 'b' => Note.B,
				'C' or 'c' => Note.C,
				'D' or 'd' => Note.D,
				'E' or 'e' => Note.E,
				'F' or 'f' => Note.F,
				_ => Note.G
			};
			if (m.Groups[3].Success) {
				if (note is Note.B or Note.E) {
					context.Reply("That is not a valid note.");
					return;
				}
				note++;
			} else if (m.Groups[4].Success) {
				if (note is Note.C or Note.F) {
					context.Reply("That is not a valid note.");
					return;
				}
				note--;
			}

			var count = m.Groups[5].Success ? int.Parse(m.Groups[5].Value) : 1;
			if (count is <= 0 or >= 10) {
				context.Reply("That is not a valid number.");
				return;
			}

			for (; newHighlight > note; newHighlight--) {
				buttons.Add(Button.Left);
			}
			for (; newHighlight < note; newHighlight++) {
				buttons.Add(Button.Right);
			}
			for (; count > 0; count--) {
				buttons.Add(Button.A);
			}
		}

		using var interrupt = await CurrentModuleInterruptAsync(context);
		script.highlight = newHighlight;
		await interrupt.SubmitAsync(buttons);
	}
}
