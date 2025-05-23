namespace KtaneDefuserScripts.Modules;
[AimlInterface("WordScramble")]
internal class WordScramble: ModuleScript<KtaneDefuserConnector.Components.WordScramble> {
	public override string IndefiniteDescription => "Word Scramble";

	private readonly Processor _processor;

	public WordScramble() : base(4, 2) => _processor = new(this, i => i.Read(Reader));

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
		await GameState.Current.CurrentScript<WordScramble>()._processor.SubmitLetters(interrupt, Nato.DecodeChar(nato1), Nato.DecodeChar(nato2), Nato.DecodeChar(nato3), Nato.DecodeChar(nato4), Nato.DecodeChar(nato5), Nato.DecodeChar(nato6));
	}

	internal class Processor(ModuleScript module, Func<Interrupt, KtaneDefuserConnector.Components.WordScramble.ReadData> readFunc) {
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
			if (data.Selection is { } selection) module.Selection = selection;
			_letters = data.Letters;

			// Find a word.
			var word = Utils.EnumeratePermutations(_letters).Skip(1).Select(a => new string(a)).FirstOrDefault(s => Words.Contains(s));

			if (word is null) {
				// No words found; ask the expert.
				interrupt.Context.Reply(Nato.Speak(data.Letters));
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
				if (data.Selection is { } selection) module.Selection = selection;
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
			module.Select(interrupt, x, y);
			if (submit)
				await interrupt.SubmitAsync();
			else
				await interrupt.SendInputsAsync(Button.A);
		}

	}
}
