namespace KtaneDefuserScripts.Modules;
[AimlInterface("NeedyMath")]
internal class NeedyMath() : ModuleScript<KtaneDefuserConnector.Components.NeedyMath>(4, 3) {
	public override string IndefiniteDescription => "Needy Math";
	public override PriorityCategory PriorityCategory => PriorityCategory.Needy;

	protected internal override async void NeedyStateChanged(AimlAsyncContext context, NeedyState newState) {
		try {
			if (newState != NeedyState.Running) return;
			await Delay(25);
			using var interrupt = await ModuleInterruptAsync(context);
			var data = interrupt.Read(Reader);

			var pos = data.Display.IndexOfAny('+', '-');
			if (pos < 0) return;

			var a = int.Parse(data.Display[..pos]);
			var b = int.Parse(data.Display[pos..]);
			var answer = a + b;
			foreach (var c in Math.Abs(answer).ToString()) {
				var (x, y) = c == 0 ? (3, 0) : ((c - '1') % 3, (c - '1') / 3);
				PressButton(interrupt, x, y);
			}

			if (answer < 0) PressButton(interrupt, 3, 1);
			PressButton(interrupt, 3, 2);
		} catch (Exception ex) {
			LogException(ex);
		}
	}

	private void PressButton(Interrupt interrupt, int x, int y) {
		Select(interrupt, x, y);
		interrupt.SendInputs(Button.A);
	}
}
