using Tensorflow.Contexts;

namespace KtaneDefuserScripts.Modules;
[AimlInterface("LetterKeys")]
internal partial class LetterKeys : ModuleScript<KtaneDefuserConnector.Components.LetterKeys> {
	public override string IndefiniteDescription => "Letter Keys";

	private bool _readyToRead;
	private char[]? _labels;
	private int _highlightX;
	private int _highlightY;

	protected internal override void Started(AimlAsyncContext context) {
		_readyToRead = true;
	}

	protected internal override void ModuleSelected(Interrupt interrupt) {
		if (_readyToRead) {
			_readyToRead = false;
			Read(interrupt);
		}
	}

	[AimlCategory("read")]
	internal static async Task Read(AimlAsyncContext context) {
		using var interrupt = await CurrentModuleInterruptAsync(context);
		var script = GameState.Current.CurrentScript<LetterKeys>();
		script.Read(interrupt);
	}
	private void Read(Interrupt interrupt) {
		var data = interrupt.Read(Reader);
		_labels = data.ButtonLabels;
		if (data.Selection is { } selection) (_highlightX, _highlightY) = selection;
		interrupt.Context.Reply(data.Display.ToString());
	}

	[AimlCategory("<set>NATO</set>"), AimlCategory("press <set>NATO</set>")]
	internal static Task Read(AimlAsyncContext context, string letter)
		=> GameState.Current.CurrentScript<LetterKeys>().PressButtonAsync(context, NATO.DecodeChar(letter));


	private async Task PressButtonAsync(AimlAsyncContext context, char letter) {
		if (_labels is null) throw new InvalidOperationException("Must read the module first.");
		var index = Array.IndexOf(_labels, letter);
		int x = index % 2, y = index / 2;
		
		using var interrupt = await CurrentModuleInterruptAsync(context);
		var buttons = new List<Button>();
		for (; _highlightX < x; _highlightX++) buttons.Add(Button.Right);
		for (; _highlightX > x; _highlightX--) buttons.Add(Button.Left);
		for (; _highlightY < y; _highlightY++) buttons.Add(Button.Down);
		for (; _highlightY > y; _highlightY--) buttons.Add(Button.Up);
		buttons.Add(Button.A);
		await interrupt.SubmitAsync(buttons);
	}
}
