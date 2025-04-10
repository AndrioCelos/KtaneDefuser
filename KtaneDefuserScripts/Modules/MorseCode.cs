using System.Collections;

namespace KtaneDefuserScripts.Modules;

[AimlInterface("MorseCode")]
internal partial class MorseCode : ModuleScript<KtaneDefuserConnector.Components.MorseCode> {
	public override string IndefiniteDescription => "Morse Code";
	private static Interrupt? interrupt;
	private static CancellationTokenSource? cancellationTokenSource;
	private static readonly int DASH_THRESHOLD = 4;
	private static readonly int WORD_SPACE_THRESHOLD = 10;

	private static readonly Dictionary<MorseLetter, char> decodeMorse = new() {
		{ new(MorseElement.Dot, MorseElement.Dash), 'A' },
		{ new(MorseElement.Dash, MorseElement.Dot, MorseElement.Dot, MorseElement.Dot), 'B' },
		{ new(MorseElement.Dash, MorseElement.Dot, MorseElement.Dash, MorseElement.Dot), 'C' },
		{ new(MorseElement.Dash, MorseElement.Dot, MorseElement.Dot), 'D' },
		{ new(MorseElement.Dot), 'E' },
		{ new(MorseElement.Dot, MorseElement.Dot, MorseElement.Dash, MorseElement.Dot), 'F' },
		{ new(MorseElement.Dash, MorseElement.Dash, MorseElement.Dot), 'G' },
		{ new(MorseElement.Dot, MorseElement.Dot, MorseElement.Dot, MorseElement.Dot), 'H' },
		{ new(MorseElement.Dot, MorseElement.Dot), 'I' },
		{ new(MorseElement.Dot, MorseElement.Dash, MorseElement.Dash, MorseElement.Dash), 'J' },
		{ new(MorseElement.Dash, MorseElement.Dot, MorseElement.Dash), 'K' },
		{ new(MorseElement.Dot, MorseElement.Dash, MorseElement.Dot, MorseElement.Dot), 'L' },
		{ new(MorseElement.Dash, MorseElement.Dash), 'M' },
		{ new(MorseElement.Dash, MorseElement.Dot), 'N' },
		{ new(MorseElement.Dash, MorseElement.Dash, MorseElement.Dash), 'O' },
		{ new(MorseElement.Dot, MorseElement.Dash, MorseElement.Dash, MorseElement.Dot), 'P' },
		{ new(MorseElement.Dash, MorseElement.Dash, MorseElement.Dot, MorseElement.Dash), 'Q' },
		{ new(MorseElement.Dot, MorseElement.Dash, MorseElement.Dot), 'R' },
		{ new(MorseElement.Dot, MorseElement.Dot, MorseElement.Dot), 'S' },
		{ new(MorseElement.Dash), 'T' },
		{ new(MorseElement.Dot, MorseElement.Dot, MorseElement.Dash), 'U' },
		{ new(MorseElement.Dot, MorseElement.Dot, MorseElement.Dot, MorseElement.Dash), 'V' },
		{ new(MorseElement.Dot, MorseElement.Dash, MorseElement.Dash), 'W' },
		{ new(MorseElement.Dash, MorseElement.Dot, MorseElement.Dot, MorseElement.Dash), 'X' },
		{ new(MorseElement.Dash, MorseElement.Dot, MorseElement.Dash, MorseElement.Dash), 'Y' },
		{ new(MorseElement.Dash, MorseElement.Dash, MorseElement.Dot, MorseElement.Dot), 'Z' },
		{ new(MorseElement.Dash, MorseElement.Dash, MorseElement.Dash, MorseElement.Dash, MorseElement.Dash), '0' },
		{ new(MorseElement.Dot, MorseElement.Dash, MorseElement.Dash, MorseElement.Dash, MorseElement.Dash), '1' },
		{ new(MorseElement.Dot, MorseElement.Dot, MorseElement.Dash, MorseElement.Dash, MorseElement.Dash), '2' },
		{ new(MorseElement.Dot, MorseElement.Dot, MorseElement.Dot, MorseElement.Dash, MorseElement.Dash), '3' },
		{ new(MorseElement.Dot, MorseElement.Dot, MorseElement.Dot, MorseElement.Dot, MorseElement.Dash), '4' },
		{ new(MorseElement.Dot, MorseElement.Dot, MorseElement.Dot, MorseElement.Dot, MorseElement.Dot), '5' },
		{ new(MorseElement.Dash, MorseElement.Dot, MorseElement.Dot, MorseElement.Dot, MorseElement.Dot), '6' },
		{ new(MorseElement.Dash, MorseElement.Dash, MorseElement.Dot, MorseElement.Dot, MorseElement.Dot), '7' },
		{ new(MorseElement.Dash, MorseElement.Dash, MorseElement.Dash, MorseElement.Dot, MorseElement.Dot), '8' },
		{ new(MorseElement.Dash, MorseElement.Dash, MorseElement.Dash, MorseElement.Dash, MorseElement.Dot), '9' }
	};

