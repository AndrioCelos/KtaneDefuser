namespace KtaneDefuserScripts.Modules;
[AimlInterface("Maze")]
internal class Maze : ModuleScript<KtaneDefuserConnector.Components.Maze> {
	public override string IndefiniteDescription => "a Maze";
	private Direction _highlight;

	protected internal override void Started(AimlAsyncContext context) => context.AddReply("ready");

	[AimlCategory("read")]
	internal static async Task Read(AimlAsyncContext context) {
		using var interrupt = await CurrentModuleInterruptAsync(context);
		var data = interrupt.Read(Reader);
		interrupt.Context.Reply(data.Circle2 is { } circle2
			? $"Markings at {Nato.Speak(data.Circle1.ToString())} and {Nato.Speak(circle2.ToString())}. Starting at {Nato.Speak(data.Start.ToString())}. The goal is {Nato.Speak(data.Goal.ToString())}."
			: $"Marking at {Nato.Speak(data.Circle1.ToString())}. Starting at {Nato.Speak(data.Start.ToString())}. The goal is {Nato.Speak(data.Goal.ToString())}.");
		interrupt.Context.AddReplies("left", "down", "up", "right");
	}

	[AimlCategory("<set>Direction</set>")]
	internal static Task Input1(AimlAsyncContext context, string s1)
		=> GameState.Current.CurrentScript<Maze>().ProcessInputAsync(context, s1);
	[AimlCategory("<set>Direction</set> *")]
	internal static Task Input2(AimlAsyncContext context, string s1, string? s2)
		=> GameState.Current.CurrentScript<Maze>().ProcessInputAsync(context, $"{s1} {s2}");

	[AimlCategory("move *"), AimlCategory("press *")]
	internal static Task Input(AimlAsyncContext context, string s)
		=> GameState.Current.CurrentScript<Maze>().ProcessInputAsync(context, s);

	private async Task ProcessInputAsync(AimlAsyncContext context, string s) {
		var buttons = new List<Button>();
		var tokens = s.Split((char[]?) null, StringSplitOptions.RemoveEmptyEntries);
		var currentHighlight = _highlight;
		for (var i = 0; i < tokens.Length; i++) {
			var token = tokens[i].ToLowerInvariant();
			if (token is "time" or "times" or "step" or "steps" or "space" or "spaces") continue;
			var direction = token switch {
				"up" or "north" => Direction.Up,
				"right" or "east" => Direction.Right,
				"down" or "south" => Direction.Down,
				"left" or "west" => Direction.Left,
				_ => throw new ArgumentException("Invalid direction")
			};
			if (tokens.ElementAtOrDefault(i + 1) is { } s2 && int.TryParse(s2, out var count)) {
				if (count is not (> 0 and < 6)) throw new ArgumentException("Invalid number of steps");
				i++;
			} else
				count = 1;

			if (direction != currentHighlight) {
				buttons.Add(direction switch { Direction.Up => Button.Up, Direction.Right => Button.Right, Direction.Down => Button.Down, _ => Button.Left });
				currentHighlight = direction;
			}
			for (; count > 0; count--)
				buttons.Add(Button.A);
		}
		_highlight = currentHighlight;
		using var interrupt = await ModuleInterruptAsync(context);
		var result = await interrupt.SubmitAsync(buttons);
		if (result == ModuleStatus.Solved) return;

		var data = interrupt.Read(Reader);
		interrupt.Context.Reply($"<priority/> Now at {Nato.Speak(data.Start.ToString())}.");
		interrupt.Context.AddReplies("left", "down", "up", "right");
	}
}
