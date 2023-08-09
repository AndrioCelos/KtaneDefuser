namespace BombDefuserScripts;
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

	public T Read<T>(ComponentReader<T> reader) where T : notnull {
		if (this.IsDisposed) throw new ObjectDisposedException(nameof(Interrupt));
		using var ss = DefuserConnector.Instance.TakeScreenshot();
		return DefuserConnector.Instance.ReadComponent(ss, reader, Utils.CurrentModulePoints);
	}

	public Task<ModuleLightState> SubmitAsync(params Button[] buttons) => this.SubmitAsync(from b in buttons select new ButtonAction(b));
	public Task<ModuleLightState> SubmitAsync(IEnumerable<Button> buttons) => this.SubmitAsync(from b in buttons select new ButtonAction(b));
	public Task<ModuleLightState> SubmitAsync(params IInputAction[] actions) => this.SubmitAsync((IEnumerable<IInputAction>) actions);
	public async Task<ModuleLightState> SubmitAsync(IEnumerable<IInputAction> actions) {
		if (this.IsDisposed) throw new ObjectDisposedException(nameof(Interrupt));
		var context = AimlAsyncContext.Current ?? throw new InvalidOperationException("No current request");
		await this.SendInputsAsync(actions);
		await Delay(0.5);  // Wait for the interaction punch to end.
		using var ss = DefuserConnector.Instance.TakeScreenshot();
		var result = DefuserConnector.Instance.GetModuleLightState(ss, Utils.CurrentModulePoints);
		switch (result) {
			case ModuleLightState.Solved: context.Reply("Module complete."); break;
			case ModuleLightState.Strike: context.Reply("Strike."); break;
		}
		return result;
	}

	public void Exit() => this.Dispose();

	public void Dispose() {
		if (!this.IsDisposed) {
			this.IsDisposed = true;
			Exit(AimlAsyncContext.Current ?? throw new InvalidOperationException("No current request"));
			GC.SuppressFinalize(this);
		}
	}
}
