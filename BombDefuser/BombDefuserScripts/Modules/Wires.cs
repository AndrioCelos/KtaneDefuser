using System.Text;

namespace BombDefuserScripts.Modules;
[AimlInterface("Wires")]
internal class Wires : ModuleScript<BombDefuserConnector.Components.Wires> {
	public override string IndefiniteDescription => "Wires";
	
	private int wireCount;
	private int highlight;

	[AimlCategory("read")]
	internal static async Task Read(AimlAsyncContext context) {
		using var interrupt = await CurrentModuleInterruptAsync(context);
		var data = interrupt.Read(Reader);
		GameState.Current.CurrentScript<Wires>().wireCount = data.Colours.Length;
		interrupt.Context.Reply($"{data.Colours.Length} wires: {string.Join(", ", data.Colours)}. <reply>cut the [nth] wire</reply>");
	}

	[AimlCategory("cut wire *")]
	internal static async Task CutWire(AimlAsyncContext context, int wireNum) {
		wireNum--;
		var buttons = new List<Button>();
		var script = GameState.Current.CurrentScript<Wires>();
		while (script.highlight < wireNum) {
			buttons.Add(Button.Down);
			script.highlight++;
		}
		while (script.highlight > wireNum) {
			buttons.Add(Button.Up);
			script.highlight--;
		}
		buttons.Add(Button.A);
		using var interrupt = await CurrentModuleInterruptAsync(context);
		await interrupt.SubmitAsync(buttons);
	}

	[AimlCategory("cut the <set>ordinal</set> wire")]
	internal static Task CutWireOrdinal(AimlAsyncContext context, string ordinal) => CutWire(context, Utils.ParseOrdinal(ordinal));

	[AimlCategory("cut the last wire")]
	internal static Task CutWireLast(AimlAsyncContext context) => CutWire(context, GameState.Current.CurrentScript<Wires>().wireCount);
}
