using System.ComponentModel;

namespace BombDefuserScripts;
[AimlInterface]
internal class AlarmClock {
	[AimlCategory("OOB DefuserSocketMessage AlarmClock *"), EditorBrowsable(EditorBrowsableState.Never)]
	public static async Task AlarmClockInterruptAsync(AimlAsyncContext context, bool on) {
		if (!on) return;
		using var interrupt = await Interrupt.EnterAsync(context);
		await interrupt.SendInputsAsync(GameState.Current.FocusState switch { FocusState.Bomb => "b left a a b right a", FocusState.Module => "b b left a a b right a a", _ => throw new InvalidOperationException($"Don't know how to deal with the alarm clock from state {GameState.Current.FocusState}.") });
		await AimlTasks.Delay(1.5);
	}
}
