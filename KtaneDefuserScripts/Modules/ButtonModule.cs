using Button = KtaneDefuserConnector.Components.Button;

namespace KtaneDefuserScripts.Modules;

[AimlInterface("Button")]
internal class ButtonModule : ModuleScript<Button> {
	public override string IndefiniteDescription => "a Button";
	private static Interrupt? interrupt;

	protected internal override void Started(AimlAsyncContext context) => context.AddReply("ready");

	[AimlCategory("read")]
	internal static async Task Read(AimlAsyncContext context) {
		using var interrupt = await CurrentModuleInterruptAsync(context);
		var data = interrupt.Read(Reader);
		if (data.IndicatorColour != null)
			interrupt.Context.Reply($"The light is {data.IndicatorColour}.<reply>release on …</reply>");
		else
			interrupt.Context.Reply($"The button is {data.Colour} and reads '{data.Label}'.<reply>tap</reply><reply>hold</reply>");
	}

	[AimlCategory("tap")]
	internal static async Task Tap(AimlAsyncContext context) {
		using var interrupt = await CurrentModuleInterruptAsync(context);
		await interrupt.SubmitAsync(KtaneDefuserConnectorApi.Button.A);
	}

	[AimlCategory("hold")]
	internal static async Task Hold(AimlAsyncContext context) {
		interrupt = await CurrentModuleInterruptAsync(context);
		interrupt.SendInputs(new ButtonAction(KtaneDefuserConnectorApi.Button.A, ButtonActionType.Hold));
		await Delay(1);
		var data = interrupt.Read(Reader);
		interrupt.Context.Reply($"The light is {data.IndicatorColour}.<reply>release on …</reply>");
	}

	[AimlCategory("release on <set>number</set>")]
	internal static async Task Release(AimlAsyncContext context, int digit) {
		if (interrupt == null) {
			context.Reply("I cannot do that now.");
			return;
		}
		interrupt.Context = context;
		await Timer.WaitForDigitInTimerAsync(digit);
		await interrupt.SubmitAsync(new ButtonAction(KtaneDefuserConnectorApi.Button.A, ButtonActionType.Release));
		interrupt.Exit();
		interrupt = null;
	}

	[AimlCategory("release on …")]
	internal static void ReleaseMenu(AimlAsyncContext context) {
		for (var i = 0; i < 10; i++) {
			context.Reply($"<reply><text>{i}</text><postback>release on {i}</postback></reply>");
		}
	}
}
