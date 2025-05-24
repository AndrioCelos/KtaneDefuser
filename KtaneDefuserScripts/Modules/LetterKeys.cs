namespace KtaneDefuserScripts.Modules;
[AimlInterface("LetterKeys")]
internal class LetterKeys() : ModuleScript<KtaneDefuserConnector.Components.LetterKeys>(2, 2) {
	public override string IndefiniteDescription => "Letter Keys";

	private bool _readyToRead;
	private char[]? _labels;

	protected internal override void Started(AimlAsyncContext context) {
		_readyToRead = true;
	}

	protected internal override void ModuleSelected(Interrupt interrupt) {
		if (!_readyToRead) return;
		_readyToRead = false;
		Read(interrupt);
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
		if (data.Selection is { } selection) Selection = selection;
		interrupt.Context.Reply(data.Display.ToString());
	}

	[AimlCategory("<set>NATO</set>"), AimlCategory("press <set>NATO</set>")]
	internal static Task Read(AimlAsyncContext context, string letter)
		=> GameState.Current.CurrentScript<LetterKeys>().PressButtonAsync(context, Nato.DecodeChar(letter));


	private async Task PressButtonAsync(AimlAsyncContext context, char letter) {
		if (_labels is null) throw new InvalidOperationException("Must read the module first.");
		var index = Array.IndexOf(_labels, letter);
		using var interrupt = await CurrentModuleInterruptAsync(context);
		await InteractWaitAsync(interrupt, index % 2, index / 2);
		await interrupt.CheckStatusAsync();
	}
}
