using System.Text;

namespace KtaneDefuserScripts.Modules;
[AimlInterface("EmojiMath")]
internal class EmojiMath : ModuleScript<KtaneDefuserConnector.Components.EmojiMath> {
	public override string IndefiniteDescription => "Emoji Math";

	private int highlightX;
	private int highlightY;

	protected internal override void Started(AimlAsyncContext context) => context.AddReply("ready");

	[AimlCategory("read")]
	internal static Task Read(AimlAsyncContext context) => GameState.Current.CurrentScript<EmojiMath>().ReadAsync(context);

	private async Task ReadAsync(AimlAsyncContext context) {
		using var interrupt = await CurrentModuleInterruptAsync(context);
		var data = interrupt.Read(Reader);
		if (data.Selection is { } selection) (highlightX, highlightY) = selection;
		var builder = new StringBuilder();
		builder.Append("<speak>");
		var n = 0;
		foreach (var c in data.Display) {
			var alias = c switch {
				':' => "colon",
				'=' => "equal",
				'(' => "left",
				'|' => "bar",
				')' => "right",
				'+' => "plus",
				'-' => "minus",
				_ => "unknown"
			};

			builder.Append($"<sub alias='{alias}'>{c}</sub>");
			n++;
			if (n >= 2 || c is '+' or '-') {
				builder.Append("<break strength='weak'/><![CDATA[ ]]>");
				n = 0;
			}
		}
		builder.Append($"</speak>");
		context.Reply(builder.ToString());
	}

	[AimlCategory("<set>number</set>")]
	internal static Task SubmitPlus1(AimlAsyncContext context, string? d0) => GameState.Current.CurrentScript<EmojiMath>().SubmitAnswer(context, int.Parse($"{d0}"));
	[AimlCategory("<set>number</set> <set>number</set>")]
	internal static Task SubmitPlus2(AimlAsyncContext context, string? d0, string? d1) => GameState.Current.CurrentScript<EmojiMath>().SubmitAnswer(context, int.Parse($"{d0}{d1}"));
	[AimlCategory("<set>number</set> <set>number</set> <set>number</set>")]
	internal static Task SubmitPlus3(AimlAsyncContext context, string? d0, string? d1, string? d2) => GameState.Current.CurrentScript<EmojiMath>().SubmitAnswer(context, int.Parse($"{d0}{d1}{d2}"));
	[AimlCategory("minus <set>number</set>")]
	internal static Task SubmitMinus1(AimlAsyncContext context, string? d0) => GameState.Current.CurrentScript<EmojiMath>().SubmitAnswer(context, int.Parse($"-{d0}"));
	[AimlCategory("minus <set>number</set> <set>number</set>")]
	internal static Task SubmitMinus2(AimlAsyncContext context, string? d0, string? d1) => GameState.Current.CurrentScript<EmojiMath>().SubmitAnswer(context, int.Parse($"-{d0}{d1}"));
	[AimlCategory("minus <set>number</set> <set>number</set> <set>number</set>")]
	internal static Task SubmitMinus3(AimlAsyncContext context, string? d0, string? d1, string? d2) => GameState.Current.CurrentScript<EmojiMath>().SubmitAnswer(context, int.Parse($"-{d0}{d1}{d2}"));

	private async Task SubmitAnswer(AimlAsyncContext context, int answer) {
		using var interrupt = await CurrentModuleInterruptAsync(context);
		foreach (var c in Math.Abs(answer).ToString()) {
			var (x, y) = c == 0 ? (3, 0) : ((c - '1') % 3, (c - '1') / 3);
			await PressButtonAsync(interrupt, x, y, false);
		}
		if (answer < 0) await PressButtonAsync(interrupt, 3, 1, false);
		await PressButtonAsync(interrupt, 3, 2, true);
	}

	private async Task PressButtonAsync(Interrupt interrupt, int x, int y, bool submit) {
		var buttons = new List<Button>();
		for (; highlightX < x; highlightX++) buttons.Add(Button.Right);
		for (; highlightX > x; highlightX--) buttons.Add(Button.Left);
		for (; highlightY < y; highlightY++) buttons.Add(Button.Down);
		for (; highlightY > y; highlightY--) buttons.Add(Button.Up);
		buttons.Add(Button.A);
		if (submit)
			await interrupt.SubmitAsync(buttons);
		else
			await interrupt.SendInputsAsync(buttons);
	}
}
