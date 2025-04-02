using KtaneDefuserConnectorApi;
using static KtaneDefuserConnector.Components.Semaphore;

namespace KtaneDefuserScripts.Modules;
[AimlInterface("Semaphore")]
internal partial class Semaphore : ModuleScript<KtaneDefuserConnector.Components.Semaphore> {

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

	private int highlight;
	private List<ReadData>? displays;
	private int display;

	protected internal override void Started(AimlAsyncContext context) => context.AddReply("ready");

	[AimlCategory("read")]
	internal static async Task Read(AimlAsyncContext context) {
		var script = GameState.Current.CurrentScript<Semaphore>();
		using var interrupt = await CurrentModuleInterruptAsync(context);
		
		if (script.display > 0) {
			var list = new List<Button>();
			while (script.highlight > 0) {
				list.Add(Button.Left);
				script.highlight--;
			}
			while (script.display > 0) {
				list.Add(Button.A);
				script.display--;
			}
			await interrupt.SendInputsAsync(list);
			await Delay(0.5);
		}

		script.displays = [];
		while (true) {
			var read = interrupt.Read(Reader);
			if (script.displays.Count > 0) {
				if (read == script.displays.Last()) break;
				script.display++;
			}
			script.displays.Add(read);
			context.Reply($"<priority/> {DirectionDescriptions[read.LeftFlag]} and {DirectionDescriptions[read.RightFlag]}");

			if (script.highlight < 2) {
				var list = new List<Button>();
				while (script.highlight < 2) {
					list.Add(Button.Right);
					script.highlight++;
				}
				list.Add(Button.A);
				await interrupt.SendInputsAsync(list);
			} else
				await interrupt.SendInputsAsync(Button.A);
			await Delay(0.5);
		}
	}

	private static async Task Submit(AimlAsyncContext context, int index) {
		var script = GameState.Current.CurrentScript<Semaphore>();
		if (index <= 0 || index > script.displays?.Count) {
			context.Reply("Not a valid position.");
			return;
		}

		using var interrupt = await CurrentModuleInterruptAsync(context);

		var list = new List<Button>();
		while (script.display < index) {
			while (script.highlight < 2) {
				list.Add(Button.Right);
				script.highlight++;
			}
			list.Add(Button.A);
			script.display++;
		}

		while (script.display > index) {
			while (script.highlight > 0) {
				list.Add(Button.Left);
				script.highlight--;
			}
			list.Add(Button.A);
			script.display--;
		}

		if (script.highlight == 0) list.Add(Button.Right);
		else if (script.highlight == 2) list.Add(Button.Left);
		script.highlight = 1;

		list.Add(Button.A);
		await interrupt.SubmitAsync(list);
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
		if (script.displays == null) {
			context.Reply("Need to read the module first.");
			return Task.CompletedTask;
		}

		var i = script.displays.IndexOf(new(leftFlag, rightFlag));
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
