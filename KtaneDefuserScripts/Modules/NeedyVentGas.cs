using KtaneDefuserConnectorApi;

namespace KtaneDefuserScripts.Modules;
[AimlInterface("NeedyVentGas")]
internal class NeedyVentGas : ModuleScript<KtaneDefuserConnector.Components.NeedyVentGas> {
	public override string IndefiniteDescription => "Needy Vent Gas";
	public override PriorityCategory PriorityCategory => PriorityCategory.Needy;

	private int highlight;

	protected internal override async void NeedyStateChanged(AimlAsyncContext context, NeedyState newState) {
		if (newState != NeedyState.Running) return;
		await Delay(25);
		using var interrupt = await this.ModuleInterruptAsync(context);
		context = interrupt.Context;

		var data = interrupt.Read(Reader);
		if (data.Message != null)
			await this.PressButtonAsync(interrupt, data.Message[0] == 'D' ? 1 : 0);
	}

	private async Task PressButtonAsync(Interrupt interrupt, int x) {
		var buttons = new List<Button>();
		if (x != this.highlight) {
			buttons.Add(x == 0 ? Button.Left : Button.Right);
			this.highlight = x;
		}
		buttons.Add(Button.A);
		await interrupt.SendInputsAsync(buttons);
	}
}
