namespace KtaneDefuserScripts.Modules;
[AimlInterface("WordScramble")]
internal class WordScramble : ModuleScript<KtaneDefuserConnector.Components.WordScramble> {
	public override string IndefiniteDescription => "Word Scramble";

	private readonly Processor _processor = new(i => i.Read(Reader));

	protected internal override void Started(AimlAsyncContext context) => context.AddReply("ready");

	[AimlCategory("read")]
	internal static async Task Read(AimlAsyncContext context) {
		var script = GameState.Current.CurrentScript<WordScramble>();
		using var interrupt = await script.ModuleInterruptAsync(context);
		await script._processor.ReadAsync(interrupt);
	}

	[AimlCategory("<set>NATO</set> <set>NATO</set> <set>NATO</set> <set>NATO</set> <set>NATO</set> <set>NATO</set>")]
	internal static async Task SubmitLetters(AimlAsyncContext context, string nato1, string nato2, string nato3, string nato4, string nato5, string nato6) {
		using var interrupt = await CurrentModuleInterruptAsync(context);
		await GameState.Current.CurrentScript<WordScramble>()._processor.SubmitLetters(interrupt, NATO.DecodeChar(nato1), NATO.DecodeChar(nato2), NATO.DecodeChar(nato3), NATO.DecodeChar(nato4), NATO.DecodeChar(nato5), NATO.DecodeChar(nato6));
	}

	internal class Processor(Func<Interrupt, KtaneDefuserConnector.Components.WordScramble.ReadData> readFunc) {
		private int _highlightX;
		private int _highlightY;
		private char[]? _letters;

		private static readonly HashSet<string> Words = new(StringComparer.InvariantCultureIgnoreCase) {
			"ARCHER", "ATTACK", "BANANA", "BLASTS", "BURSTS", "BUTTON",
			"CANNON", "CASING", "CHARGE", "DAMAGE", "DEFUSE", "DEVICE",
			"DISARM", "FLAMES", "KABOOM", "KEVLAR", "KEYPAD", "LETTER",
			"MODULE", "MORTAR", "NAPALM", "OTTAWA", "PERSON", "ROBOTS",
			"ROCKET", "SAPPER", "SEMTEX", "WEAPON", "WIDGET", "WIRING",
			"STREAM", "MASTER", "TAMERS", "LOOPED", "POODLE", "POOLED",
			"CELLAR", "CALLER", "RECALL", "SEATED", "SEDATE", "TEASED",
			"RESCUE", "SECURE", "RECUSE", "RASHES", "SHEARS", "SHARES",
			"BARELY", "BARLEY", "BLEARY", "DUSTER", "RUSTED", "RUDEST",
		};

		internal async Task ReadAsync(Interrupt interrupt) {
			var data = readFunc(interrupt);
			if (data.Selection is { } selection) (_highlightX, _highlightY) = selection;
			_letters = data.Letters;

			// Find a word.
			var word = Utils.EnumeratePermutations(_letters).Skip(1).Select(a => new string(a)).FirstOrDefault(s => Words.Contains(s));

			if (word is null) {
				// No words found; ask the expert.
				interrupt.Context.Reply(NATO.Speak(data.Letters));
				return;
			}

			await SubmitLetters(interrupt, word.ToCharArray());
		}
		
		internal async Task SubmitLetters(Interrupt interrupt, params char[] letters) {
			if (letters.Length != 6) {
				interrupt.Context.Reply("There should be exactly 6 letters.");
				return;
			}

			if (_letters is null) {
				var data = readFunc(interrupt);
				if (data.Selection is { } selection) (_highlightX, _highlightY) = selection;
				_letters = data.Letters;
			}

			var indices = new int[letters.Length];
			foreach (var (i, c) in letters.Index()) {
				var j = Array.IndexOf(_letters, char.ToUpper(c));
				if (j < 0) {
					interrupt.Context.Reply($"The letter {c} is not present.");
					return;
				}
				indices[i] = j;
			}

			foreach (var i in indices) {
				await PressButtonAsync(interrupt, i % 3, i / 3, false);
			}
			await PressButtonAsync(interrupt, 3, 1, true);
		}
		private async Task PressButtonAsync(Interrupt interrupt, int x, int y, bool submit) {
			var buttons = new List<Button>();
			for (; _highlightX < x; _highlightX++) buttons.Add(Button.Right);
			for (; _highlightX > x; _highlightX--) buttons.Add(Button.Left);
			for (; _highlightY < y; _highlightY++) buttons.Add(Button.Down);
			for (; _highlightY > y; _highlightY--) buttons.Add(Button.Up);
			buttons.Add(Button.A);
			if (submit)
				await interrupt.SubmitAsync(buttons);
			else
				await interrupt.SendInputsAsync(buttons);
		}

	}
}
