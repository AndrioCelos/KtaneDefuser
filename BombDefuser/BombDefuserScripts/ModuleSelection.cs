using System.ComponentModel;

namespace BombDefuserScripts;
[AimlInterface]
internal static class ModuleSelection {
	/// <summary>Handles switching the user's selection to the specified module.</summary>
	internal static async Task ChangeModuleAsync(AimlAsyncContext context, int index) {
		GameState.Current.CurrentModule?.Script.Stopped(context);
		GameState.Current.CurrentModuleNum = index;
		var script = GameState.Current.CurrentModule!.Script;
		context.RequestProcess.Log(Aiml.LogLevel.Info, $"Selected module {index + 1} ({GameState.Current.CurrentModule.Reader.Name})");
		context.RequestProcess.User.Topic = script.Topic;
		context.Reply($"<oob><queue/><setgrammar>{script.Topic}</setgrammar></oob> Module {index + 1} is {script.IndefiniteDescription}.");
		script.Started(context);
		// If we aren't in another interrupt, select this module. (If we are, Interrupt.Exit will select this module afterward if it is still the current module.)
		if (Interrupt.EnableInterrupts) {
			using var interrupt = await Interrupt.EnterAsync(context);
			await Utils.SelectModuleAsync(interrupt, GameState.Current.CurrentModuleNum.Value, true);
			script.ModuleSelected(context);
		}
	}

	/// <summary>Populates the user selection queue with all modules that match the specified precidate and are not already solved. The 'next module' command will then cycle through these first.</summary>
	internal static async Task QueueModulesAsync(AimlAsyncContext context, Func<ModuleState, bool> predicate) {
		var modules = GameState.Current.Modules.Where(predicate);
		if (!modules.Any()) {
			context.Reply("No such modules.");
			return;
		} else if (modules.All(m => m.IsSolved)) {
			context.Reply("All of those modules are solved.");
			return;
		}
		GameState.Current.NextModuleNums.Clear();
		foreach (var i in Enumerable.Range(0, GameState.Current.Modules.Count).Where(i => GameState.Current.Modules[i] is var module && !module.IsSolved && predicate(module)))
			GameState.Current.NextModuleNums.Enqueue(i);
		if (GameState.Current.NextModuleNums.Count > 1)
			context.Reply($"{GameState.Current.NextModuleNums.Count} of those remain.");
		await ChangeModuleAsync(context, GameState.Current.NextModuleNums.Dequeue());
	}

	[AimlCategory("module <set>number</set>"), EditorBrowsable(EditorBrowsableState.Never)]
	public static async Task SelectModule(AimlAsyncContext context, int number) {
		GameState.Current.NextModuleNums.Clear();
		var index = number - 1;
		if (index < 0) {
			context.Reply("Not a valid number.");
			return;
		}
		if (index >= GameState.Current.Modules.Count) {
			context.Reply("There do not seem to be that many modules.");
			return;
		}
		await ChangeModuleAsync(context, index);
	}

	[AimlCategory("first module"), EditorBrowsable(EditorBrowsableState.Never)]
	public static async Task SelectModuleFirst(AimlAsyncContext context) {
		GameState.Current.NextModuleNums.Clear();
		var index = GameState.Current.Modules.FindIndex(m => m.Script.PriorityCategory != PriorityCategory.Needy && !m.IsSolved);
		if (index < 0) {
			context.Reply("Could not find any modules.");
			return;
		}
		await ChangeModuleAsync(context, index);
	}

	[AimlCategory("next module"), EditorBrowsable(EditorBrowsableState.Never)]
	public static async Task SelectModuleNext(AimlAsyncContext context) {
		if (!GameState.Current.NextModuleNums.TryDequeue(out var index))
			index =  GameState.Current.Modules.FindIndex(GameState.Current.SelectedModuleNum is int i ? i + 1 : 0, m => m.Script.PriorityCategory != PriorityCategory.Needy && !m.IsSolved);
		if (index < 0) {
			context.Reply("Could not find any more modules.");
			return;
		}
		await ChangeModuleAsync(context, index);
	}

	[AimlCategory("vanilla modules"), EditorBrowsable(EditorBrowsableState.Never)]
	public static Task QueueVanilla(AimlAsyncContext context) => QueueModulesAsync(context, m => m.Type <= ModuleType.WireSequence);

	[AimlCategory("select <set>ModuleType</set>"), AimlCategory("select a <set>ModuleType</set>"), AimlCategory("select an <set>ModuleType</set>"), AimlCategory("select the <set>ModuleType</set>"), EditorBrowsable(EditorBrowsableState.Never)]
	public static Task QueueSpecificType(AimlAsyncContext context, ModuleType moduleType) => QueueModulesAsync(context, m => m.Type == moduleType);
}
