namespace BombDefuserScripts;
internal class Interrupt : IDisposable {
	public static bool IsModuleSubmitInProgress { get; set; }
	public static bool EnableInterrupts { get; set; } = true;
	public static int? InterruptedModuleNum { get; set; }
	private static Queue<(TaskCompletionSource<Interrupt> taskSource, AimlAsyncContext context)> interruptQueue = new();

	public bool IsDisposed { get; private set; }
	
	public AimlAsyncContext Context { get; }

	private Interrupt(AimlAsyncContext context) => this.Context = context ?? throw new ArgumentNullException(nameof(context));

	public static Task<Interrupt> EnterAsync(AimlAsyncContext context) {
		if (EnableInterrupts) {
			EnableInterrupts = false;
			InterruptedModuleNum = GameState.Current.SelectedModuleNum;
			return Task.FromResult(new Interrupt(context));
		}
		var taskSource = new TaskCompletionSource<Interrupt>();
		interruptQueue.Enqueue((taskSource, context));
		return taskSource.Task;
	}

	public static async void Exit(AimlAsyncContext context) {
		IsModuleSubmitInProgress = false;
		if (interruptQueue.TryDequeue(out var entry)) {
			entry.taskSource.SetResult(new(context));
		} else {
			if (GameState.Current.SelectedModuleNum != InterruptedModuleNum) {
				// Re-select the bomb or module that was interrupted.
				if (InterruptedModuleNum is null) {
					context.SendInputs("b");
					GameState.Current.SelectedModuleNum = null;
					GameState.Current.FocusState = FocusState.Bomb;
				} else
					await Utils.SelectModuleAsync(context, InterruptedModuleNum!.Value);
				await AimlTasks.Delay(0.5);
				// Perhaps another interrupt occurred during this.
				if (interruptQueue.TryDequeue(out entry))
					entry.taskSource.SetResult(new(context));
				else
					EnableInterrupts = true;
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
		await context.SendInputsAsync(inputs);
		await AimlTasks.Delay(0.5);  // Wait for the interaction punch to end.
		var ss = await AimlTasks.TakeScreenshotAsync();
		var result = BombDefuserAimlService.Instance.GetLightState(ss, Utils.CurrentModulePoints);
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
		}
	}
}
