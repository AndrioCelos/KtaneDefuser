using System.Text;

namespace BombDefuserScripts.Modules;
[AimlInterface("NeedyVentGas")]
internal class NeedyVentGas : ModuleScript<BombDefuserConnector.Components.NeedyVentGas> {
	public override string IndefiniteDescription => "Needy Vent Gas";
	public override PriorityCategory PriorityCategory => PriorityCategory.Needy;

	private int highlight;

	protected internal override async void NeedyStateChanged(AimlAsyncContext context, NeedyState newState) {
		if (newState != NeedyState.Running) return;
		await AimlTasks.Delay(25);
		using var interrupt = await Interrupt.EnterAsync(context);
		context = interrupt.Context;
		await Utils.SelectModuleAsync(interrupt, this.ModuleIndex);
		await AimlTasks.Delay(0.75);

		var data = ReadCurrent(Reader);
		if (data.Message != null)
			await this.PressButtonAsync(interrupt, data.Message[0] == 'D' ? 1 : 0);
	}

	private async Task PressButtonAsync(Interrupt interrupt, int x) {
		var builder = new StringBuilder();
		if (x != this.highlight) {
			builder.Append(x == 0 ? "left " : "right ");
			this.highlight = x;
		}
		builder.Append('a');
		await interrupt.SendInputsAsync(builder.ToString());
	}
}
