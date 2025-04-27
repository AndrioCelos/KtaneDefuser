using System.ComponentModel;
using System.Diagnostics;
using AngelAiml;
using Microsoft.Extensions.Logging.Abstractions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace KtaneDefuserScripts;
[AimlInterface]
internal static partial class Start {
	private static ILogger logger = NullLogger.Instance;
	private static bool waitingForLights;
	private static Stopwatch? startDelayStopwatch;

	[AimlCategory("OOB NewBomb *"), EditorBrowsable(EditorBrowsableState.Never)]
	public static void NewBomb(RequestProcess process) {
		// 0. Clear previous game variables.
		waitingForLights = true;
		GameState.Current = new(process.Bot.LoggerFactory);
		logger = GameState.Current.LoggerFactory.CreateLogger(nameof(Start));
		Edgework.Logger = GameState.Current.LoggerFactory.CreateLogger(nameof(Edgework));
		ModuleSelection.Logger = GameState.Current.LoggerFactory.CreateLogger(nameof(ModuleSelection));
	}

	[AimlCategory("OOB LightsChange *"), EditorBrowsable(EditorBrowsableState.Never)]
	public static async Task Lights(AimlAsyncContext context, bool on) {
		if (on && waitingForLights) {
			waitingForLights = false;
			startDelayStopwatch = Stopwatch.StartNew();
			await GameStartAsync(context);
		}
	}

	[AimlCategory("test start")]
	public static async Task TestCategory(AimlAsyncContext context) => await GameStartAsync(context);

	private static async Task GameStartAsync(AimlAsyncContext context) {
		GameState.Current = new(context.RequestProcess.Bot.LoggerFactory);
		
		// 1. Pick up the bomb
		GameState.Current.TimerStopwatch.Restart();
		using var interrupt = await Interrupt.EnterAsync(context);
		using (var ss = DefuserConnector.Instance.TakeScreenshot())
			GameState.Current.BombType = ImageUtils.IsCenturion(ss) ? BombType.Centurion : BombType.Standard;
		interrupt.SendInputs(new ButtonAction(Button.A, ButtonActionType.Release), new AxisAction(Axis.RightStickX, 0), new AxisAction(Axis.RightStickY, 0));
		interrupt.SendInputs(Button.A);
		await Delay(1);  // Wait for modules to initialise.
		// 2. Identify components on the bomb.
		using (var ss = DefuserConnector.Instance.TakeScreenshot())
			await RegisterComponentsAsync(interrupt, context, ss);
		// 3. Turn the bomb around and identify widgets on the side.
		await Utils.SelectFaceAsync(interrupt, 1, SelectFaceAlignMode.CheckWidgets);
		// 4. Repeat steps 2-3 for the other faces.
		using (var ss = DefuserConnector.Instance.TakeScreenshot())
			await RegisterComponentsAsync(interrupt, context, ss);
		await Utils.SelectFaceAsync(interrupt, 0, SelectFaceAlignMode.CheckWidgets);
		// 5. Turn the bomb to the bottom face.
		GameState.Current.LookingAtSide = true;
		interrupt.SendInputs(new AxisAction(Axis.RightStickY, -0.875f));
		await Delay(0.5);
		// 6. Identify widgets on the bottom face.
		using (var ss = DefuserConnector.Instance.TakeScreenshot())
			Edgework.RegisterWidgets(context, false, ss);
		// 7. Turn the bomb to the top face.
		interrupt.SendInputs(new AxisAction(Axis.RightStickY, 1));
		await Delay(0.5);
		// 8. Identify widgets on the top face.
		using (var ss = DefuserConnector.Instance.TakeScreenshot())
			Edgework.RegisterWidgets(context, false, ss);
		// 9. Reset the bomb tilt.
		interrupt.SendInputs(new AxisAction(Axis.RightStickY, 0));
		GameState.Current.LookingAtSide = false;
		context.Reply("Ready. <reply>edgework</reply><reply>first module</reply><reply>vanilla modules</reply><reply>specific module…</reply>");
	}

	private static async Task RegisterComponentsAsync(Interrupt interrupt, AimlAsyncContext context, Image<Rgba32> screenshot) {
		var lightsState = DefuserConnector.Instance.GetLightsState(screenshot);
		if (lightsState != LightsState.On) throw new ArgumentException($"Can't identify components on lights state {lightsState}.");
		if (GameState.Current.BombType == BombType.Centurion) {
			var anyModules = false;
			foreach (var (centre, group) in CenturionUtil.SlotGroups[GameState.Current.SelectedFaceNum]) {
				await Utils.SelectSlotAsync(interrupt, centre, true);
				anyModules = await ReadComponentSlotsAsync(context, screenshot, group, anyModules);
			}
		} else {
			var slots = from y in Enumerable.Range(0, 2) from x in Enumerable.Range(0, 3) select new Slot(0, GameState.Current.SelectedFaceNum, x, y);
			await ReadComponentSlotsAsync(context, screenshot, slots, false);
		}
	}

