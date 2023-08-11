namespace BombDefuserScripts;
/// <summary>Represents a context for performing a task that cannot be interrupted by other bot actions.</summary>
/// <remarks>
/// All inputs and all reading of a focused module require an interrupt. To perform one of these actions when another interrupt is in progress, it is necessary to wait for that interrupt to end first.
/// This prevents issues such as desyncs and attempting to do things while holding a button.
/// </remarks>
public class Interrupt : IDisposable {
	public static bool EnableInterrupts { get; set; } = true;
	private static readonly Queue<TaskCompletionSource<Interrupt>> interruptQueue = new();

	public bool IsDisposed { get; private set; }

	/// <summary>
	/// Returns or sets the current <see cref="AimlAsyncContext"/> of this interrupt. When initialised, this will be the context in which the interrupt was entered.
	/// It may be set in scripts like The Button which handle user input during an interrupt, to continue it in a new <see cref="AimlAsyncContext"/>.
	/// </summary>
	public AimlAsyncContext Context { get; set; }

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

	/// <summary>Presses the specified buttons, announces a resulting solve or strike, and returns the resulting module light state afterward.</summary>
	public Task<ModuleLightState> SubmitAsync(params Button[] buttons) => this.SubmitAsync(from b in buttons select new ButtonAction(b));
	/// <summary>Presses the specified buttons, announces a resulting solve or strike, and returns the resulting module light state afterward.</summary>
	public Task<ModuleLightState> SubmitAsync(IEnumerable<Button> buttons) => this.SubmitAsync(from b in buttons select new ButtonAction(b));
	/// <summary>Performs the specified input actions, announces a resulting solve or strike, and returns the resulting module light state afterward.</summary>
	public Task<ModuleLightState> SubmitAsync(params IInputAction[] actions) => this.SubmitAsync((IEnumerable<IInputAction>) actions);
	/// <summary>Performs the specified input actions, announces a resulting solve or strike, and returns the resulting module light state afterward.</summary>
	public async Task<ModuleLightState> SubmitAsync(IEnumerable<IInputAction> actions) {
		if (this.IsDisposed) throw new ObjectDisposedException(nameof(Interrupt));
		var context = AimlAsyncContext.Current ?? throw new InvalidOperationException("No current request");
		await this.SendInputsAsync(actions);
		await Delay(0.5);  // Wait for the interaction punch to end.
		using var ss = DefuserConnector.Instance.TakeScreenshot();
		var result = DefuserConnector.Instance.GetModuleLightState(ss, Utils.CurrentModuleArea);
		switch (result) {
			case ModuleLightState.Solved:
				if (GameState.Current.CurrentModule is ModuleState module && !module.IsSolved) {
					context.RequestProcess.Log(Aiml.LogLevel.Info, $"Module {GameState.Current.CurrentModuleNum + 1} is solved.");
					module.IsSolved = true;
				}
				context.Reply("Module complete.");
				break;
			case ModuleLightState.Strike: context.Reply("Strike."); break;
		}
		return result;
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
