using System.Diagnostics;

namespace BombDefuserScripts.Modules;

[AimlInterface("NeedyCapacitor")]
internal class NeedyCapacitor : ModuleScript<BombDefuserConnector.Components.NeedyCapacitor> {
	public override string IndefiniteDescription => "a Needy Capacitor";
	public override PriorityCategory PriorityCategory => PriorityCategory.Needy;

	protected internal override async void NeedyStateChanged(AimlAsyncContext context, NeedyState newState) {
		if (newState != NeedyState.Running) return;
		var stopwatch = Stopwatch.StartNew();
		while (this.NeedyState == NeedyState.Running) {
			await AimlTasks.Delay(25);
			using var interrupt = await this.ModuleInterruptAsync(context, false);
			context = interrupt.Context;
			context.Reply("Discharging the capacitor.");
			interrupt.SendInputs("a:hold");
			var elapsed = stopwatch.Elapsed;
			await AimlTasks.Delay(TimeSpan.FromTicks(elapsed.Ticks / 5 + TimeSpan.TicksPerSecond / 2));
			interrupt.SendInputs("a:release");
			stopwatch.Restart();
		}
	}
}
