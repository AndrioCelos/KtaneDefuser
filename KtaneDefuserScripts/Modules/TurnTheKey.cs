using KtaneDefuserConnector.DataTypes;

namespace KtaneDefuserScripts.Modules;
[AimlInterface("TurnTheKey")]
internal class TurnTheKey() : ModuleScript<KtaneDefuserConnector.Components.TurnTheKey>(2, 1) {
	private static bool _turningKey;

	public override string IndefiniteDescription => "Turn the Key";
	public override PriorityCategory PriorityCategory => PriorityCategory.TimeDependent;

	private TimeSpan _time;
	private CancellationTokenSource? _cancellationTokenSource;

	protected internal override void Initialise(Interrupt interrupt) {
		GameState.Current.TimerUpdated += OnTimerUpdated;
		using var ss = DefuserConnector.Instance.TakeScreenshot();
		var data = DefuserConnector.Instance.ReadComponent(ss, DefuserConnector.Instance.GetLightsState(ss), Reader, Utils.GetPoints(GameState.Current.Modules[ModuleIndex].Slot));
		_time = data.Time;
		interrupt.Context.Reply($"Time is {_time.Minutes} {(_time.Minutes == 1 ? "minute" : "minutes")} {_time.Seconds} {(_time.Seconds == 1 ? "second" : "seconds")}.");
	}

	private async void OnTimerUpdated(object? sender, EventArgs e) {
		if (_turningKey) return;
		_cancellationTokenSource?.Cancel();
		var timerRate = GameState.Current.GameMode is GameMode.Steady or GameMode.Time
			? 1
			: GameState.Current.Strikes switch { 0 => 1, 1 => 1.25, 2 => 1.50, 3 => 1.75, _ => 2 };
		var realTime = GameState.Current.GameMode is GameMode.Zen or GameMode.Training
			? (_time - GameState.Current.Time) / timerRate
			: (GameState.Current.Time - _time) / timerRate;
		if (realTime < TimeSpan.Zero) return;  // Uh oh

		_cancellationTokenSource = new();
		if (realTime > TimeSpan.FromSeconds(10)) {
			var token = _cancellationTokenSource.Token;
			await Delay(realTime - TimeSpan.FromSeconds(10));
			if (token.IsCancellationRequested) return;
		}

		using var interrupt = await Interrupt.EnterAsync(AimlAsyncContext.Current);
		interrupt.Context.Reply("Turning the key.");
		await Utils.SelectModuleAsync(interrupt, ModuleIndex, true);
		_turningKey = true;
		do {
			var ss = DefuserConnector.Instance.TakeScreenshot();
			await TimerUtil.ReadTimerAsync(ss, false);
		} while ((int) (GameState.Current.TimerBaseTime.TotalSeconds + 0.1) != (int) _time.TotalSeconds);
		await interrupt.SubmitAsync();
		_turningKey = false;
	}
}
