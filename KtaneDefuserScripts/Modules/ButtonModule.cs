using JetBrains.Annotations;
using Button = KtaneDefuserConnector.Components.Button;

namespace KtaneDefuserScripts.Modules;

[AimlInterface("Button")]
internal class ButtonModule : ModuleScript<Button> {
	public override string IndefiniteDescription => "a Button";
	private static Interrupt? _holdInterrupt;

	protected internal override void Started(AimlAsyncContext context) => context.AddReply("ready");

	[AimlCategory("read"), UsedImplicitly]
	internal static async Task Read(AimlAsyncContext context) {
		using var interrupt = await CurrentModuleInterruptAsync(context);
		var data = interrupt.Read(Reader);
		interrupt.Context.Reply(data.IndicatorColour is not null
			? $"The light is {data.IndicatorColour}.<reply>release on …</reply>"
			: $"The button is {data.Colour} and reads '{data.Label}'.<reply>tap</reply><reply>hold</reply>");
	}

	[AimlCategory("tap")]
	internal static async Task Tap(AimlAsyncContext context) {
		using var interrupt = await CurrentModuleInterruptAsync(context);
		await interrupt.SubmitAsync(KtaneDefuserConnectorApi.Button.A);
	}

	[AimlCategory("hold")]
	internal static async Task Hold(AimlAsyncContext context) {
		_holdInterrupt = await CurrentModuleInterruptAsync(context);
		_holdInterrupt.SendInputs(new ButtonAction(KtaneDefuserConnectorApi.Button.A, ButtonActionType.Hold));
		await Delay(1);
		var data = _holdInterrupt.Read(Reader);
		_holdInterrupt.Context.Reply($"The light is {data.IndicatorColour}.<reply>release on …</reply>");
	}

	[AimlCategory("release on <set>number</set>")]
	internal static async Task Release(AimlAsyncContext context, int digit) {
		if (_holdInterrupt == null) {
			context.Reply("I cannot do that now.");
			return;
		}
		_holdInterrupt.Context = context;
		await TimerUtil.WaitForDigitInTimerAsync(digit);
		await _holdInterrupt.SubmitAsync(new ButtonAction(KtaneDefuserConnectorApi.Button.A, ButtonActionType.Release));
		_holdInterrupt.ExitAsync();
		_holdInterrupt = null;
	}

	[AimlCategory("release on …")]
	internal static void ReleaseMenu(AimlAsyncContext context) {
		for (var i = 0; i < 10; i++) {
			context.Reply($"<reply><text>{i}</text><postback>release on {i}</postback></reply>");
		}
	}
}
