using System.Text;

namespace BombDefuserScripts.Modules;
[AimlInterface("NeedyVentGas")]
internal class NeedyVentGas : ModuleScript<BombDefuserConnector.Components.NeedyVentGas> {
	public override string IndefiniteDescription => "Needy Vent Gas";
	public override PriorityCategory PriorityCategory => PriorityCategory.Needy;

	private int highlightX;

	protected internal override async void NeedyStateChanged(AimlAsyncContext context, NeedyState newState) {
		if (newState != NeedyState.Running) return;
		await AimlTasks.Delay(25);
		using var interrupt = await Interrupt.EnterAsync(context);
		context = interrupt.Context;
		await Utils.SelectModuleAsync(context, this.ModuleIndex);
		await AimlTasks.Delay(0.5);

		var data = await ReadCurrentAsync(GetProcessor());
		if (data.Message != null)
			await this.PressButtonAsync(context, data.Message[0] == 'D' ? 1 : 0);
	}

	private async Task PressButtonAsync(AimlAsyncContext context, int x) {
		var builder = new StringBuilder();
		if (x != this.highlightX) {
			builder.Append(x == 0 ? "left " : "right ");
			this.highlightX = x;
		}
		builder.Append('a');
		await context.SendInputsAsync(builder.ToString());
	}
}
