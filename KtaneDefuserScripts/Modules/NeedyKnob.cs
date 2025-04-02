﻿using KtaneDefuserConnector;
using KtaneDefuserConnectorApi;

namespace KtaneDefuserScripts.Modules;

[AimlInterface("NeedyKnob")]
internal partial class NeedyKnob : ModuleScript<KtaneDefuserConnector.Components.NeedyKnob> {
	public override string IndefiniteDescription => "a Needy Knob";
	public override PriorityCategory PriorityCategory => PriorityCategory.Needy;

	private Counts? counts;
	private Direction direction;
	private bool isHandled;

	private static readonly Dictionary<Counts, Direction> correctDirections = [];

	protected internal override async void NeedyStateChanged(AimlAsyncContext context, NeedyState newState) {
		if (newState == NeedyState.Running) { 
			using var interrupt = await this.PrepareToReadAsync(context);
			if (interrupt is not null) context = interrupt.Context;
			using var ss = KtaneDefuserConnector.DefuserConnector.Instance.TakeScreenshot();
			var data = KtaneDefuserConnector.DefuserConnector.Instance.ReadComponent(ss, KtaneDefuserConnector.DefuserConnector.Instance.GetLightsState(ss), Reader, Utils.GetPoints(GameState.Current.Modules[this.ModuleIndex].Slot));
			var counts = new Counts();
			for (var i = 0; i < 12; i++) {
				if (data.Lights[i]) {
					if (i % 6 < 3) counts.Left++;
					else counts.Right++;
				}
			}
			this.LogActivated(this.ModuleIndex + 1, counts,
				string.Join(' ', from i in Enumerable.Range(0, 6) select data.Lights[i] ? '*' : '.'),
				string.Join(' ', from i in Enumerable.Range(0, 6) select data.Lights[i + 6] ? '*' : '.'));
			this.counts = counts;
			if (correctDirections.TryGetValue(counts, out var correctPosition)) {
				if (this.direction != correctPosition) {
					using var interrupt2 = interrupt ?? await this.ModuleInterruptAsync(context);
					await this.TurnAsync(interrupt2, correctPosition);
				}
			} else {
				this.isHandled = false;
				context.Reply($"<priority/> Knob {this.ModuleIndex + 1} is active. Counts: {counts.Left}, {counts.Right}");
				context.AddReply("up", $"knob {this.ModuleIndex + 1} is up");
				context.AddReply("down", $"knob {this.ModuleIndex + 1} is down");
				context.AddReply("left", $"knob {this.ModuleIndex + 1} is left");
				context.AddReply("right", $"knob {this.ModuleIndex + 1} is right");
				context.Reply(".");
			}
		} else
			this.counts = null;
	}

	private async Task<Interrupt?> PrepareToReadAsync(AimlAsyncContext context) {
		if (Utils.CanReadModuleImmediately(this.ModuleIndex)) return null;
		var interrupt = await this.ModuleInterruptAsync(context);
		return interrupt;
	}

	private async Task HandleInputAsync(AimlAsyncContext context, Direction direction) {
		if (this.counts is null) {
			context.Reply("It is not active.");
			return;
		}
		context.Reply("Roger.");
		correctDirections[this.counts.Value] = direction;
		this.isHandled = true;
		if (this.direction != direction) {
			using var interrupt = await this.ModuleInterruptAsync(context, false);
			await this.TurnAsync(interrupt, direction);
		}
	}

	private async Task TurnAsync(Interrupt interrupt, Direction direction) {
		var presses = direction - this.direction;
		if (presses < 0) presses += 4;
		this.direction = direction;
		await Utils.SelectModuleAsync(interrupt, this.ModuleIndex, false);
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
				if (knob.NeedyState == NeedyState.Running && !knob.isHandled) {
					if (currentKnob is null)
						currentKnob = knob;
					else {
						context.Reply("Please specify the number of the Knob.");
						return Task.CompletedTask;
					}
				}
			}
		}
		if (currentKnob is null) {
			context.Reply(anyKnobs ? "It is not active." : "There does not seem to be a Knob.");
			return Task.CompletedTask;
		}
		return currentKnob.HandleInputAsync(context, direction);
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
