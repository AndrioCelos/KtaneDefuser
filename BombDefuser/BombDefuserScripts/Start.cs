using System.ComponentModel;
using Aiml;

namespace BombDefuserScripts;
[AimlInterface]
internal static class Start {
	private static bool waitingForLights;

	[AimlCategory("OOB DefuserSocketMessage NewBomb *"), EditorBrowsable(EditorBrowsableState.Never)]
	public static void NewBomb() => waitingForLights = true;

	[AimlCategory("OOB DefuserSocketMessage Lights *"), EditorBrowsable(EditorBrowsableState.Never)]
	public static async Task Lights(AimlAsyncContext context, bool on) {
		if (on && waitingForLights) {
			waitingForLights = false;
			await GameStartAsync(context);
		}
	}

	[AimlCategory("test start")]
	public static async Task TestCategory(AimlAsyncContext context) => await GameStartAsync(context);

	internal static async Task GameStartAsync(AimlAsyncContext context) {
		// 0. Clear previous game variables.
		GameState.Current = new();
		// 1. Pick up the bomb.
		using var interrupt = await Interrupt.EnterAsync(context);
		interrupt.SendInputs("a");
		await AimlTasks.Delay(1);
		// 2. Identify components on the bomb.
		using (var ss = DefuserConnector.Instance.TakeScreenshot())
			await RegisterComponentsAsync(context, ss);
		// 3. Turn the bomb around and identify widgets on the side.
		await Utils.SelectFaceAsync(interrupt, 1, SelectFaceAlignMode.CheckWidgets);
		// 4. Repeat steps 2-3 for the other faces.
		using (var ss = DefuserConnector.Instance.TakeScreenshot())
			await RegisterComponentsAsync(context, ss);
		await Utils.SelectFaceAsync(interrupt, 0, SelectFaceAlignMode.CheckWidgets);
		// 5. Turn the bomb to the bottom face.
		interrupt.SendInputs("ry:-0.875");
		await AimlTasks.Delay(0.5);
		// 6. Identify widgets on the bottom face.
		using (var ss = DefuserConnector.Instance.TakeScreenshot())
			Edgework.RegisterWidgets(context, false, ss);
		// 7. Turn the bomb to the top face.
		interrupt.SendInputs("ry:1");
		await AimlTasks.Delay(0.5);
		// 8. Identify widgets on the top face.
		using (var ss = DefuserConnector.Instance.TakeScreenshot())
			Edgework.RegisterWidgets(context, false, ss);
		// 9. Reset the bomb tilt.
		interrupt.SendInputs("ry:0");
		context.Reply("Ready.");
	}

	private static async Task RegisterComponentsAsync(AimlAsyncContext context, Image<Rgba32> screenshot) {
		var components = Enumerable.Range(0, 6).Select(i => DefuserConnector.Instance.GetComponentReader(screenshot, Utils.GetPoints(new ComponentSlot(GameState.Current.SelectedFaceNum, i % 3, i / 3)))).ToList();
		var needTimerRead = false;
		var anyModules = false;
		for (var i = 0; i < components.Count; i++) {
			var component = components[i];
			var actualComponent = DefuserConnector.Instance.CheatGetComponentReader(GameState.Current.SelectedFaceNum, i % 3, i / 3);
			if (actualComponent != component && !(actualComponent is null && component is BombDefuserConnector.Components.Timer)) {
				context.RequestProcess.Log(LogLevel.Warning, $"Wrong component at {GameState.Current.SelectedFaceNum} {i % 3} {i / 3} - identified: {component?.Name}; actual: {actualComponent?.Name}");
				component = actualComponent;
			}

			var slot = new ComponentSlot(GameState.Current.SelectedFaceNum, i % 3, i / 3);
			if (!anyModules && component is not (null or BombDefuserConnector.Components.Timer)) {
				// Set the initially selected slot on each side to be the first one with a module in it.
				anyModules = true;
				GameState.Current.Faces[GameState.Current.SelectedFaceNum].SelectedSlot = slot;
			}
			RegisterComponent(context, slot, component);  // TODO: This assumes the vanilla bomb layout. It will need to be updated for other layouts.

			// If we saw the timer but don't already know the bomb time, read it now.
			needTimerRead |= component is BombDefuserConnector.Components.Timer;
		}
		if (needTimerRead)
			await Timer.ReadTimerAsync(screenshot);
	}

	private static void RegisterComponent(AimlAsyncContext context, ComponentSlot slot, ComponentReader? component) {
		switch (component) {
			case null:
				break;
			case BombDefuserConnector.Components.Timer:
				GameState.Current.TimerSlot = slot;
				context.RequestProcess.Log(LogLevel.Info, $"Registering timer @ {slot}");
				break;
			default:
				var script = ModuleScript.Create(component);
				script.ModuleIndex = GameState.Current.Modules.Count;
				var module = new ModuleState(slot, component, script);
				GameState.Current.Faces[slot.Face][slot] = module;
				GameState.Current.Modules.Add(module);
				GameState.Current.Faces[slot.Face].HasModules = true;
				context.RequestProcess.Log(LogLevel.Info, $"Registering module {script.ModuleIndex + 1}: {component.Name} @ {slot}");
				if (script.PriorityCategory != PriorityCategory.None)
					context.Reply($"<oob><queue/></oob> Module {script.ModuleIndex + 1} is {script.IndefiniteDescription}.");
				script.Initialise(context);
				if (GameState.Current.UnknownNeedyStates.TryGetValue(slot, out var state)) {
					GameState.Current.UnknownNeedyStates.Remove(slot);
					script.NeedyState = state;
					script.NeedyStateChanged(context, state);
				}
				break;
		}
	}
}

public enum SelectFaceAlignMode {
	None,
	Align,
	CheckWidgets
}
