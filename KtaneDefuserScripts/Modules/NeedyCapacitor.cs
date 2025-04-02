using System.Diagnostics;
using KtaneDefuserConnectorApi;

namespace KtaneDefuserScripts.Modules;

[AimlInterface("NeedyCapacitor")]
internal class NeedyCapacitor : ModuleScript<KtaneDefuserConnector.Components.NeedyCapacitor> {
	public override string IndefiniteDescription => "a Needy Capacitor";
	public override PriorityCategory PriorityCategory => PriorityCategory.Needy;

	protected internal override async void NeedyStateChanged(AimlAsyncContext context, NeedyState newState) {
		if (newState != NeedyState.Running) return;
		var stopwatch = Stopwatch.StartNew();
		while (this.NeedyState == NeedyState.Running) {
			await Delay(25);
			using var interrupt = await this.ModuleInterruptAsync(context, false);
			context = interrupt.Context;
			context.Reply("Discharging the capacitor.");
			interrupt.SendInputs(new ButtonAction(Button.A, ButtonActionType.Hold));
			var elapsed = stopwatch.Elapsed;
			await Delay(TimeSpan.FromTicks(elapsed.Ticks / 5 + TimeSpan.TicksPerSecond / 2));
			interrupt.SendInputs(new ButtonAction(Button.A, ButtonActionType.Release));
			stopwatch.Restart();
		}
	}
}
