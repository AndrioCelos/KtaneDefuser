using System.Text;

namespace BombDefuserScripts.Modules;
[AimlInterface("Wires")]
internal class Wires : ModuleScript<BombDefuserConnector.Components.Wires> {
	public override string IndefiniteDescription => "Wires";
	
	private int wireCount;
	private int highlight;

	[AimlCategory("read")]
	internal static void Read(AimlAsyncContext context) {
		var data = ReadCurrent(GetProcessor());
		GameState.Current.CurrentScript<Wires>().wireCount = data.Colours.Length;
		context.Reply($"{data.Colours.Length} wires: {string.Join(", ", data.Colours)}. <reply>cut the [nth] wire</reply>");
	}

	[AimlCategory("cut wire *")]
	internal static async Task CutWire(AimlAsyncContext context, int wireNum) {
		wireNum--;
		var builder = new StringBuilder();
		var script = GameState.Current.CurrentScript<Wires>();
		while (script.highlight < wireNum) {
			builder.Append("down ");
			script.highlight++;
		}
		while (script.highlight > wireNum) {
			builder.Append("up ");
			script.highlight--;
		}
		builder.Append('a');
		await Interrupt.SubmitAsync(context, builder.ToString());
	}

	[AimlCategory("cut the <set>ordinal</set> wire")]
	internal static Task CutWireOrdinal(AimlAsyncContext context, string ordinal) => CutWire(context, Utils.ParseOrdinal(ordinal));

	[AimlCategory("cut the last wire")]
	internal static Task CutWireLast(AimlAsyncContext context) => CutWire(context, GameState.Current.CurrentScript<Wires>().wireCount);
}
