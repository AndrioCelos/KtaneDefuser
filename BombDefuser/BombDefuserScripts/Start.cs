using System.ComponentModel;
using Aiml;

namespace BombDefuserScripts;
[AimlInterface]
internal static class Start {
	private static bool waitingForLights;

	[AimlCategory("OOB DefuserSocketMessage NewBomb *"), EditorBrowsable(EditorBrowsableState.Never)]
	public static void NewBomb() {
		waitingForLights = true;
	}

	[AimlCategory("OOB DefuserSocketMessage Lights *"), EditorBrowsable(EditorBrowsableState.Never)]
	public static async Task Lights(AimlAsyncContext context, bool on) {
		if (on && waitingForLights) {
			waitingForLights = false;
			await GameStartAsync(context);
		}
	}


	[AimlCategory("test start")]
	public static async Task TestCategory(AimlAsyncContext context) {
		await GameStartAsync(context);
	}

	internal static async Task GameStartAsync(AimlAsyncContext context) {
		// 0. Clear previous game variables.
		GameState.Current = new();
		// 1. Pick up the bomb.
		context.SendInputs("a");
		await AimlTasks.Delay(1);
		// 2. Take a screenshot.
		var screenshot = await AimlTasks.TakeScreenshotAsync();
		// 3. Identify components on the bomb. Turn the bomb around.
		await RegisterComponentsAsync(context, screenshot);
		await Utils.SelectFaceAsync(context, 1, SelectFaceAlignMode.CheckWidgets);
		// 4. Identify widgets on this side.
		// 5. Take a screenshot. Repeat steps 3-4 for the other faces, then turn the bomb to the bottom face.
		screenshot = await AimlTasks.TakeScreenshotAsync();
		await RegisterComponentsAsync(context, screenshot);
		await Utils.SelectFaceAsync(context, 0, SelectFaceAlignMode.CheckWidgets);
		context.SendInputs("ry:-0.875");
		await AimlTasks.Delay(0.5);
		// 6. Take a screenshot.
		screenshot = await AimlTasks.TakeScreenshotAsync();
		// 7. Identify widgets on the bottom face. Turn the bomb to the top face.
		Edgework.RegisterWidgets(context, false, screenshot);
		context.SendInputs("ry:1");
		await AimlTasks.Delay(0.5);
		// 8. Take a screenshot.
		screenshot = await AimlTasks.TakeScreenshotAsync();
		// 9. Identify widgets on the top face. Reset the bomb tilt.
		Edgework.RegisterWidgets(context, false, screenshot);
		context.SendInputs("ry:0");
		context.Reply("Ready.");
	}

	private static async Task RegisterComponentsAsync(AimlAsyncContext context, string screenshot) {
		var components = Enumerable.Range(0, 6).Select(i => BombDefuserAimlService.Instance.GetComponentProcessor(screenshot, Utils.GetPoints(new ComponentSlot(GameState.Current.SelectedFaceNum, i % 3, i / 3)))).ToList();
		var needTimerRead = false;
		var anyModules = false;
		for (var i = 0; i < components.Count; i++) {
			var component = components[i];
			var actualComponent = BombDefuserAimlService.Instance.CheatGetComponentProcessor(GameState.Current.SelectedFaceNum + 1, i % 3 + 1, i / 3 + 1);
			if (actualComponent != component && !(actualComponent is null && component is BombDefuserConnector.Components.Timer)) {
				context.RequestProcess.Log(LogLevel.Warning, $"Wrong component at {GameState.Current.SelectedFaceNum + 1} {i % 3 + 1}, {i / 3 + 1} - identified: {component?.Name}; actual: {actualComponent?.Name}");
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

	private static void RegisterComponent(AimlAsyncContext context, ComponentSlot slot, ComponentProcessor? component) {
		switch (component) {
			case null:
				break;
			case BombDefuserConnector.Components.Timer:
				GameState.Current.Faces[slot.Face][slot] = new(slot, component, null);
				GameState.Current.TimerSlot = slot;
				context.RequestProcess.Log(LogLevel.Info, $"Registering timer @ {slot}");
				break;
			default:
				var script = ModuleScript.Create(component);
				var module = new ModuleState(slot, component, script);
				GameState.Current.Faces[slot.Face][slot] = module;
				GameState.Current.Modules.Add(module);
				GameState.Current.Faces[slot.Face].HasModules = true;
				context.RequestProcess.Log(LogLevel.Info, $"Registering module {GameState.Current.Modules.Count}: {component.Name} @ {slot}");
				break;
		}
	}
}

public enum SelectFaceAlignMode {
	None,
	Align,
	CheckWidgets
}
