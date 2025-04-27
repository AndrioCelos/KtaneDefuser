using JetBrains.Annotations;

namespace KtaneDefuserScripts;
/// <summary>Represents a context for performing a task that cannot be interrupted by other bot actions.</summary>
/// <remarks>
/// All inputs and all reading of a focused module require an interrupt. To perform one of these actions when another interrupt is in progress, it is necessary to wait for that interrupt to end first.
/// This prevents issues such as desyncs and attempting to do things while holding a button.
/// </remarks>
[MustDisposeResource]
public class Interrupt : IDisposable {
	[Obsolete("String inputs are being replaced with IInputAction")]
	private static readonly AimlTaskFactory sendInputs = new("<oob><sendinputs>{0} callback:{ID}</sendinputs></oob>");
	[AimlResponse("OOB DefuserCallback *")]
	private static readonly AimlTaskFactory inputCallback = new(null);

	public static bool EnableInterrupts { get; private set; } = true;
	private static readonly Queue<TaskCompletionSource<Interrupt>> InterruptQueue = new();

	public bool IsDisposed { get; private set; }

	/// <summary>
	/// Returns or sets the current <see cref="AimlAsyncContext"/> of this interrupt. When initialised, this will be the context in which the interrupt was entered.
	/// It may be set in scripts like The Button which handle user input during an interrupt, to continue it in a new <see cref="AimlAsyncContext"/>.
	/// </summary>
	public AimlAsyncContext Context { get; set; }

	private CancellationTokenSource? submitCancellationTokenSource;
	private Slot submitCurrentSlot;

	private Interrupt(AimlAsyncContext context) => Context = context ?? throw new ArgumentNullException(nameof(context));

	/// <summary>Attempts to enter a new interrupt on the interrupt queue and creates a <see cref="Task"/> that completes when the interrupt is entered.</summary>
	/// <remarks>This will return an already-completed task if the interrupt queue was empty. <paramref name="context"/> is no longer valid after calling this method; the created <see cref="Interrupt"/> instance's <see cref="Context"/> must be used instead.</remarks>
	[MustDisposeResource]
	public static Task<Interrupt> EnterAsync(AimlAsyncContext context) {
		lock (InterruptQueue) {
			if (EnableInterrupts) {
				EnableInterrupts = false;
				return Task.FromResult(new Interrupt(context));
			}
			var taskSource = new TaskCompletionSource<Interrupt>();
			InterruptQueue.Enqueue(taskSource);
			return taskSource.Task;
		}
	}

	private static async Task ExitAsync(AimlAsyncContext context) {
		TaskCompletionSource<Interrupt>? queuedTaskSource;
		lock (InterruptQueue) {
			InterruptQueue.TryDequeue(out queuedTaskSource);
		}
		if (queuedTaskSource is not null) {
			queuedTaskSource.SetResult(new(context));
		} else {
			if (GameState.Current.CurrentModuleNum != GameState.Current.SelectedModuleNum && GameState.Current.CurrentModuleNum is not null) {
				// Re-select the module that was interrupted, or the new current module if it changed.
				var interrupt = new Interrupt(context);
				await Utils.SelectModuleAsync(interrupt, GameState.Current.CurrentModuleNum!.Value, true);
				// Perhaps another interrupt occurred during this.
				lock (InterruptQueue) {
					InterruptQueue.TryDequeue(out queuedTaskSource);
				}
				if (queuedTaskSource is not null)
					queuedTaskSource.SetResult(interrupt);
				else {
					EnableInterrupts = true;
					GameState.Current.CurrentModule!.Script.ModuleSelected(context);
				}
			} else
				EnableInterrupts = true;
		}
	}

	/// <summary>Reads data from the currently-focused module using the specified <see cref="ComponentReader"/.></summary>
	public T Read<T>(ComponentReader<T> reader) where T : notnull {
		ObjectDisposedException.ThrowIf(IsDisposed, this);
		using var ss = DefuserConnector.Instance.TakeScreenshot();
		return DefuserConnector.Instance.ReadComponent(ss, DefuserConnector.Instance.GetLightsState(ss), reader, Utils.CurrentModuleArea);
	}

	[Obsolete("String inputs are being replaced with IInputAction")]
	public AimlTask SendInputsAsync(string inputs) => sendInputs.CallAsync(inputs);
	[Obsolete("String inputs are being replaced with IInputAction")]
	public void SendInputs(string inputs) => Context.Reply($"<oob><sendinputs>{inputs}</sendinputs></oob>");