	private static readonly Dictionary<string, int> frequencies = new() {
		{ "505",  0 },
		{ "515",  1 },
		{ "522",  2 },
		{ "532",  3 },
		{ "535",  4 },
		{ "542",  5 },
		{ "545",  6 },
		{ "552",  7 },
		{ "555",  8 },
		{ "565",  9 },
		{ "572", 10 },
		{ "575", 11 },
		{ "582", 12 },
		{ "592", 13 },
		{ "595", 14 },
		{ "600", 15 }
	};

	private int highlight;  // For this script, 0 => down button, 1 => right button, 2 => submit button
	private int selectedFrequency;

	protected internal override void Started(AimlAsyncContext context) => context.AddReply("ready");

	[AimlCategory("read")]
	internal static Task Read(AimlAsyncContext context) {
		context.Reply($"Stand by.");
		cancellationTokenSource = new();
		return GameState.Current.CurrentScript<MorseCode>().ReadAsync(context, cancellationTokenSource.Token);
	}

	private async Task ReadAsync(AimlAsyncContext context, CancellationToken cancellationToken) {
		var interrupt = await ModuleInterruptAsync(context);
		try {
			MorseCode.interrupt = interrupt;

			// Wait for a space between letters.
			LogWaitingForLetterSpace();
			while (true) {
				await WaitForStateAsync(interrupt, false, cancellationToken);
				var continuedLetter = await WaitForStateAsync(interrupt, true, DASH_THRESHOLD, cancellationToken);
				if (!continuedLetter) break;
			}
			LogLetterSpaceFound();

			for (var lettersRead = 0; lettersRead < 5; lettersRead++) {
				// We've just seen a space between letters. Find out whether it is a space between words.
				LogWaitingForNextLetter();
				var continuedWord = await WaitForStateAsync(interrupt, true, WORD_SPACE_THRESHOLD - DASH_THRESHOLD, cancellationToken);
				if (cancellationToken.IsCancellationRequested) return;
				if (!continuedWord && !interrupt.IsDisposed) {
					LogWordStart();
					interrupt.Context.Reply("Word start.");
					interrupt.Context.Reply("<reply>505</reply><reply>515</reply><reply>522</reply><reply>532</reply><reply>535</reply><reply>542</reply><reply>545</reply><reply>552</reply><reply>555</reply><reply>565</reply><reply>572</reply><reply>575</reply><reply>582</reply><reply>592</reply><reply>595</reply><reply>600</reply>");
					await WaitForStateAsync(interrupt, true, cancellationToken);
				}
				LogLetterStart();

				var currentLetter = new MorseLetter();
				while (true) {
					var isDot = await WaitForStateAsync(interrupt, false, DASH_THRESHOLD, cancellationToken);
					LogSymbol(isDot ? "Dot" : "Dash");
					currentLetter.Add(isDot ? MorseElement.Dot : MorseElement.Dash);
					if (!isDot) await WaitForStateAsync(interrupt, false, cancellationToken);
					var continuedLetter = await WaitForStateAsync(interrupt, true, DASH_THRESHOLD, cancellationToken);
					if (!continuedLetter) break;
				}
				if (cancellationToken.IsCancellationRequested || interrupt.IsDisposed) return;
				LogLetterSpaceFound();

				if (decodeMorse.TryGetValue(currentLetter, out var c)) {
					LogLetter(c);
					interrupt.Context.Reply(NATO.Speak(c.ToString()));
				} else
					interrupt.Context.Reply(string.Join(' ', currentLetter));
				interrupt.Context.Reply("<reply>505</reply><reply>515</reply><reply>522</reply><reply>532</reply><reply>535</reply><reply>542</reply><reply>545</reply><reply>552</reply><reply>555</reply><reply>565</reply><reply>572</reply><reply>575</reply><reply>582</reply><reply>592</reply><reply>595</reply><reply>600</reply>");
			}
		} finally {
			// Only dispose the interrupt if we're not ending due to a cancellation signal, as that indicates we're keeping the interrupt to submit an answer.
			if (!cancellationToken.IsCancellationRequested) {
				interrupt.Dispose();
				MorseCode.interrupt = null;
			}
		}
	}

