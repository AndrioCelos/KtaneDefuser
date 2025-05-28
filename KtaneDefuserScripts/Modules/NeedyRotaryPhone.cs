namespace KtaneDefuserScripts.Modules;
[AimlInterface("NeedyRotaryPhone")]
internal class NeedyRotaryPhone() : ModuleScript<KtaneDefuserConnector.Components.NeedyRotaryPhone>(10, 1) {
	public override string IndefiniteDescription => "a Needy Rotary Phone";
	public override PriorityCategory PriorityCategory => PriorityCategory.Needy;

	private int _total;

	protected internal override void Initialise(Interrupt interrupt) {
		var quadrilateral = Utils.GetPoints(GameState.Current.Modules[ModuleIndex].Slot);
		using var ss = DefuserConnector.Instance.TakeScreenshot();
		_total = DefuserConnector.Instance.ReadComponent(ss, DefuserConnector.Instance.GetLightsState(ss), Reader, quadrilateral).Number;
	}

	protected internal override async void NeedyStateChanged(AimlAsyncContext context, NeedyState newState) {
		try {
			if (newState != NeedyState.Running) return;
			await Delay(30);
			if (newState != NeedyState.Running) return;
			using var interrupt = await ModuleInterruptAsync(context);
			interrupt.Context.Reply("Dialling the rotary phone.");
			var data = interrupt.Read(Reader);
			if (data.Time is null) return;

			_total = (_total + data.Number) % 1000;
			for (var i = 0; ; i++) {
				var n = i switch {
					0 => _total / 100,
					1 => _total / 10 % 10,
					_ => _total % 10
				};
				await InteractWaitAsync(interrupt, n == 0 ? 9 : n - 1, 0);
				if (i >= 2) break;
				await Delay(n == 0 ? 3 : n * 0.3);
			}
		} catch (Exception ex) {
			LogException(ex);
		}
	}

	protected internal override async void Strike(AimlAsyncContext context) {
		using var interrupt = await Interrupt.EnterAsync(context);
		if (Utils.TryGetPoints(GameState.Current.Modules[ModuleIndex].Slot, out var quadrilateral)) {
			// Can read the module without looking away from the current one.
			using var ss = DefuserConnector.Instance.TakeScreenshot();
			_total = DefuserConnector.Instance.ReadComponent(ss, DefuserConnector.Instance.GetLightsState(ss), Reader, quadrilateral).Number;
		} else {
			await Utils.SelectModuleAsync(interrupt, ModuleIndex, true);
			using var ss = DefuserConnector.Instance.TakeScreenshot();
			_total = interrupt.Read(Reader).Number;
		}
	}
}
