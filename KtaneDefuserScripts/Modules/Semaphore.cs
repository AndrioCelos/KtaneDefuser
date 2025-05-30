﻿using static KtaneDefuserConnector.Components.Semaphore;

namespace KtaneDefuserScripts.Modules;
[AimlInterface("Semaphore")]
internal class Semaphore() : ModuleScript<KtaneDefuserConnector.Components.Semaphore>(3, 1) {
	public override string IndefiniteDescription => "Semaphore";

	private static Dictionary<KtaneDefuserConnector.Components.Semaphore.Direction, string> DirectionDescriptions { get; } = new() {
		{ KtaneDefuserConnector.Components.Semaphore.Direction.Down, "down" },
		{ KtaneDefuserConnector.Components.Semaphore.Direction.DownLeft, "down left" },
		{ KtaneDefuserConnector.Components.Semaphore.Direction.Left, "left" },
		{ KtaneDefuserConnector.Components.Semaphore.Direction.UpLeft, "up left" },
		{ KtaneDefuserConnector.Components.Semaphore.Direction.Up, "up" },
		{ KtaneDefuserConnector.Components.Semaphore.Direction.UpRight, "up right" },
		{ KtaneDefuserConnector.Components.Semaphore.Direction.Right, "right" },
		{ KtaneDefuserConnector.Components.Semaphore.Direction.DownRight, "down right" },
	};

	private List<ReadData>? _displays;
	private int _display;

	protected internal override void Started(AimlAsyncContext context) => context.AddReply("ready");

	[AimlCategory("read")]
	internal static async Task Read(AimlAsyncContext context) {
		var script = GameState.Current.CurrentScript<Semaphore>();
		using var interrupt = await CurrentModuleInterruptAsync(context);
		
		if (script._display > 0) {
			script.Select(interrupt, 0, 0);
			while (script._display > 0) {
				interrupt.SendInputs(Button.A);
				script._display--;
			}
			await interrupt.WaitInputsAsync();
			await Delay(0.5);
		}

		script._displays = [];
		while (true) {
			var read = interrupt.Read(Reader);
			if (script._displays.Count > 0) {
				if (read == script._displays.Last()) break;
				script._display++;
			}
			script._displays.Add(read);
			context.Reply($"<priority/> {DirectionDescriptions[read.LeftFlag]} and {DirectionDescriptions[read.RightFlag]}");

			await script.InteractWaitAsync(interrupt, 2, 0);
			await Delay(0.5);
		}
	}

	private static async Task Submit(AimlAsyncContext context, int index) {
		var script = GameState.Current.CurrentScript<Semaphore>();
		if (index <= 0 || index > script._displays?.Count) {
			context.Reply("Not a valid position.");
			return;
		}

		using var interrupt = await CurrentModuleInterruptAsync(context);

		while (script._display < index) {
			script.Interact(interrupt, 2, 0);
			script._display++;
		}

		while (script._display > index) {
			script.Interact(interrupt, 0, 0);
			script._display--;
		}

		await script.InteractWaitAsync(interrupt, 1, 0);
		await interrupt.CheckStatusAsync();
	}

	[AimlCategory("<set>number</set>"), AimlCategory("submit <set>number</set>")]
	internal static Task SubmitNumber(AimlAsyncContext context, int index) => Submit(context, index - 1);

	[AimlCategory("<set>ordinal</set>"), AimlCategory("submit <set>ordinal</set>")]
	internal static Task SubmitOrdinal(AimlAsyncContext context, string index) => Submit(context, Utils.ParseOrdinal(index) - 1);

	[AimlCategory("submit * and *"), AimlCategory("submit * then *")]
	internal static Task SubmitSignal(AimlAsyncContext context, string leftFlagStr, string rightFlagStr) {
		if (!TryParseDirection(leftFlagStr, out var leftFlag) || !TryParseDirection(rightFlagStr, out var rightFlag)) {
			context.Reply("Not a valid direction.");
			return Task.CompletedTask;
		}

		var script = GameState.Current.CurrentScript<Semaphore>();
		if (script._displays == null) {
			context.Reply("Need to read the module first.");
			return Task.CompletedTask;
		}

		var i = script._displays.IndexOf(new(new(2, 0), leftFlag, rightFlag));
		if (i < 0) {
			context.Reply("That signal was not present.");
			return Task.CompletedTask;
		}

		return Submit(context, i);
	}

	private static bool TryParseDirection(string s, out KtaneDefuserConnector.Components.Semaphore.Direction direction) {
		switch (s.ToLowerInvariant()) {
			case "down": direction = KtaneDefuserConnector.Components.Semaphore.Direction.Down; break;
			case "down left": direction = KtaneDefuserConnector.Components.Semaphore.Direction.DownLeft; break;
			case "left": direction = KtaneDefuserConnector.Components.Semaphore.Direction.Left; break;
			case "up left": direction = KtaneDefuserConnector.Components.Semaphore.Direction.UpLeft; break;
			case "up": direction = KtaneDefuserConnector.Components.Semaphore.Direction.Up; break;
			case "up right": direction = KtaneDefuserConnector.Components.Semaphore.Direction.UpRight; break;
			case "right": direction = KtaneDefuserConnector.Components.Semaphore.Direction.Right; break;
			case "down right": direction = KtaneDefuserConnector.Components.Semaphore.Direction.DownRight; break;
			default:
				direction = 0;
				return false;
		}
		return true;
	}
}
