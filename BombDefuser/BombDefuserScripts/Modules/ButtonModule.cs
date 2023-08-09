namespace BombDefuserScripts.Modules;

[AimlInterface("Button")]
internal class ButtonModule : ModuleScript<BombDefuserConnector.Components.Button> {
	public override string IndefiniteDescription => "a Button";
	private static Interrupt? interrupt;

	[AimlCategory("read")]
	internal static async Task Read(AimlAsyncContext context) {
		using var interrupt = await CurrentModuleInterruptAsync(context);
		var data = interrupt.Read(Reader);
		if (data.IndicatorColour != null)
			interrupt.Context.Reply($"The light is {data.IndicatorColour}.");
		else
			interrupt.Context.Reply($"The button is {data.Colour} and reads '{data.Label}'.");
	}

	[AimlCategory("tap")]
	internal static async Task Tap(AimlAsyncContext context) {
		using var interrupt = await CurrentModuleInterruptAsync(context);
		await interrupt.SubmitAsync(Button.A);
	}

	[AimlCategory("hold")]
	internal static async Task Hold(AimlAsyncContext context) {
		interrupt = await CurrentModuleInterruptAsync(context);
		interrupt.SendInputs(new ButtonAction(Button.A, ButtonActionType.Hold));
		await Delay(1);
		var data = interrupt.Read(Reader);
		interrupt.Context.Reply($"The light is {data.IndicatorColour}.");
	}

	[AimlCategory("release on <set>number</set>")]
	internal static async Task Release(AimlAsyncContext context, int digit) {
		if (interrupt == null) {
			context.Reply("I cannot do that now.");
			return;
		}
		interrupt.Context = context;
		await Timer.WaitForDigitInTimerAsync(digit);
		await interrupt.SubmitAsync(new ButtonAction(Button.A, ButtonActionType.Release));
		interrupt.Exit();
		interrupt = null;
	}
}
