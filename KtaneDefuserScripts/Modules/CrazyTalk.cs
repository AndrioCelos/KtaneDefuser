using System.Text;
using System.Text.RegularExpressions;

namespace KtaneDefuserScripts.Modules;
[AimlInterface("CrazyTalk")]
internal partial class CrazyTalk : ModuleScript<KtaneDefuserConnector.Components.CrazyTalk> {
	public override string IndefiniteDescription => "Crazy Talk";

	private bool _switchIsDown;

	protected internal override void Started(AimlAsyncContext context) => context.AddReply("ready");

	[AimlCategory("read")]
	internal static async Task Read(AimlAsyncContext context) {
		var script = GameState.Current.CurrentScript<CrazyTalk>();
		using var interrupt = await script.ModuleInterruptAsync(context);
		var data = interrupt.Read(Reader);
		script._switchIsDown = data.SwitchIsDown;

		if (data.Display.Contains('←')) {
			context.Reply($"Words and arrows: {string.Join(", ", data.Display.Select(c => c switch { '←' => "arrow left", '→' => "arrow right", 'L' => "word left", 'R' => "word right", _ => null}).Where(s => s is not null))}");
		} else if (data.Display.Any(c => c is '1' or '2' or '3' or '4')) {
			var builder = new StringBuilder();
			foreach (var word in data.Display.Split((char[]?) null, StringSplitOptions.RemoveEmptyEntries)) {
				if (char.IsDigit(word[0]))
					builder.Append($"Numeral '{word}', ");
				else
					builder.Append($"Word <speak><say-as interpret-as='spell-out'>{word}</say-as></speak>, ");
			}
			builder.Remove(builder.Length - 2, 2);
			builder.Append('.');
			context.Reply(builder.ToString());
		} else {
			context.Reply($"Quote: {RemovePunctuationRegex().Replace(data.Display, "")}. End quote.");
		}
	}

	[AimlCategory("<set>number</set> <set>number</set>")]
	internal static async Task Submit(AimlAsyncContext context, string downTime, string upTime) {
		var script = GameState.Current.CurrentScript<CrazyTalk>();
		using var interrupt = await script.ModuleInterruptAsync(context);
		var switchIsDown = script._switchIsDown;
		await TimerUtil.WaitForSecondsDigitAsync(int.Parse(switchIsDown ? upTime : downTime));
		var result = await interrupt.SubmitAsync(Button.A);
		if (result != ModuleStatus.Off) return;
		await TimerUtil.WaitForSecondsDigitAsync(int.Parse(switchIsDown ? downTime : upTime));
		await interrupt.SubmitAsync(Button.A);
	}

	[GeneratedRegex("[-,.?]")]
	private static partial Regex RemovePunctuationRegex();
}