	private Task<bool> WaitForStateAsync(Interrupt interrupt, bool state, CancellationToken token) => WaitForStateAsync(interrupt, state, int.MaxValue, token);
	private async Task<bool> WaitForStateAsync(Interrupt interrupt, bool state, int limit, CancellationToken token) {
		if (token.IsCancellationRequested) return false;
		var count = 0;
		do {
			await Delay(0.075);
			if (token.IsCancellationRequested) return false;
			if (interrupt.IsDisposed || interrupt.Read(Reader).IsLightOn == state) {
				LogAwaitedStateReached(state, count);
				return true;
			}
			count++;
		} while (count < limit);
		LogAwaitedStateTimedOut(state, count);
		return false;
	}

	[AimlCategory("<set>number</set>")]
	[AimlCategory("submit <set>number</set>")]
	[AimlCategory("transmit at <set>number</set>")]
	[AimlCategory("respond at <set>number</set>")]
	internal static async Task Submit1(AimlAsyncContext context, string s) {
		if (!frequencies.TryGetValue(s, out var frequency)) {
			context.Reply("That is not a valid frequency.");
			return;
		}
		var script = GameState.Current.CurrentScript<MorseCode>();
		await script.Submit(context, frequency);
	}

	[AimlCategory("<set>number</set> <set>number</set> <set>number</set>")]
	[AimlCategory("submit <set>number</set> <set>number</set> <set>number</set>")]
	[AimlCategory("transmit at <set>number</set> <set>number</set> <set>number</set>")]
	[AimlCategory("respond at <set>number</set> <set>number</set> <set>number</set>")]
	internal static async Task Submit2(AimlAsyncContext context, string d1, string d2, string d3) {
		if (!frequencies.TryGetValue($"{d1}{d2}{d3}", out var frequency)) {
			context.Reply("That is not a valid frequency.");
			return;
		}
		var script = GameState.Current.CurrentScript<MorseCode>();
		await script.Submit(context, frequency);
	}

