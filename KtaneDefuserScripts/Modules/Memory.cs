namespace KtaneDefuserScripts.Modules;
[AimlInterface("Memory")]
internal class Memory : ModuleScript<KtaneDefuserConnector.Components.Memory> {
	public override string IndefiniteDescription => "Memory";

	private bool readyToRead;
	private int[] keyLabels = new int[4];
	private int highlight;

	protected internal override void Started(AimlAsyncContext context) => readyToRead = true;

	protected internal override async void ModuleSelected(AimlAsyncContext context) {
		if (readyToRead) {
			readyToRead = false;
			using var interrupt = await ModuleInterruptAsync(context);
			Read(interrupt);
		}
	}

	private async Task WaitRead(Interrupt interrupt) {
		await Delay(3);
		Read(interrupt);
	}

	private void Read(Interrupt interrupt) {
		var data = interrupt.Read(Reader);
		keyLabels = data.Keys;
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
			return script.PressButtonAsync(context, Array.IndexOf(script.keyLabels, label), true);
		context.Reply("Not a valid label.");
		return Task.CompletedTask;
	}

	private async Task PressButtonAsync(AimlAsyncContext context, int index, bool fromLabel) {
		var buttons = new List<Button>();
		var highlight = this.highlight;
		while (highlight > index) {
			highlight--;
			buttons.Add(Button.Left);
		}
		while (highlight < index) {
			highlight++;
			buttons.Add(Button.Right);
		}
		buttons.Add(Button.A);
		using var interrupt = await ModuleInterruptAsync(context);
		this.highlight = highlight;
		var result = await interrupt.SubmitAsync(buttons);
		if (result != ModuleLightState.Solved) {
			if (result == ModuleLightState.Off)
				interrupt.Context.Reply(fromLabel ? $"The position was {index + 1}." : $"The label was {keyLabels[index]}.");
			await WaitRead(interrupt);
		}
	}
}
