namespace KtaneDefuserScripts.Modules;
[AimlInterface("NeedyVentGas")]
internal class NeedyVentGas() : ModuleScript<KtaneDefuserConnector.Components.NeedyVentGas>(2, 1) {
	public override string IndefiniteDescription => "Needy Vent Gas";
	public override PriorityCategory PriorityCategory => PriorityCategory.Needy;

	protected internal override async void NeedyStateChanged(AimlAsyncContext context, NeedyState newState) {
		try {
			if (newState != NeedyState.Running) return;
			await Delay(25);
			using var interrupt = await ModuleInterruptAsync(context);
			var data = interrupt.Read(Reader);
			if (data.Message != null)
				Interact(interrupt, data.Message[0] == 'D' ? 1 : 0, 0);
		} catch (Exception ex) {
			LogException(ex);
		}
	}
}
