namespace KtaneDefuserScripts.Modules;
[AimlInterface("ColourFlash")]
internal partial class ColourFlash : ModuleScript<KtaneDefuserConnector.Components.ColourFlash> {
	public override string IndefiniteDescription => "Colour Flash";

	private static CancellationTokenSource? cancellationTokenSource;
	private static Interrupt? currentInterrupt;

	private List<KtaneDefuserConnector.Components.ColourFlash.ReadData>? sequence;
	private int highlight;

	protected internal override void Started(AimlAsyncContext context) => context.AddReply("ready");

	[AimlCategory("read")]
	internal static Task Read(AimlAsyncContext context) {
		context.Reply($"Stand by.");
		cancellationTokenSource = new();
		return GameState.Current.CurrentScript<ColourFlash>().ReadAsync(context, cancellationTokenSource.Token);
	}

	private async Task ReadAsync(AimlAsyncContext context, CancellationToken cancellationToken) {
		var interrupt = await ModuleInterruptAsync(context);
		try {
			currentInterrupt = interrupt;
			LogWaitingForStart();

			// Wait for the start of the sequence.
			while (true) {
				await Delay(0.25);
				var state = interrupt.Read(Reader);
				if (state.Word == null) break;
			}

			// Read the sequence.
			LogReadingSequence();
			var sequence = new List<KtaneDefuserConnector.Components.ColourFlash.ReadData>(8);
			while (true) {
				await Delay(0.25);
				var state = interrupt.Read(Reader);
				if (sequence.Count == 0 ? state.Word != null : state != sequence[^1]) {
					sequence.Add(state);
					if (sequence.Count >= 8) break;
				}
			}

			this.sequence = sequence;
			interrupt.Context.Reply($"Words are {string.Join(", ", from s in sequence select s.Word?.ToLower())}. Colours are {string.Join(", ", from s in sequence select s.Colour.ToString().ToLower())}.");
		} finally {
			// Only dispose the interrupt if we're not ending due to a cancellation signal, as that indicates we're keeping the interrupt to submit an answer.
			if (!cancellationToken.IsCancellationRequested) {
				interrupt.Dispose();
				currentInterrupt = null;
			}
		}
	}

	internal async Task Submit(AimlAsyncContext context, bool pressYes, int? index) {
		cancellationTokenSource?.Cancel();
		cancellationTokenSource?.Dispose();
		cancellationTokenSource = null;
		using var interrupt = currentInterrupt ?? await ModuleInterruptAsync(context);
		interrupt.Context = context;

		if (pressYes && highlight == 1) {
			interrupt.SendInputs(Button.Left);
			highlight = 0;
		} else if (!pressYes && highlight == 0) {
			interrupt.SendInputs(Button.Right);
			highlight = 1;
		}

		if (index != null) {
			// Figure out where we are in the sequence currently.
			int currentIndex; KtaneDefuserConnector.Components.ColourFlash.ReadData state;
			while (true) {
				state = interrupt.Read(Reader);
				if (state.Word == null) {
					currentIndex = -1;
					break;
				}
				if (sequence != null) {
					currentIndex = sequence.IndexOf(state);
					if (currentIndex < 0) {
						// Shouldn't happen, but treat this as if we no longer know the sequence.
						sequence = null;
					} else {
						if (sequence.IndexOf(state, currentIndex + 1) < 0) {
							// This is a unique element in the sequence, so we know where we are now.
							break;
						}
					}
				}
			}

			// Wait for the right time.
			index--;
			while (currentIndex != index) {
				await Delay(0.25);
				var state2 = interrupt.Read(Reader);
				if (state2 != state) {
					state = state2;
					currentIndex = state2.Word == null ? -1 : currentIndex + 1;
				}
			}
		}

		await interrupt.SubmitAsync(Button.A);
	}

	[AimlCategory("press <set>boolean</set> on <set>number</set>")]
	[AimlCategory("press <set>boolean</set> on the <set>ordinal</set> ^")]
	internal static Task SubmitCommand(AimlAsyncContext context, bool pressYes, int index) {
		var script = GameState.Current.CurrentScript<ColourFlash>();
		return script.Submit(context, pressYes, index);
	}
	[AimlCategory("press <set>boolean</set>")]
	[AimlCategory("press <set>boolean</set> any time")]
	internal static Task SubmitCommand(AimlAsyncContext context, bool pressYes) {
		var script = GameState.Current.CurrentScript<ColourFlash>();
		return script.Submit(context, pressYes, null);
	}
	
	#region Log templates
	
	[LoggerMessage(LogLevel.Information, "Waiting for start")]
	private partial void LogWaitingForStart();
	
	[LoggerMessage(LogLevel.Information, "Reading sequence")]
	private partial void LogReadingSequence();
	
	#endregion
}