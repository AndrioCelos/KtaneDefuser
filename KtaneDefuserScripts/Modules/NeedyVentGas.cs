namespace KtaneDefuserScripts.Modules;
[AimlInterface("NeedyVentGas")]
internal class NeedyVentGas : ModuleScript<KtaneDefuserConnector.Components.NeedyVentGas> {
	public override string IndefiniteDescription => "Needy Vent Gas";
	public override PriorityCategory PriorityCategory => PriorityCategory.Needy;

	private int _highlight;

	protected internal override async void NeedyStateChanged(AimlAsyncContext context, NeedyState newState) {
		try {
			if (newState != NeedyState.Running) return;
			await Delay(25);
			using var interrupt = await ModuleInterruptAsync(context);
			var data = interrupt.Read(Reader);
			if (data.Message != null)
				await PressButtonAsync(interrupt, data.Message[0] == 'D' ? 1 : 0);
		} catch (Exception ex) {
			LogException(ex);
		}
	}

	private async Task PressButtonAsync(Interrupt interrupt, int x) {
		var buttons = new List<Button>();
		if (x != _highlight) {
			buttons.Add(x == 0 ? Button.Left : Button.Right);
			_highlight = x;
		}
		buttons.Add(Button.A);
		await interrupt.SendInputsAsync(buttons);
	}
}
