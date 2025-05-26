namespace KtaneDefuserScripts.Modules;

[AimlInterface("NeedyKnob")]
internal partial class NeedyKnob() : ModuleScript<KtaneDefuserConnector.Components.NeedyKnob>(1, 1) {
	private static readonly Dictionary<Counts, Direction> CorrectDirections = [];

	public override string IndefiniteDescription => "a Needy Knob";
	public override PriorityCategory PriorityCategory => PriorityCategory.Needy;

	private Counts? _counts;
	private Direction _direction;
	private bool _isHandled;

	protected internal override async void NeedyStateChanged(AimlAsyncContext context, NeedyState newState) {
		KtaneDefuserConnector.Components.NeedyKnob.ReadData data;
		try {
			if (newState != NeedyState.Running) {
				_counts = null;
				return;
			}
			
			using var interrupt = await Interrupt.EnterAsync(context);
			if (Utils.TryGetPoints(GameState.Current.Modules[ModuleIndex].Slot, out var quadrilateral)) {
				// Can read the module without looking away from the current one.
				using var ss = DefuserConnector.Instance.TakeScreenshot();
				data = DefuserConnector.Instance.ReadComponent(ss, DefuserConnector.Instance.GetLightsState(ss), Reader, quadrilateral);
			} else {
				await Utils.SelectModuleAsync(interrupt, ModuleIndex, true);
				using var ss = DefuserConnector.Instance.TakeScreenshot();
				data = interrupt.Read(Reader);
			}

			var counts = new Counts();
			for (var i = 0; i < 12; i++) {
				if (!data.Lights[i]) continue;
				if (i % 6 < 3) counts.Left++;
				else counts.Right++;
			}

			_counts = counts;
			LogActivated(ModuleIndex + 1, counts,
				string.Join(' ', from i in Enumerable.Range(0, 6) select data.Lights[i] ? '*' : '.'),
				string.Join(' ', from i in Enumerable.Range(0, 6) select data.Lights[i + 6] ? '*' : '.'));

			if (CorrectDirections.TryGetValue(counts, out var correctPosition)) {
				if (_direction == correctPosition) return;
				await TurnAsync(interrupt, correctPosition);
			} else {
				_isHandled = false;
				context.Reply($"<priority/> Knob {ModuleIndex + 1} is active. Counts: {counts.Left}, {counts.Right}");
				context.AddReply("up", $"knob {ModuleIndex + 1} is up");
				context.AddReply("down", $"knob {ModuleIndex + 1} is down");
				context.AddReply("left", $"knob {ModuleIndex + 1} is left");
				context.AddReply("right", $"knob {ModuleIndex + 1} is right");
				context.Reply(".");
			}
		} catch (Exception ex) {
			LogException(ex);
		}
	}

	private async Task HandleInputAsync(AimlAsyncContext context, Direction direction) {
		if (_counts is null) {
			context.Reply("It is not active.");
			return;
		}
		context.Reply($"Setting the knob to {direction}.");
		CorrectDirections[_counts.Value] = direction;
		_isHandled = true;
		if (_direction != direction) {
			using var interrupt = await ModuleInterruptAsync(context, false);
			await TurnAsync(interrupt, direction);
		}
	}

	private async Task TurnAsync(Interrupt interrupt, Direction direction) {
		var presses = direction - _direction;
		if (presses < 0) presses += 4;
		_direction = direction;
		await Utils.SelectModuleAsync(interrupt, ModuleIndex, false);
		await interrupt.SendInputsAsync(Enumerable.Repeat(Button.A, presses));
	}

	[AimlCategory("<set>KnobDirection</set>", That = "Counts *", Topic = "*")]
	[AimlCategory("<set>KnobDirection</set>", Topic = "*")]
	[AimlCategory("<set>KnobDirection</set> position", That = "Counts *", Topic = "*")]
	[AimlCategory("<set>KnobDirection</set> position", Topic = "*")]
	[AimlCategory("knob is <set>KnobDirection</set>", Topic = "*")]
	internal static Task Read(AimlAsyncContext context, Direction direction) {
		var anyKnobs = false; NeedyKnob? currentKnob = null;
		foreach (var m in GameState.Current.Modules) {
			if (m.Script is NeedyKnob knob) {
				anyKnobs = true;
				if (knob.NeedyState == NeedyState.Running && !knob._isHandled) {
					if (currentKnob is null)
						currentKnob = knob;
					else {
						context.Reply("Please specify the number of the Knob.");
						return Task.CompletedTask;
					}
				}
			}
		}

		if (currentKnob is not null) return currentKnob.HandleInputAsync(context, direction);
		context.Reply(anyKnobs ? "It is not active." : "There does not seem to be a Knob.");
		return Task.CompletedTask;
	}

	[AimlCategory("knob <set>number</set> is <set>Direction</set>", Topic = "*")]
	internal static Task Read(AimlAsyncContext context, int moduleNum, Direction direction) {
		if (GameState.Current.Modules[moduleNum - 1].Script is not NeedyKnob knobScript) {
			context.Reply("That is not a Knob.");
			return Task.CompletedTask;
		}
		return knobScript.HandleInputAsync(context, direction);
	}
	
	#region Log templates
	
	[LoggerMessage(LogLevel.Information, "Knob ({Number}) activated: {Counts}\n{StatesLine1}\n{StatesLine2}")]
	private partial void LogActivated(int number, Counts counts, string statesLine1, string statesLine2);

	#endregion

	private record struct Counts(int Left, int Right);
}
