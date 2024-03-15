namespace BombDefuserScripts;
/// <summary>Represents a context for performing a task that cannot be interrupted by other bot actions.</summary>
/// <remarks>
/// All inputs and all reading of a focused module require an interrupt. To perform one of these actions when another interrupt is in progress, it is necessary to wait for that interrupt to end first.
/// This prevents issues such as desyncs and attempting to do things while holding a button.
/// </remarks>
public class Interrupt : IDisposable {
	[AimlResponse("OOB DefuserCallback *"), Obsolete("String inputs are being replaced with IInputAction")]
	private static readonly AimlTaskFactory sendInputs = new("<oob><sendinputs>{0} callback:{ID}</sendinputs></oob>");
	//[AimlResponse("OOB DefuserCallback *")]
	private static readonly AimlTaskFactory inputCallback = new(null);

	public static bool EnableInterrupts { get; set; } = true;
	private static readonly Queue<TaskCompletionSource<Interrupt>> interruptQueue = new();

	public bool IsDisposed { get; private set; }

	/// <summary>
	/// Returns or sets the current <see cref="AimlAsyncContext"/> of this interrupt. When initialised, this will be the context in which the interrupt was entered.
	/// It may be set in scripts like The Button which handle user input during an interrupt, to continue it in a new <see cref="AimlAsyncContext"/>.
	/// </summary>
	public AimlAsyncContext Context { get; set; }

	private CancellationTokenSource? submitCancellationTokenSource;
	private Slot submitCurrentSlot;

	private Interrupt(AimlAsyncContext context) => this.Context = context ?? throw new ArgumentNullException(nameof(context));

	/// <summary>Attempts to enter a new interrupt on the interrupt queue and creates a <see cref="Task"/> that completes when the interrupt is entered.</summary>
	/// <remarks>This will return an already-completed task if the interrupt queue was empty. <paramref name="context"/> is no longer valid after calling this method; the created <see cref="Interrupt"/> instance's <see cref="Context"/> must be used instead.</remarks>
	public static Task<Interrupt> EnterAsync(AimlAsyncContext context) {
		lock (interruptQueue) {
			if (EnableInterrupts) {
				EnableInterrupts = false;
				return Task.FromResult(new Interrupt(context));
			}
			var taskSource = new TaskCompletionSource<Interrupt>();
			interruptQueue.Enqueue(taskSource);
			return taskSource.Task;
		}
	}