	/// <summary>Presses the specified buttons in sequence.</summary>
	public void SendInputs(params Button[] buttons) => SendInputs(from b in buttons select new ButtonAction(b));
	/// <summary>Presses the specified buttons in sequence.</summary>
	public void SendInputs(IEnumerable<Button> buttons) => SendInputs(from b in buttons select new ButtonAction(b));
	/// <summary>Performs the specified input actions in sequence.</summary>
	public void SendInputs(params IInputAction[] actions) => SendInputs((IEnumerable<IInputAction>) actions);
	/// <summary>Performs the specified input actions in sequence.</summary>
	public void SendInputs(IEnumerable<IInputAction> actions) {
		ObjectDisposedException.ThrowIf(IsDisposed, this);
		DefuserConnector.Instance.SendInputs(actions);
	}
	/// <summary>Creates an <see cref="AimlTask"/> that presses the specified buttons in sequence and completes after this is complete.</summary>
	public AimlTask SendInputsAsync(params IEnumerable<Button> buttons) => SendInputsAsync(from b in buttons select new ButtonAction(b), CancellationToken.None);
	/// <summary>Creates an <see cref="AimlTask"/> that presses the specified buttons in sequence and completes after this is complete.</summary>
	public AimlTask SendInputsAsync(IEnumerable<Button> buttons, CancellationToken cancellationToken) => SendInputsAsync(from b in buttons select new ButtonAction(b), cancellationToken);
	/// <summary>Creates an <see cref="AimlTask"/> that performs the specified input actions in sequence and completes after this is complete.</summary>
	public AimlTask SendInputsAsync(params IEnumerable<IInputAction> actions) => SendInputsAsync(actions, CancellationToken.None);
	/// <summary>Creates an <see cref="AimlTask"/> that performs the specified input actions in sequence and completes after this is complete.</summary>
	public AimlTask SendInputsAsync(IEnumerable<IInputAction> actions, CancellationToken cancellationToken) {
		ObjectDisposedException.ThrowIf(IsDisposed, this);
		cancellationToken.Register(DefuserConnector.Instance.CancelInputs);
		var guid = Guid.NewGuid();
		var task = inputCallback.CallAsync(Context, guid, cancellationToken);
		DefuserConnector.Instance.SendInputs(actions.Concat([new CallbackAction(guid)]));
		return task;
	}

	/// <summary>Performs the specified input actions, announces a resulting solve, and returns the resulting module light state afterward. If a strike occurs, it interrupts the sequence.</summary>
	public Task<ModuleLightState> SubmitAsync(params IEnumerable<Button> buttons) => SubmitAsync(from b in buttons select new ButtonAction(b));
	/// <summary>Performs the specified input actions, announces a resulting solve, and returns the resulting module light state afterward. If a strike occurs, it interrupts the sequence.</summary>
	public async Task<ModuleLightState> SubmitAsync(params IEnumerable<IInputAction> actions) {
		ObjectDisposedException.ThrowIf(IsDisposed, this);
		if (submitCancellationTokenSource is not null) throw new InvalidOperationException("Already submitting.");
		if (GameState.Current.CurrentModuleNum is null) throw new InvalidOperationException("No current module.");
		var module = GameState.Current.Modules[GameState.Current.CurrentModuleNum.Value];
		var context = AimlAsyncContext.Current ?? throw new InvalidOperationException("No current request");

		submitCancellationTokenSource = new();
		submitCurrentSlot = module.Slot;
		GameState.Current.Strike += GameState_Strike;
		try {
			await SendInputsAsync(actions, submitCancellationTokenSource.Token);
			await Delay(0.5);  // Wait for the interaction punch to end.
			using var ss = DefuserConnector.Instance.TakeScreenshot();
			var result = DefuserConnector.Instance.GetModuleLightState(ss, Utils.CurrentModuleArea);
			if (result == ModuleLightState.Solved) {
				var isDefused = GameState.Current.TryMarkModuleSolved(context, GameState.Current.CurrentModuleNum.Value);
				if (isDefused)
					context.Reply("The bomb is defused.");
				else {
					context.Reply("<priority/>Module complete<reply>next module</reply>.");
					if (!GameState.Current.NextModuleNums.TryDequeue(out var nextModule))
						nextModule = GameState.Current.Modules.FindIndex(GameState.Current.SelectedModuleNum is { } i ? i + 1 : 0, m => m.Script.PriorityCategory != PriorityCategory.Needy && !m.IsSolved);
					if (nextModule >= 0) {
						context.Reply($"Next is {GameState.Current.Modules[nextModule].Script.IndefiniteDescription}.");
						await ModuleSelection.ChangeModuleAsync(context, nextModule, true);
					}
				}
			}
			return result;
		} catch (TaskCanceledException) {
			return ModuleLightState.Strike;
		} finally {
			submitCancellationTokenSource?.Dispose();
			submitCancellationTokenSource = null;
		}
	}

	private void GameState_Strike(object? sender, StrikeEventArgs e) {
		if (e.Slot == submitCurrentSlot) {
			Context = e.Context;
			submitCancellationTokenSource?.Cancel();
		}
	}

	/// <summary>Exits this interrupt, allowing the bot to enter another interrupt. After calling this method, this instance is no longer usable.</summary>
	public void ExitAsync() => Dispose();

	/// <summary>Exits this interrupt, allowing the bot to enter another interrupt. After calling this method, this instance is no longer usable.</summary>
	public void Dispose() {
		if (IsDisposed) return;
		IsDisposed = true;
		_ = ExitAsync(AimlAsyncContext.Current ?? throw new InvalidOperationException("No current request"));
		GC.SuppressFinalize(this);
	}
}
