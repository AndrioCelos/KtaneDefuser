using KtaneDefuserConnector.DataTypes;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace KtaneDefuserScripts;
internal static class Timer {
	internal static async Task ReadTimerAsync(Image<Rgba32> screenshot, bool timerHasNotStartedYet) {
		if (GameState.Current.TimerSlot is null) throw new InvalidOperationException("Don't know where the timer is.");
		var polygon = Utils.GetPoints(GameState.Current.TimerSlot.Value);
		int? lastSeconds = null;
		// Keep watching the timer until it ticks over to get sub-second precision.
		while (true) {
			var data = DefuserConnector.Instance.ReadComponent(screenshot, DefuserConnector.Instance.GetLightsState(screenshot), DefuserConnector.TimerReader, polygon);
			screenshot.Dispose();
			GameState.Current.GameMode = data.GameMode;
			if (timerHasNotStartedYet) {  // TimerBaseTime is now the time as of 2 seconds before the timer starts.
				GameState.Current.TimerBaseTime = data.GameMode is GameMode.Zen or GameMode.Training
					? TimeSpan.FromSeconds(data.Time - 2)
					: TimeSpan.FromSeconds(data.Time + 2);
				return;
			}
			if (lastSeconds is not null && data.Time != lastSeconds.Value) {
				GameState.Current.TimerBaseTime = data.GameMode is GameMode.Zen or GameMode.Training
					? TimeSpan.FromSeconds(data.Time) - TimeSpan.FromMilliseconds(50)
					: TimeSpan.FromSeconds(data.Time + 1) + TimeSpan.FromMilliseconds(50);
				GameState.Current.TimerStopwatch.Restart();
				return;
			}
			lastSeconds = data.Time;
			await Delay(0.1);
			screenshot = DefuserConnector.Instance.TakeScreenshot();
		}
	}

	/// <summary>Returns a <see cref="Task"/> that completes when the specified digit is displayed on the bomb timer.</summary>
	public static async Task WaitForDigitInTimerAsync(int digit) {
		var time = GameState.Current.Time;
		if (time.Seconds % 10 == digit || time.Seconds / 10 == digit) return;
		if (time.Minutes > 0 ? (time.Minutes % 10 == digit || time.Minutes / 10 == digit) : digit == 0) return;

		// Find out how long to wait for.
		var timeSpan = GameState.Current.GameMode is GameMode.Zen or GameMode.Training
			? TimeSpan.FromTicks(digit * TimeSpan.TicksPerSecond + TimeSpan.TicksPerSecond / 2 - time.Ticks % (TimeSpan.TicksPerSecond * 10))
			: TimeSpan.FromTicks(time.Ticks % (TimeSpan.TicksPerSecond * 10) - digit * TimeSpan.TicksPerSecond - TimeSpan.TicksPerSecond / 2);
		if (timeSpan < TimeSpan.Zero) timeSpan += TimeSpan.FromSeconds(10);
		await Delay(timeSpan);
	}
}
