namespace BombDefuserScripts.Modules;

[AimlInterface("Button")]
internal class Button : ModuleScript<BombDefuserConnector.Components.Button> {
	public override string IndefiniteDescription => "a Button";
	private static Interrupt? interrupt;

	[AimlCategory("read")]
	internal static async Task Read(AimlAsyncContext context) {
		var data = await ReadCurrentAsync(GetProcessor());
		if (data.IndicatorColour != null)
			context.Reply($"The light is {data.IndicatorColour}.");
		else
			context.Reply($"The button is {data.Colour} and reads '{data.Label}'.");
	}

	[AimlCategory("tap")]
	internal static async Task Tap(AimlAsyncContext context) => await Interrupt.SubmitAsync(context, "a");

	[AimlCategory("hold")]
	internal static async Task Hold(AimlAsyncContext context) {
		interrupt = await Interrupt.EnterAsync(context);
		interrupt.Context.SendInputs("a:hold");
		await AimlTasks.Delay(1);
		await Read(interrupt.Context);
	}

	[AimlCategory("release on <set>number</set>")]
	internal static async Task Release(AimlAsyncContext context, int digit) {
		if (interrupt == null) {
			context.Reply("I cannot do that now.");
			return;
		}
		await Timer.WaitForDigitInTimerAsync(digit);
		await interrupt.SubmitAsync("a:release");
		interrupt.Exit();
		interrupt = null;
	}
}