	internal async Task Submit(AimlAsyncContext context, int frequency) {
		cancellationTokenSource?.Cancel();
		cancellationTokenSource?.Dispose();
		cancellationTokenSource = null;
		using var interrupt = MorseCode.interrupt ?? await ModuleInterruptAsync(context);
		interrupt.Context = context;
		var buttons = new List<Button>();
		if (frequency < selectedFrequency) {
			switch (highlight) {
				case 1: buttons.Add(Button.Left); break;
				case 2: buttons.Add(Button.Up); break;
			}
			highlight = 0;
			do {
				buttons.Add(Button.A);
				selectedFrequency--;
			} while (frequency < selectedFrequency);
		}
		if (frequency > selectedFrequency) {
			if (highlight != 1) {
				buttons.Add(Button.Right);
				highlight = 1;
			}
			do {
				buttons.Add(Button.A);
				selectedFrequency++;
			} while (frequency > selectedFrequency);
		}
		if (highlight != 2) {
			buttons.Add(Button.Down);
			highlight = 2;
		}
		buttons.Add(Button.A);
		await interrupt.SubmitAsync(buttons);
	}
	
	#region Log templates
	
	[LoggerMessage(LogLevel.Information, "Waiting for letter space")]
	private partial void LogWaitingForLetterSpace();
	
	[LoggerMessage(LogLevel.Information, "Letter space found")]
	private partial void LogLetterSpaceFound();
	
	[LoggerMessage(LogLevel.Information, "Waiting for next letter")]
	private partial void LogWaitingForNextLetter();
		
	[LoggerMessage(LogLevel.Information, "Word start")]
	private partial void LogWordStart();
		
	[LoggerMessage(LogLevel.Information, "Next letter started")]
	private partial void LogLetterStart();
		
	[LoggerMessage(LogLevel.Information, "{Symbol}")]
	private partial void LogSymbol(string symbol);
		
	[LoggerMessage(LogLevel.Information, "Decoded letter: {Letter}")]
	private partial void LogLetter(char letter);
		
	[LoggerMessage(LogLevel.Information, "Awaited state {State} reached after {Count}")]
	private partial void LogAwaitedStateReached(bool state, int count);
		
	[LoggerMessage(LogLevel.Information, "Awaited state {State} timed out after {Count}")]
	private partial void LogAwaitedStateTimedOut(bool state, int count);

	#endregion

	internal struct MorseLetter : IEquatable<MorseLetter>, IEnumerable<MorseElement> {
		public int Bits;
		public int Length;

		public MorseLetter(MorseElement e1) {
			Length = 1;
			Bits = (int) e1;
		}
		public MorseLetter(MorseElement e1, MorseElement e2) {
			Length = 2;
			Bits = (int) e1 | (int) e2 << 1;
		}
		public MorseLetter(MorseElement e1, MorseElement e2, MorseElement e3) {
			Length = 3;
			Bits = (int) e1 | (int) e2 << 1 | (int) e3 << 2;
		}
		public MorseLetter(MorseElement e1, MorseElement e2, MorseElement e3, MorseElement e4) {
			Length = 4;
			Bits = (int) e1 | (int) e2 << 1 | (int) e3 << 2 | (int) e4 << 3;
		}
		public MorseLetter(MorseElement e1, MorseElement e2, MorseElement e3, MorseElement e4, MorseElement e5) {
			Length = 5;
			Bits = (int) e1 | (int) e2 << 1 | (int) e3 << 2 | (int) e4 << 3 | (int) e5 << 4;
		}

		public void Add(MorseElement element) {
			if (element != MorseElement.Dot) Bits |= 1 << Length;
			Length++;
		}

		public readonly bool Equals(MorseLetter other) => Bits == other.Bits && Length == other.Length;
		public override readonly bool Equals(object? obj) => obj is MorseLetter letter && Equals(letter);
		public static bool operator ==(MorseLetter v1, MorseLetter v2) => v1.Equals(v2);
		public static bool operator !=(MorseLetter v1, MorseLetter v2) => !v1.Equals(v2);

		public override readonly int GetHashCode() => HashCode.Combine(Bits, Length);
		public readonly IEnumerator<MorseElement> GetEnumerator() {
			for (var i = 0; i < Length; i++)
				yield return (Bits & 1 << i) == 0 ? MorseElement.Dot : MorseElement.Dash;
		}
		readonly IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}

	internal enum MorseElement {
		Dot,
		Dash
	}
}
