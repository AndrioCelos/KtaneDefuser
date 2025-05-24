using static KtaneDefuserConnector.Components.WireSequence;

namespace KtaneDefuserScripts.Modules;
[AimlInterface("WireSequence")]
internal partial class WireSequence() : ModuleScript<KtaneDefuserConnector.Components.WireSequence>(1, 5) {
	public override string IndefiniteDescription => "a Wire Sequence";

	private bool _readyToRead;
	private readonly int[] _wireCounts = new int[3];
	private WireColour?[] _currentPageColours = new WireColour?[3];
	private int _highlight = -1;  // For this script, -1 => previous button, 0~2 => wire slots, 3 => next button, < -1 => unknown

	protected override bool IsSelectablePresent(int x, int y) => y is 0 or 4 || _currentPageColours[y - 1] is not null;

	protected internal override void Started(AimlAsyncContext context) => _readyToRead = true;

	protected internal override async void ModuleSelected(Interrupt interrupt) {
		try {
			if (!_readyToRead) return;
			_readyToRead = false;
			using var interrupt2 = await CurrentModuleInterruptAsync(interrupt.Context);
			// The highlight starts on the previous button, so move down first.
			if (_highlight == -1) {
				await interrupt2.SendInputsAsync(Button.Down);
				_highlight = -2;
			}

			await ContinuePageAsync(interrupt2);
		} catch (Exception ex) {
			LogException(ex);
		}
	}

	private async Task ContinuePageAsync(Interrupt interrupt) {
		var data = await ReadAsync(interrupt);
		if (data.Selection is { Y: >= 4 }) {
			// If we've reached the next button, or there are no wires on the first page, go ahead and switch to the next page.
			await MoveToNextPageAsync(interrupt);
		} else {
			ReadCurrentWire(interrupt, data);
		}
	}

	private void ReadCurrentWire(Interrupt interrupt, ReadData data) {
		if (data.HighlightedWire is null) return;
		var colour = data.WireColours[data.HighlightedWire.From]!;
		ref var count = ref _wireCounts[(int) colour];
		count++;
		interrupt.Context.Reply($"{Utils.ToOrdinal(count)} {colour} to {Nato.Speak([data.HighlightedWire.To])}");
		interrupt.Context.AddReplies("cut", "don't cut");
	}

	private async Task MoveToNextPageAsync(Interrupt interrupt) {
		while (true) {
			var result = await interrupt.SubmitAsync();
			switch (result) {
				case ModuleStatus.Solved:
				case ModuleStatus.Strike:
					return;
				default:
					await Delay(1);
					await ReadAsync(interrupt);
					var i = Array.FindIndex(_currentPageColours, c => c is not null);
					if (i < 0) {
						// It's possible for a page to not contain any wires. In this case, just move to the next page.
						continue;
					}

					// Highlight the first wire and read it.
					_highlight = i;
					await interrupt.SendInputsAsync(Enumerable.Repeat(Button.Up, _currentPageColours.Count(c => c is not null)));
					await ContinuePageAsync(interrupt);
					break;
			}

			break;
		}
	}

	private async Task<ReadData> ReadAsync(Interrupt interrupt) {
		var data = interrupt.Read(Reader);
		LogHighlightedButton(data.Selection?.Y ?? -1, string.Join(", ", data.WireColours));
		_currentPageColours = data.WireColours;
		switch (data.Selection?.Y) {
			case 0: _highlight = -1; break;
			case 4: _highlight = 3; break;
			default:
				while (data.HighlightedWire is null) {
					// Currently, we are only able to read the highlighted wire, and only when the selection highlight is within a strict range of intensities.
					// Keep looking at the module until that condition is met.
					LogReadingHighlightedWire();
					await Delay(0.05);
					data = interrupt.Read(Reader);
				}
				LogHighlightedWire(data.HighlightedWire);
				_highlight = data.HighlightedWire.From;
				break;
		}
		return data;
	}

	private async Task ActionAsync(AimlAsyncContext context, bool cut) {
		using var interrupt = await ModuleInterruptAsync(context);
		if (cut) {
			await interrupt.SendInputsAsync(Button.A, Button.Down);
		} else {
			await interrupt.SendInputsAsync(Button.Down);
		}
		do { _highlight++; } while (_highlight < 3 && _currentPageColours[_highlight] is null);
		await ContinuePageAsync(interrupt);
	}

	[AimlCategory("cut ^")]
	internal static Task Cut(AimlAsyncContext context) => GameState.Current.CurrentScript<WireSequence>().ActionAsync(context, true);

	[AimlCategory("don't cut ^")]
	[AimlCategory("do not cut ^")]
	[AimlCategory("skip")]
	[AimlCategory("next")]
	internal static Task DoNotCut(AimlAsyncContext context) => GameState.Current.CurrentScript<WireSequence>().ActionAsync(context, false);
	
	#region Log templates

	[LoggerMessage(LogLevel.Information, "Highlighted button: {HighlightedButton}; wires: {WireColours}")]
	private partial void LogHighlightedButton(int highlightedButton, string wireColours);

	[LoggerMessage(LogLevel.Information, "Reading highlighted wire...")]
	private partial void LogReadingHighlightedWire();

	[LoggerMessage(LogLevel.Information, "Highlighted wire: {HighlightedWire}")]
	private partial void LogHighlightedWire(HighlightedWireData highlightedWire);

	#endregion
}
