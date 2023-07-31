using System.Text;

namespace BombDefuserScripts.Modules;
[AimlInterface("Wires")]
internal class Wires : ModuleScript<BombDefuserConnector.Components.Wires> {
	public override string IndefiniteDescription => "Wires";
	int wireCount;

	[AimlCategory("read")]
	internal static async void Read(AimlAsyncContext context) {
		var data = await ReadCurrentAsync(GetProcessor());
		GameState.Current.CurrentScript<Wires>().wireCount = data.Colours.Length;
		context.Reply($"{data.Colours.Length} wires: {string.Join(", ", data.Colours)}. <reply>cut the [nth] wire</reply>");
	}

	[AimlCategory("cut wire *")]
	internal static async Task CutWire(AimlAsyncContext context, int wireNum) {
		wireNum--;
		var builder = new StringBuilder();
		var module = GameState.Current.SelectedModule!;
		while (module.Y < wireNum) {
			builder.Append("down ");
			module.Y++;
		}
		while (module.Y > wireNum) {
			builder.Append("up ");
			module.Y--;
		}
		builder.Append('a');
		await Interrupt.SubmitAsync(context, builder.ToString());
	}

	[AimlCategory("cut the <set>ordinal</set> wire")]
	internal static Task CutWireOrdinal(AimlAsyncContext context, string ordinal) => CutWire(context, Utils.ParseOrdinal(ordinal));

	[AimlCategory("cut the last wire")]
	internal static Task CutWireLast(AimlAsyncContext context) => CutWire(context, GameState.Current.CurrentScript<Wires>().wireCount);
}