	private static async void Exit(AimlAsyncContext context) {
		TaskCompletionSource<Interrupt>? queuedTaskSource;
		lock (interruptQueue) {
			interruptQueue.TryDequeue(out queuedTaskSource);
		}
		if (queuedTaskSource is not null) {
			queuedTaskSource.SetResult(new(context));
		} else {
			if (GameState.Current.CurrentModuleNum != GameState.Current.SelectedModuleNum && GameState.Current.CurrentModuleNum is not null) {
				// Re-select the module that was interrupted, or the new current module if it changed.
				var interrupt = new Interrupt(context);
				await Utils.SelectModuleAsync(interrupt, GameState.Current.CurrentModuleNum!.Value, true);
				// Perhaps another interrupt occurred during this.
				lock (interruptQueue) {
					interruptQueue.TryDequeue(out queuedTaskSource);
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
		if (this.IsDisposed) throw new ObjectDisposedException(nameof(Interrupt));
		using var ss = DefuserConnector.Instance.TakeScreenshot();
		return DefuserConnector.Instance.ReadComponent(ss, DefuserConnector.Instance.GetLightsState(ss), reader, Utils.CurrentModuleArea);
	}

	[Obsolete("String inputs are being replaced with IInputAction")]
	public AimlTask SendInputsAsync(string inputs) => sendInputs.CallAsync(inputs);
	[Obsolete("String inputs are being replaced with IInputAction")]
	public void SendInputs(string inputs) => this.Context.Reply($"<oob><sendinputs>{inputs}</sendinputs></oob>");

	/// <summary>Presses the specified buttons in sequence.</summary>
	public void SendInputs(params Button[] buttons) => this.SendInputs(from b in buttons select new ButtonAction(b));
	/// <summary>Presses the specified buttons in sequence.</summary>
	public void SendInputs(IEnumerable<Button> buttons) => this.SendInputs(from b in buttons select new ButtonAction(b));
	/// <summary>Performs the specified input actions in sequence.</summary>
	public void SendInputs(params IInputAction[] actions) => this.SendInputs((IEnumerable<IInputAction>) actions);
	/// <summary>Performs the specified input actions in sequence.</summary>
	public void SendInputs(IEnumerable<IInputAction> actions) {
		if (this.IsDisposed) throw new ObjectDisposedException(nameof(Interrupt));
		DefuserConnector.Instance.SendInputs(actions);
	}
	/// <summary>Creates an <see cref="AimlTask"/> that presses the specified buttons in sequence and completes after this is complete.</summary>
	public AimlTask SendInputsAsync(params Button[] buttons) => this.SendInputsAsync(from b in buttons select new ButtonAction(b));
	/// <summary>Creates an <see cref="AimlTask"/> that presses the specified buttons in sequence and completes after this is complete.</summary>
	public AimlTask SendInputsAsync(IEnumerable<Button> buttons, CancellationToken cancellationToken = default) => this.SendInputsAsync(from b in buttons select new ButtonAction(b), cancellationToken);
	/// <summary>Creates an <see cref="AimlTask"/> that performs the specified input actions in sequence and completes after this is complete.</summary>
	public AimlTask SendInputsAsync(params IInputAction[] actions) => this.SendInputsAsync(actions);
	/// <summary>Creates an <see cref="AimlTask"/> that performs the specified input actions in sequence and completes after this is complete.</summary>
	public AimlTask SendInputsAsync(IEnumerable<IInputAction> actions, CancellationToken cancellationToken = default) {
		if (this.IsDisposed) throw new ObjectDisposedException(nameof(Interrupt));
		cancellationToken.Register(DefuserConnector.Instance.CancelInputs);
		var guid = Guid.NewGuid();
		var task = inputCallback.CallAsync(this.Context, guid, cancellationToken);
		DefuserConnector.Instance.SendInputs(actions.Concat(new[] { new CallbackAction(guid) }));
		return task;
	}

	/// <summary>Performs the specified input actions, announces a resulting solve, and returns the resulting module light state afterward. If a strike occurs, it interrupts the sequence.</summary>
	public Task<ModuleLightState> SubmitAsync(params Button[] buttons) => this.SubmitAsync(from b in buttons select new ButtonAction(b));
	/// <summary>Performs the specified input actions, announces a resulting solve, and returns the resulting module light state afterward. If a strike occurs, it interrupts the sequence.</summary>
	public Task<ModuleLightState> SubmitAsync(IEnumerable<Button> buttons) => this.SubmitAsync(from b in buttons select new ButtonAction(b));
	/// <summary>Performs the specified input actions, announces a resulting solve, and returns the resulting module light state afterward. If a strike occurs, it interrupts the sequence.</summary>
	public Task<ModuleLightState> SubmitAsync(params IInputAction[] actions) => this.SubmitAsync((IEnumerable<IInputAction>) actions);
	/// <summary>Performs the specified input actions, announces a resulting solve, and returns the resulting module light state afterward. If a strike occurs, it interrupts the sequence.</summary>
	public async Task<ModuleLightState> SubmitAsync(IEnumerable<IInputAction> actions) {
		if (this.IsDisposed) throw new ObjectDisposedException(nameof(Interrupt));
		if (this.submitCancellationTokenSource is not null) throw new InvalidOperationException("Already submitting.");
		if (GameState.Current.CurrentModuleNum is null) throw new InvalidOperationException("No current module.");
		var module = GameState.Current.Modules[GameState.Current.CurrentModuleNum.Value];
		var context = AimlAsyncContext.Current ?? throw new InvalidOperationException("No current request");

		this.submitCancellationTokenSource = new CancellationTokenSource();
		this.submitCurrentSlot = module.Slot;
		GameState.Current.Strike += this.GameState_Strike;
		try {
			await this.SendInputsAsync(actions, this.submitCancellationTokenSource.Token);
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
						nextModule = GameState.Current.Modules.FindIndex(GameState.Current.SelectedModuleNum is int i ? i + 1 : 0, m => m.Script.PriorityCategory != PriorityCategory.Needy && !m.IsSolved);
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
			this.submitCancellationTokenSource?.Dispose();
			this.submitCancellationTokenSource = null;
		}
	}

	private void GameState_Strike(object? sender, StrikeEventArgs e) {
		if (e.Slot == this.submitCurrentSlot) {
			this.Context = e.Context;
			this.submitCancellationTokenSource?.Cancel();
		}
	}

	/// <summary>Exits this interrupt, allowing the bot to enter another interrupt. After calling this method, this instance is no longer usable.</summary>
	public void Exit() => this.Dispose();

	/// <summary>Exits this interrupt, allowing the bot to enter another interrupt. After calling this method, this instance is no longer usable.</summary>
	public void Dispose() {
		if (!this.IsDisposed) {
			this.IsDisposed = true;
			Exit(AimlAsyncContext.Current ?? throw new InvalidOperationException("No current request"));
			GC.SuppressFinalize(this);
		}
	}
}
