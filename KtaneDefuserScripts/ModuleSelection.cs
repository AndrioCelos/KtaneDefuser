﻿using System.ComponentModel;
using System.Reflection;
using AngelAiml.Media;
using Microsoft.Extensions.Logging.Abstractions;

namespace KtaneDefuserScripts;
[AimlInterface]
internal static partial class ModuleSelection {
	internal static ILogger Logger = NullLogger.Instance;
	
	/// <summary>Handles switching the user's selection to the specified module.</summary>
	internal static async Task ChangeModuleAsync(AimlAsyncContext context, int index, bool silent) {
		GameState.Current.CurrentModule?.Script.Stopped(context);
		GameState.Current.CurrentModuleNum = index;
		var script = GameState.Current.CurrentModule!.Script;
		LogSelectedModule(Logger, index + 1, GameState.Current.CurrentModule.Reader.Name);
		context.RequestProcess.User.Topic = script.Topic;
		context.Reply($"<oob><setgrammar>{script.Topic}</setgrammar></oob>");
		if (!silent)
			context.Reply($"<priority/> Module {index + 1} is {script.IndefiniteDescription}.");
		script.Started(context);
		// If we aren't in another interrupt, select this module. (If we are, Interrupt.Exit will select this module afterward if it is still the current module.)
		if (Interrupt.EnableInterrupts) {
			using var interrupt = await Interrupt.EnterAsync(context);
			await Utils.SelectModuleAsync(interrupt, GameState.Current.CurrentModuleNum.Value, true);
			script.ModuleSelected(interrupt);
		}
	}

	/// <summary>Populates the user selection queue with all modules that match the specified predicate and are not solved. The 'next module' command will then cycle through these first.</summary>
	private static async Task QueueModulesAsync(AimlAsyncContext context, Func<ModuleState, bool> predicate) {
		var modules = GameState.Current.Modules.Where(predicate);
		if (!modules.Any()) {
			context.Reply("No such modules.");
			return;
		}
		if (modules.All(m => m.IsSolved)) {
			context.Reply("All of those modules are solved.");
			return;
		}
		GameState.Current.NextModuleNums.Clear();
		foreach (var i in Enumerable.Range(0, GameState.Current.Modules.Count).Where(i => GameState.Current.Modules[i] is { IsSolved: false } module && predicate(module)))
			GameState.Current.NextModuleNums.Enqueue(i);
		if (GameState.Current.NextModuleNums.Count > 1)
			context.Reply($"{GameState.Current.NextModuleNums.Count} of those remain.");
		await ChangeModuleAsync(context, GameState.Current.NextModuleNums.Dequeue(), false);
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
		await ChangeModuleAsync(context, index, false);
	}

	[AimlCategory("first module"), EditorBrowsable(EditorBrowsableState.Never)]
	public static async Task SelectModuleFirst(AimlAsyncContext context) {
		// Queue most solvable modules to prepare to cycle through them.
		GameState.Current.NextModuleNums.Clear();
		foreach (var i in from e in GameState.Current.Modules.Index() where !e.Item.IsSolved && !e.Item.Script.PriorityCategory.HasFlag(PriorityCategory.Needy) select e.Index)
			GameState.Current.NextModuleNums.Enqueue(i);

		if (!GameState.Current.NextModuleNums.TryDequeue(out var index)) {
			context.Reply("Could not find any modules.");
			return;
		}
		await ChangeModuleAsync(context, index, false);
	}

	[AimlCategory("next module"), AimlCategory("next", That = "MODULE COMPLETE ^"), EditorBrowsable(EditorBrowsableState.Never)]
	public static async Task SelectModuleNext(AimlAsyncContext context) {
		if (!GameState.Current.NextModuleNums.TryDequeue(out var index))
			index =  GameState.Current.Modules.FindIndex(GameState.Current.SelectedModuleNum is { } i ? i + 1 : 0, m => !m.Script.PriorityCategory.HasFlag(PriorityCategory.Needy) && !m.IsSolved);
		if (index < 0) {
			context.Reply("Could not find any more modules.");
			return;
		}
		await ChangeModuleAsync(context, index, false);
	}

	[AimlCategory("vanilla modules"), EditorBrowsable(EditorBrowsableState.Never)]
	public static Task QueueVanilla(AimlAsyncContext context) => QueueModulesAsync(context, m => m.Type <= ModuleType.WireSequence);

	[AimlCategory("select <set>ModuleType</set>"), AimlCategory("select a <set>ModuleType</set>"), AimlCategory("select an <set>ModuleType</set>"), AimlCategory("select the <set>ModuleType</set>"), EditorBrowsable(EditorBrowsableState.Never)]
	public static Task QueueSpecificType(AimlAsyncContext context, ModuleType moduleType) => QueueModulesAsync(context, m => m.Type == moduleType);

	[AimlCategory("specific module…"), EditorBrowsable(EditorBrowsableState.Never)]
	public static void SelectModuleReplyMenuEntry(AimlAsyncContext context)
		=> context.Reply($"<reply><text>Vanilla modules…</text><postback>{nameof(SelectModuleMenu)} vanilla 0</postback></reply><reply><text>Mod modules…</text><postback>{nameof(SelectModuleMenu)} mods 0</postback></reply>");

	[AimlCategory($"{nameof(SelectModuleMenu)} vanilla <set>number</set>"), EditorBrowsable(EditorBrowsableState.Never)]
	public static void SelectModuleMenuVanilla(AimlAsyncContext context, int startIndex) => SelectModuleMenu(context, true, startIndex);

	[AimlCategory($"{nameof(SelectModuleMenu)} mods <set>number</set>"), EditorBrowsable(EditorBrowsableState.Never)]
	public static void SelectModuleMenuMods(AimlAsyncContext context, int startIndex) => SelectModuleMenu(context, false, startIndex);

	private static void SelectModuleMenu(AimlAsyncContext context, bool vanilla, int startIndex) {
		var list = (from f in typeof(ModuleType).GetFields() where f.IsStatic && (vanilla ? (ModuleType) f.GetValue(null)! is >= 0 and <= ModuleType.WireSequence : (ModuleType) f.GetValue(null)! > ModuleType.WireSequence)
					select (f, f.GetCustomAttributes<AimlSetItemAttribute>().FirstOrDefault() is { } attr ? attr.Phrase : f.Name)).ToList();
		list.Sort((x, y) => ModuleNameComparer.Instance.Compare(x.Item2, y.Item2));
		context.AddReplies(list.Skip(startIndex).Take(6).Select(f => new Reply(f.Item2, $"select {f.Item2}")).Append(new("More…", $"{nameof(SelectModuleMenu)} {(vanilla ? "vanilla" : "mods")} {(startIndex + 6 >= list.Count ? 0 : startIndex + 6)}")));
	}

	#region Log templates
	
	[LoggerMessage(LogLevel.Warning, "Selected module {Number} ({Name})")]
	private static partial void LogSelectedModule(ILogger logger, int number, string name);
	
	#endregion

	private class ModuleNameComparer : IComparer<string> {
		public static ModuleNameComparer Instance { get; } = new();

		public int Compare(string? x, string? y) {
			if (x is null) return y is null ? 0 : -1;
			if (y is null) return 1;
			if (x.StartsWith("The ")) x = x[4..];
			if (y.StartsWith("The ")) y = y[4..];
			return StringComparer.InvariantCultureIgnoreCase.Compare(x, y);
		}
	}
}
