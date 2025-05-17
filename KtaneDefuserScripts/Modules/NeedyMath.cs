namespace KtaneDefuserScripts.Modules;
[AimlInterface("NeedyMath")]
internal class NeedyMath : ModuleScript<KtaneDefuserConnector.Components.NeedyMath> {
	public override string IndefiniteDescription => "Needy Math";
	public override PriorityCategory PriorityCategory => PriorityCategory.Needy;

	private int _highlightX;
	private int _highlightY;

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
				await PressButtonAsync(interrupt, x, y);
			}

			if (answer < 0) await PressButtonAsync(interrupt, 3, 1);
			await PressButtonAsync(interrupt, 3, 2);
		} catch (Exception ex) {
			LogException(ex);
		}
	}

	private async Task PressButtonAsync(Interrupt interrupt, int x, int y) {
		var buttons = new List<Button>();
		for (; _highlightX < x; _highlightX++) buttons.Add(Button.Right);
		for (; _highlightX > x; _highlightX--) buttons.Add(Button.Left);
		for (; _highlightY < y; _highlightY++) buttons.Add(Button.Down);
		for (; _highlightY > y; _highlightY--) buttons.Add(Button.Up);
		buttons.Add(Button.A);
		await interrupt.SendInputsAsync(buttons);
	}
}
