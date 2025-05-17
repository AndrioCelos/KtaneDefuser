namespace KtaneDefuserScripts.Modules;
[AimlInterface("Wires")]
internal class Wires : ModuleScript<KtaneDefuserConnector.Components.Wires> {
	public override string IndefiniteDescription => "Wires";
	
	private int _wireCount;
	private int _highlight;

	protected internal override void Started(AimlAsyncContext context) => context.AddReply("ready");

	[AimlCategory("read")]
	internal static async Task Read(AimlAsyncContext context) {
		using var interrupt = await CurrentModuleInterruptAsync(context);
		var data = interrupt.Read(Reader);
		GameState.Current.CurrentScript<Wires>()._wireCount = data.Colours.Length;
		interrupt.Context.Reply($"{data.Colours.Length} wires: {string.Join(", ", data.Colours)}.");

		interrupt.Context.AddReply("cut the first wire");
		for (var i = 2; i <= data.Colours.Length; i++)
			interrupt.Context.AddReply(Utils.ToOrdinal(i), $"cut wire {i}");
	}

	[AimlCategory("cut wire *")]
	internal static async Task CutWire(AimlAsyncContext context, int wireNum) {
		wireNum--;
		var buttons = new List<Button>();
		var script = GameState.Current.CurrentScript<Wires>();
		while (script._highlight < wireNum) {
			buttons.Add(Button.Down);
			script._highlight++;
		}
		while (script._highlight > wireNum) {
			buttons.Add(Button.Up);
			script._highlight--;
		}
		buttons.Add(Button.A);
		using var interrupt = await CurrentModuleInterruptAsync(context);
		await interrupt.SubmitAsync(buttons);
	}

	[AimlCategory("cut the <set>ordinal</set> wire")]
	internal static Task CutWireOrdinal(AimlAsyncContext context, string ordinal) => CutWire(context, Utils.ParseOrdinal(ordinal));

	[AimlCategory("cut the last wire")]
	internal static Task CutWireLast(AimlAsyncContext context) => CutWire(context, GameState.Current.CurrentScript<Wires>()._wireCount);
}
