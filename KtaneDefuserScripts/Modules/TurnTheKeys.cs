namespace KtaneDefuserScripts.Modules;
[AimlInterface("TurnTheKeys")]
internal class TurnTheKeys : ModuleScript<KtaneDefuserConnector.Components.TurnTheKeys> {
	public override string IndefiniteDescription => "Turn the Keys";
	public override PriorityCategory PriorityCategory => PriorityCategory.MustSolveBeforeSome;

	public int Priority { get; private set; }

	private int _selectionIndex;

	protected internal override void Initialise(Interrupt interrupt) {
		using var ss = DefuserConnector.Instance.TakeScreenshot();
		var data = DefuserConnector.Instance.ReadComponent(ss, DefuserConnector.Instance.GetLightsState(ss), Reader, Utils.GetPoints(GameState.Current.Modules[ModuleIndex].Slot));
		Priority = data.Priority;
	}

	[AimlCategory("turn the keys", Topic = "*")]
	internal static async Task Turn(AimlAsyncContext context) {
		using var interrupt = await Interrupt.EnterAsync(context);
		interrupt.Context.Reply("Turning the keys.");

		var modules = GameState.Current.Modules.Where(e => e.Script is TurnTheKeys);

		foreach (var e in modules.OrderByDescending(e => ((TurnTheKeys) e.Script).Priority)) {
			// Turn the right key.
			var script = (TurnTheKeys) e.Script;
			await Utils.SelectModuleAsync(interrupt, script.ModuleIndex, false);
			if (script._selectionIndex < 1) {
				script._selectionIndex = 1;
				interrupt.SendInputs(Button.Right);
			}
			interrupt.SendInputs(Button.A);
		}

		foreach (var e in modules.OrderBy(e => ((TurnTheKeys) e.Script).Priority)) {
			// Turn the left key.
			var script = (TurnTheKeys) e.Script;
			await Utils.SelectModuleAsync(interrupt, script.ModuleIndex, false);
			if (script._selectionIndex > 0) {
				script._selectionIndex = 0;
				interrupt.SendInputs(Button.Left);
			}
			await interrupt.SubmitAsync(Button.A);
		}
	}
}
