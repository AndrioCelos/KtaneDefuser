using System.Text.RegularExpressions;
using static KtaneDefuserConnector.Components.PianoKeys;

namespace KtaneDefuserScripts.Modules;
[AimlInterface("PianoKeys")]
internal partial class PianoKeys() : ModuleScript<KtaneDefuserConnector.Components.PianoKeys>(11, 1) {
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

	private static Dictionary<Symbol, string> SymbolDescriptions { get; } = new() {
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

	[GeneratedRegex(@"\G\s*(?:($)|([A-G])\w*(?:\s+(?:(s\w*|#)|(flat)))?(?:\s+(\d+)\s*times)?)", RegexOptions.IgnoreCase)]
	private static partial Regex PlayParseRegex();

	public override string IndefiniteDescription => "Piano Keys";

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
		var indices = new List<Note>();

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

			indices.Add(note);
		}

		using var interrupt = await CurrentModuleInterruptAsync(context);
		foreach (var n in indices) {
			await script.InteractWaitAsync(interrupt, (int) n, 0);
			if (interrupt.HasStrikeOccurred) return;
		}

		await interrupt.CheckStatusAsync();
	}
}
