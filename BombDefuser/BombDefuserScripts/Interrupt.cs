namespace BombDefuserScripts;
public class Interrupt : IDisposable {
	public static bool EnableInterrupts { get; set; } = true;
	private static readonly Queue<TaskCompletionSource<Interrupt>> interruptQueue = new();

	public bool IsDisposed { get; private set; }

	public AimlAsyncContext Context { get; }

	private Interrupt(AimlAsyncContext context) => this.Context = context ?? throw new ArgumentNullException(nameof(context));

	~Interrupt() => this.Dispose();

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
				await Utils.SelectModuleAsync(interrupt, GameState.Current.CurrentModuleNum!.Value);
				await AimlTasks.Delay(0.75);
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

	public static async Task<ModuleLightState> SubmitAsync(AimlAsyncContext context, string inputs) {
		using var interrupt = await EnterAsync(context);
		return await interrupt.SubmitAsync(inputs);
	}

	public async Task<ModuleLightState> SubmitAsync(string inputs) {
		if (this.IsDisposed) throw new ObjectDisposedException(nameof(Interrupt));
		var context = AimlAsyncContext.Current ?? throw new InvalidOperationException("No current request");
		await this.SendInputsAsync(inputs);
		await AimlTasks.Delay(0.5);  // Wait for the interaction punch to end.
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
