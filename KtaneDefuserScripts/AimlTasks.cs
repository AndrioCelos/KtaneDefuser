namespace KtaneDefuserScripts;
internal static class AimlTasks {
	[AimlResponse("OOB Tick *")]
	private static readonly AimlTaskFactory Timer = new("<oob><timer duration='{0}'><postback>{ID}</postback></timer></oob>");

	/// <summary>Creates an <see cref="AimlTask"/> that completes after the specified duration.</summary>
	public static AimlTask Delay(TimeSpan timeSpan) => Timer.CallAsync(timeSpan.TotalSeconds);
	/// <summary>Creates an <see cref="AimlTask"/> that completes after the specified number of seconds.</summary>
	public static AimlTask Delay(object seconds) => Timer.CallAsync(seconds);
}
