using System.Text;

namespace BombDefuserScripts.Modules;

[AimlInterface("NeedyKnob")]
internal class NeedyKnob : ModuleScript<BombDefuserConnector.Components.NeedyKnob> {
	public override string IndefiniteDescription => "a Needy Knob";
	public override PriorityCategory PriorityCategory => PriorityCategory.Needy;

	private Counts? counts;
	private Direction direction;

	private static readonly Dictionary<Counts, Direction> correctDirections = new();
	private static NeedyKnob? currentKnob;

	protected internal override async void NeedyStateChanged(AimlAsyncContext context, NeedyState newState) {
		if (newState != NeedyState.Running) return;

		using var interrupt = await this.PrepareToReadAsync(context);
		if (interrupt is not null) context = interrupt.Context;
		var ss = await AimlTasks.TakeScreenshotAsync();
		var data = BombDefuserAimlService.Instance.ReadComponent(ss, GetProcessor(), Utils.GetPoints(GameState.Current.Modules[this.ModuleIndex].Slot));
		var counts = new Counts();
		for (var i = 0; i < 12; i++) {
			if (data.Lights[i]) {
				if (i % 6 < 3) counts.Left++;
				else counts.Right++;
			}
		}
		context.RequestProcess.Log(Aiml.LogLevel.Info, $"Knob ({this.ModuleIndex}) activated: {counts}");
		context.RequestProcess.Log(Aiml.LogLevel.Info, $"Knob:{string.Join(null, from i in Enumerable.Range(0, 6) select data.Lights[i] ? " *" : " .")}");
		context.RequestProcess.Log(Aiml.LogLevel.Info, $"Knob:{string.Join(null, from i in Enumerable.Range(0, 6) select data.Lights[6 + i] ? " *" : " .")}");
		if (correctDirections.TryGetValue(counts, out var correctPosition)) {
			if (this.direction != correctPosition) {
				using var interrupt2 = interrupt ?? await Interrupt.EnterAsync(context);
				await this.TurnAsync(interrupt2, correctPosition);
			}
		} else {
			this.counts = counts;
			currentKnob = this;
			context.Reply($"<oob><queue/></oob> Knob {this.ModuleIndex + 1} is active. Counts: {counts.Left}, {counts.Right}.");
		}
	}

	private async Task<Interrupt?> PrepareToReadAsync(AimlAsyncContext context) {
		if (Utils.CanReadModuleImmediately(this.ModuleIndex)) return null;
		var interrupt = await Interrupt.EnterAsync(context);
		await Utils.SelectModuleAsync(interrupt.Context, this.ModuleIndex);
		await AimlTasks.Delay(0.5);
		return interrupt;
	}

	private async Task HandleInputAsync(AimlAsyncContext context, Direction direction) {
		if (counts is null) {
			context.Reply("It is not active.");
			return;
		}
		correctDirections[counts.Value] = direction;
		if (this.direction != direction) {
			using var interrupt = await Interrupt.EnterAsync(context);
			await this.TurnAsync(interrupt, direction);
		}
	}

	private async Task TurnAsync(Interrupt interrupt, Direction direction) {
		var builder = new StringBuilder();
		var d = direction - this.direction;
		if (d < 0) d += 4;
		for (; d > 0; d--)
			builder.Append("a ");
		this.direction = direction;
		await Utils.SelectModuleAsync(interrupt.Context, this.ModuleIndex);
		await interrupt.Context.SendInputsAsync(builder.ToString());
	}

	[AimlCategory("<set>KnobDirection</set>", That = "Counts *", Topic = "*")]
	[AimlCategory("<set>KnobDirection</set>", Topic = "*")]
	[AimlCategory("<set>KnobDirection</set> position", That = "Counts *", Topic = "*")]
	[AimlCategory("<set>KnobDirection</set> position", Topic = "*")]
	[AimlCategory("knob is <set>KnobDirection</set>", Topic = "*")]
	internal static Task Read(AimlAsyncContext context, Direction direction) {
		if (currentKnob is null) {
			context.Reply("It is not active.");
			return Task.CompletedTask;
		}
		context.Reply("Roger.");
		var task = currentKnob.HandleInputAsync(context, direction);
		currentKnob = null;
		return task;
	}

	[AimlCategory("knob <set>number</set> is <set>Direction</set>", Topic = "*")]
	internal static Task Read(AimlAsyncContext context, int moduleNum, Direction direction) {
		if (GameState.Current.Modules[moduleNum - 1].Script is not NeedyKnob knobScript) {
			context.Reply("That is not a Knob.");
			return Task.CompletedTask;
		}
		context.Reply("Roger.");
		return knobScript.HandleInputAsync(context, direction);
	}

	private record struct Counts(int Left, int Right);
}
