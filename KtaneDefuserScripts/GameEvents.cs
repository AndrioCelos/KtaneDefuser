using System.ComponentModel;
using KtaneDefuserConnector.DataTypes;

namespace KtaneDefuserScripts;
[AimlInterface]
internal class GameEvents {
	[AimlCategory("OOB AlarmClockChange *"), EditorBrowsable(EditorBrowsableState.Never)]
	public static async Task AlarmClockInterruptAsync(AimlAsyncContext context, bool on) {
		if (!on) return;
		using var interrupt = await Interrupt.EnterAsync(context);
		switch (GameState.Current.FocusState) {
			case FocusState.Module: await interrupt.SendInputsAsync(Button.B, Button.B, Button.Left, Button.A, Button.A, Button.B, Button.Right, Button.A, Button.A); break;
			case FocusState.Bomb: await interrupt.SendInputsAsync(Button.B, Button.Left, Button.A, Button.A, Button.B, Button.Right, Button.A); break;
			default: throw new InvalidOperationException($"Don't know how to deal with the alarm clock from state {GameState.Current.FocusState}.");
		}
		await Delay(1.5);
	}

	[AimlCategory("OOB Strike * * * *"), EditorBrowsable(EditorBrowsableState.Never)]
	public static void OnStrike(AimlAsyncContext context, int bomb, int face, int x, int y) {
		if (GameState.Current.GameMode != GameMode.Time)
			GameState.Current.Strikes++;
		var module = GameState.Current.Modules.FirstOrDefault(m => m.Slot.Bomb == bomb && m.Slot.Face == face && m.Slot.X == x && m.Slot.Y == y);
		if (module is not null) {
			context.Reply($"<priority/> Strike from {module.Reader.Name}.");
			module.Script.Strike(context);
		}
		GameState.Current.OnStrike(new(context, new(bomb, face, x, y), module));
	}
}
