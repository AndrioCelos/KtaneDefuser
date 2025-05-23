namespace KtaneDefuserScripts.Modules;
[AimlInterface("Wires")]
internal class Wires() : ModuleScript<KtaneDefuserConnector.Components.Wires>(1, 6) {
	public override string IndefiniteDescription => "Wires";

	private int _wireCount;

	protected internal override void Started(AimlAsyncContext context) => context.AddReply("ready");

	[AimlCategory("read")]
	internal static async Task Read(AimlAsyncContext context) {
		using var interrupt = await CurrentModuleInterruptAsync(context);
		var data = interrupt.Read(Reader);
		GameState.Current.CurrentScript<Wires>()._wireCount = data.Colours.Length;
		GameState.Current.CurrentScript<Wires>().SelectableSize = new(1, data.Colours.Length);
		interrupt.Context.Reply($"{data.Colours.Length} wires: {string.Join(", ", data.Colours)}.");

		interrupt.Context.AddReply("cut the first wire");
		for (var i = 2; i <= data.Colours.Length; i++)
			interrupt.Context.AddReply(Utils.ToOrdinal(i), $"cut wire {i}");
	}

	[AimlCategory("cut wire *")]
	internal static async Task CutWire(AimlAsyncContext context, int wireNum) {
		var script = GameState.Current.CurrentScript<Wires>();
		using var interrupt = await script.ModuleInterruptAsync(context);
		script.Select(interrupt, 0, wireNum - 1);
		await interrupt.SubmitAsync();
	}

	[AimlCategory("cut the <set>ordinal</set> wire")]
	internal static Task CutWireOrdinal(AimlAsyncContext context, string ordinal) => CutWire(context, Utils.ParseOrdinal(ordinal));

	[AimlCategory("cut the last wire")]
	internal static Task CutWireLast(AimlAsyncContext context) => CutWire(context, GameState.Current.CurrentScript<Wires>()._wireCount);
}
