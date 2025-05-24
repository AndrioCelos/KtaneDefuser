namespace KtaneDefuserScripts.Modules;
[AimlInterface("TurnTheKeys")]
internal class TurnTheKeys() : ModuleScript<KtaneDefuserConnector.Components.TurnTheKeys>(2, 1) {
	public override string IndefiniteDescription => "Turn the Keys";
	public override PriorityCategory PriorityCategory => PriorityCategory.MustSolveBeforeSome;

	private int _priority;

	protected internal override void Initialise(Interrupt interrupt) {
		using var ss = DefuserConnector.Instance.TakeScreenshot();
		var data = DefuserConnector.Instance.ReadComponent(ss, DefuserConnector.Instance.GetLightsState(ss), Reader, Utils.GetPoints(GameState.Current.Modules[ModuleIndex].Slot));
		_priority = data.Priority;
	}

	[AimlCategory("turn the keys", Topic = "*")]
	internal static async Task Turn(AimlAsyncContext context) {
		using var interrupt = await Interrupt.EnterAsync(context);
		interrupt.Context.Reply("Turning the keys.");

		foreach (var e in from m in GameState.Current.Modules where m.Script is TurnTheKeys orderby ((TurnTheKeys) m.Script)._priority descending select m) {
			// Turn the right key.
			var script = (TurnTheKeys) e.Script;
			await Utils.SelectModuleAsync(interrupt, script.ModuleIndex, false);
			await script.InteractWaitAsync(interrupt, 1, 0);
			if (interrupt.HasStrikeOccurred) return;
		}

		foreach (var e in from m in GameState.Current.Modules where m.Script is TurnTheKeys orderby ((TurnTheKeys) m.Script)._priority select m) {
			// Turn the left key.
			var script = (TurnTheKeys) e.Script;
			await Utils.SelectModuleAsync(interrupt, script.ModuleIndex, false);
			await script.InteractWaitAsync(interrupt, 0, 0);
			await interrupt.CheckStatusAsync();
			if (interrupt.HasStrikeOccurred) return;
		}

		interrupt.Context.Reply("Turn the Keys is complete.");
	}
}
