using System.Text;

namespace BombDefuserScripts.Modules;
[AimlInterface("Memory")]
internal class Memory : ModuleScript<BombDefuserConnector.Components.Memory> {
	public override string IndefiniteDescription => "Memory";

	private int[] keyLabels = new int[4];
	private int highlight;

	protected internal override void ModuleSelected(AimlAsyncContext context) => this.Read(context);

	private async Task WaitRead(AimlAsyncContext context) {
		await AimlTasks.Delay(3);
		this.Read(context);
	}

	private void Read(AimlAsyncContext context) {
		var data = ReadCurrent(Reader);
		this.keyLabels = data.Keys;
		context.Reply(data.Display.ToString());
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
		var builder = new StringBuilder();
		var highlight = this.highlight;
		while (highlight > index) {
			highlight--;
			builder.Append("left ");
		}
		while (highlight < index) {
			highlight++;
			builder.Append("right ");
		}
		builder.Append('a');
		using var interrupt = await Interrupt.EnterAsync(context);
		this.highlight = highlight;
		var result = await interrupt.SubmitAsync(builder.ToString());
		if (result != ModuleLightState.Solved) {
			if (result == ModuleLightState.Off)
				interrupt.Context.Reply(fromLabel ? $"The position was {index + 1}." : $"The label was {this.keyLabels[index]}.");
			_ = this.WaitRead(interrupt.Context);
		}
	}
}
