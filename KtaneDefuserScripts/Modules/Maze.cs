using SixLabors.ImageSharp;

namespace KtaneDefuserScripts.Modules;
[AimlInterface("Maze")]
internal class Maze() : ModuleScript<KtaneDefuserConnector.Components.Maze>(3, 3, 1) {
	public override string IndefiniteDescription => "a Maze";

	protected override bool IsSelectablePresent(int x, int y) => (x + y) % 2 != 0;
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
		var points = new List<(Point point, int count)>();
		var tokens = s.Split((char[]?) null, StringSplitOptions.RemoveEmptyEntries);
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

			points.Add((direction switch { Direction.Up => new(1, 0), Direction.Right => new(2, 1), Direction.Down => new(1, 2), _ => new(0, 1) }, count));
		}

		using var interrupt = await ModuleInterruptAsync(context);
		foreach (var (point, count) in points) {
			for (var n = count; n > 0; n--) {
				await InteractWaitAsync(interrupt, point.X, point.Y);
				if (interrupt.HasStrikeOccurred) goto readNext;
			}
		}
		var result = await interrupt.CheckStatusAsync();
		if (result == ModuleStatus.Solved) return;

readNext:
		var data = interrupt.Read(Reader);
		interrupt.Context.Reply($"<priority/> Now at {Nato.Speak(data.Start.ToString())}.");
		interrupt.Context.AddReplies("left", "down", "up", "right");
	}
}
