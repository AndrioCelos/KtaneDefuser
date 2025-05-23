namespace KtaneDefuserScripts.Modules;
[AimlInterface("Memory")]
internal class Memory() : ModuleScript<KtaneDefuserConnector.Components.Memory>(4, 1) {
	public override string IndefiniteDescription => "Memory";

	private bool _readyToRead;
	private int[] _keyLabels = new int[4];

	protected internal override void Started(AimlAsyncContext context) => _readyToRead = true;

	protected internal override void ModuleSelected(Interrupt interrupt) {
		if (!_readyToRead) return;
		_readyToRead = false;
		Read(interrupt);
	}

	private async Task WaitRead(Interrupt interrupt) {
		await Delay(3);
		Read(interrupt);
	}

	private void Read(Interrupt interrupt) {
		var data = interrupt.Read(Reader);
		_keyLabels = data.Keys;
		interrupt.Context.Reply(data.Display.ToString());
		interrupt.Context.Reply("<reply>position 1</reply><reply><text>2</text><postback>position 2</postback></reply><reply><text>3</text><postback>position 3</postback></reply><reply><text>4</text><postback>position 4</postback></reply>");
		interrupt.Context.Reply("<reply>label 1</reply><reply><text>2</text><postback>label 2</postback></reply><reply><text>3</text><postback>label 3</postback></reply><reply><text>4</text><postback>label 4</postback></reply>");
	}

	[AimlCategory("position <set>number</set>")]
	public static Task PositionAsync(AimlAsyncContext context, int position) {
		if (position is > 0 and <= 4)
			return GameState.Current.CurrentScript<Memory>().PressButtonAsync(context, position - 1, false);
		context.Reply("Not a valid position.");
		return Task.CompletedTask;
	}

	[AimlCategory("<set>ordinal</set> position")]
	public static Task PositionAsync2(AimlAsyncContext context, string positionStr) {
		var position = Utils.ParseOrdinal(positionStr);
		if (position is > 0 and <= 4)
			return GameState.Current.CurrentScript<Memory>().PressButtonAsync(context, position - 1, false);
		context.Reply("Not a valid position.");
		return Task.CompletedTask;
	}

	[AimlCategory("label <set>number</set>")]
	public static Task LabelAsync(AimlAsyncContext context, int label) {
		var script = GameState.Current.CurrentScript<Memory>();
		if (label is > 0 and <= 4)
			return script.PressButtonAsync(context, Array.IndexOf(script._keyLabels, label), true);
		context.Reply("Not a valid label.");
		return Task.CompletedTask;
	}

	private async Task PressButtonAsync(AimlAsyncContext context, int index, bool fromLabel) {
		using var interrupt = await ModuleInterruptAsync(context);
		Select(interrupt, index, 0);
		var result = await interrupt.SubmitAsync();
		if (result != ModuleStatus.Solved) {
			if (result == ModuleStatus.Off)
				interrupt.Context.Reply(fromLabel ? $"The position was {index + 1}." : $"The label was {_keyLabels[index]}.");
			await WaitRead(interrupt);
		}
	}
}