	private static async Task<bool> ReadComponentSlotsAsync(AimlAsyncContext context, Image<Rgba32> screenshot, IEnumerable<Slot> slots, bool anyModules) {
		var needTimerRead = false;
		foreach (var slot in slots) {
			var points = Utils.GetPoints(slot);
			var component = DefuserConnector.Instance.GetComponentReader(screenshot, points);
			var actualComponent = DefuserConnector.Instance.CheatGetComponentReader(slot);
			if (actualComponent != component && !(actualComponent is null && component is KtaneDefuserConnector.Components.Timer)) {
				LogWrongComponent(logger, slot, component?.Name, actualComponent?.Name);
				component = actualComponent;
			}

			if (!anyModules && component is not (null or KtaneDefuserConnector.Components.Timer)) {
				// Set the initially selected slot on each side to be the first one with a module in it.
				anyModules = true;
				GameState.Current.Faces[GameState.Current.SelectedFaceNum].SelectedSlot = slot;
			}
			var module = RegisterComponent(context, slot, component);  // TODO: This assumes the vanilla bomb layout. It will need to be updated for other layouts.
			if (module is not null) {
				var lightState = DefuserConnector.Instance.GetModuleLightState(screenshot, points);
				if (lightState == ModuleLightState.Solved) {
					LogSolvedModule(logger, module.Script.ModuleIndex + 1);
					module.IsSolved = true;
				}
			}

			// If we saw the timer but don't already know the bomb time, read it now.
			needTimerRead |= component is KtaneDefuserConnector.Components.Timer;
		}
		if (needTimerRead) {
			await TimerUtil.ReadTimerAsync(screenshot, startDelayStopwatch is not null && startDelayStopwatch.Elapsed < TimeSpan.FromSeconds(2));
			startDelayStopwatch = null;
		}

		return anyModules;
	}

	private static ModuleState? RegisterComponent(AimlAsyncContext context, Slot slot, ComponentReader? component) {
		switch (component) {
			case null:
				return null;
			case KtaneDefuserConnector.Components.Timer:
				GameState.Current.TimerSlot = slot;
				LogRegisteringTimer(logger, slot);
				return null;
			default:
				var script = ModuleScript.Create(component);
				script.ModuleIndex = GameState.Current.Modules.Count;
				var module = new ModuleState(slot, component, script);
				GameState.Current.Faces[slot.Face][slot] = module;
				GameState.Current.Modules.Add(module);
				GameState.Current.Faces[slot.Face].HasModules = true;
				LogRegisteringModule(logger, script.ModuleIndex + 1, component.Name, slot);
				if (script.PriorityCategory != PriorityCategory.None)
					context.Reply($"<priority/> Module {script.ModuleIndex + 1} is {script.IndefiniteDescription}.");
				script.Initialise(context);
				NeedyState state;
				lock (GameState.Current.UnknownNeedyStates) {
					GameState.Current.UnknownNeedyStates.TryGetValue(slot, out state);
					GameState.Current.UnknownNeedyStates.Remove(slot);
				}

				if (state == 0) return module;
				script.NeedyState = state;
				script.NeedyStateChanged(context, state);
				return module;
		}
	}
	
	#region Log templates
	
	[LoggerMessage(LogLevel.Warning, "Wrong component at {Slot} - identified: {IdentifiedComponent}; actual: {ActualComponent}")]
	private static partial void LogWrongComponent(ILogger logger, Slot slot, string? identifiedComponent, string? actualComponent);
	
	[LoggerMessage(LogLevel.Information, "Registering timer @ {Slot}")]
	private static partial void LogRegisteringTimer(ILogger logger, Slot slot);
	
	[LoggerMessage(LogLevel.Information, "Registering module {Number}: {Name} @ {Slot}")]
	private static partial void LogRegisteringModule(ILogger logger, int number, string name, Slot slot);
	
	[LoggerMessage(LogLevel.Information, "Module {Number} is solved.")]
	private static partial void LogSolvedModule(ILogger logger, int number);
	
	#endregion
}

public enum SelectFaceAlignMode {
	/// <summary>Do not align the bomb to the specified face. It will not be possible to reliably read modules until a module is selected or the bomb is dropped.</summary>
	None,
	/// <summary>Align the bomb to the specified face after moving.</summary>
	Align,
	/// <summary>Register widgets on the intermediate side face, then align the bomb to the specified face after moving.</summary>
	CheckWidgets
}
