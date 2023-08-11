namespace BombDefuserScripts;
internal static class AimlTasks {
	[AimlResponse("OOB Tick *")]
	private static readonly AimlTaskFactory timer = new("<oob><timer duration='{0}'><postback>{ID}</postback></timer></oob>");

	/// <summary>Creates an <see cref="AimlTask"/> that completes after the specified duration.</summary>
	public static AimlTask Delay(TimeSpan timeSpan) => timer.CallAsync(timeSpan.TotalSeconds);
	/// <summary>Creates an <see cref="AimlTask"/> that completes after the specified number of seconds.</summary>
	public static AimlTask Delay(object seconds) => timer.CallAsync(seconds);
}
