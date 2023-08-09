namespace BombDefuserScripts;
internal static class AimlTasks {
	[AimlResponse("OOB Tick *")]
	private static readonly AimlTaskFactory timer = new("<oob><timer duration='{0}'><postback>{ID}</postback></timer></oob>");
	[AimlResponse("OOB DefuserCallback *"), Obsolete("String inputs are being replaced with IInputAction")]
	private static readonly AimlTaskFactory sendInputs = new("<oob><sendinputs>{0} callback:{ID}</sendinputs></oob>");
	[AimlResponse("OOB DefuserCallback *")]
	private static readonly AimlTaskFactory inputCallback = new(null);

	/// <summary>Creates an <see cref="AimlTask"/> that completes after the specified duration.</summary>
	public static AimlTask Delay(TimeSpan timeSpan) => timer.CallAsync(timeSpan.TotalSeconds);
	/// <summary>Creates an <see cref="AimlTask"/> that completes after the specified number of seconds.</summary>
	public static AimlTask Delay(object seconds) => timer.CallAsync(seconds);
	[Obsolete("String inputs are being replaced with IInputAction")]
	public static AimlTask SendInputsAsync(this Interrupt interrupt, string inputs) => sendInputs.CallAsync(inputs);
	[Obsolete("String inputs are being replaced with IInputAction")]
	public static void SendInputs(this Interrupt interrupt, string inputs) => interrupt.Context.Reply($"<oob><sendinputs>{inputs}</sendinputs></oob>");

	/// <summary>Presses the specified buttons in sequence.</summary>
	public static void SendInputs(this Interrupt interrupt, params Button[] buttons) => SendInputs(interrupt, from b in buttons select new ButtonAction(b));
	/// <summary>Presses the specified buttons in sequence.</summary>
	public static void SendInputs(this Interrupt interrupt, IEnumerable<Button> buttons) => SendInputs(interrupt, from b in buttons select new ButtonAction(b));
	/// <summary>Performs the specified input actions in sequence.</summary>
	public static void SendInputs(this Interrupt interrupt, params IInputAction[] actions) => SendInputs(interrupt, (IEnumerable<IInputAction>) actions);
	/// <summary>Performs the specified input actions in sequence.</summary>
	public static void SendInputs(this Interrupt interrupt, IEnumerable<IInputAction> actions) {
		if (interrupt.IsDisposed) throw new ObjectDisposedException(nameof(Interrupt));
		DefuserConnector.Instance.SendInputs(actions);
	}
	/// <summary>Creates an <see cref="AimlTask"/> that presses the specified buttons in sequence and completes after this is complete.</summary>
	public static AimlTask SendInputsAsync(this Interrupt interrupt, params Button[] buttons) => SendInputsAsync(interrupt, from b in buttons select new ButtonAction(b));
	/// <summary>Creates an <see cref="AimlTask"/> that presses the specified buttons in sequence and completes after this is complete.</summary>
	public static AimlTask SendInputsAsync(this Interrupt interrupt, IEnumerable<Button> buttons) => SendInputsAsync(interrupt, from b in buttons select new ButtonAction(b));
	/// <summary>Creates an <see cref="AimlTask"/> that performs the specified input actions in sequence and completes after this is complete.</summary>
	public static AimlTask SendInputsAsync(this Interrupt interrupt, params IInputAction[] actions) => SendInputsAsync(interrupt, (IEnumerable<IInputAction>) actions);
	/// <summary>Creates an <see cref="AimlTask"/> that performs the specified input actions in sequence and completes after this is complete.</summary>
	public static AimlTask SendInputsAsync(this Interrupt interrupt, IEnumerable<IInputAction> actions) {
		if (interrupt.IsDisposed) throw new ObjectDisposedException(nameof(Interrupt));
		var guid = Guid.NewGuid();
		var task = inputCallback.CallAsync(interrupt.Context, guid);
		DefuserConnector.Instance.SendInputs(actions.Concat(new[] { new CallbackAction(guid) }));
		return task;
	}
}
