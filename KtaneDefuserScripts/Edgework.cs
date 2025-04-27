﻿using System.ComponentModel;
using KtaneDefuserConnector.Widgets;
using Microsoft.Extensions.Logging.Abstractions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace KtaneDefuserScripts;

[AimlInterface]
internal static partial class Edgework {
	internal static ILogger Logger = NullLogger.Instance;

	private static void RegisterWidget(WidgetReader? widget, Image<Rgba32> screenshot, LightsState lightsState, Quadrilateral quadrilateral) {
		if (widget is null) return;

		LogRegisteringWidget(Logger, widget.Name);
		switch (widget) {
			case BatteryHolder batteryHolder:
				GameState.Current.BatteryHolderCount++;
				GameState.Current.BatteryCount += DefuserConnector.Instance.ReadWidget(screenshot, lightsState, batteryHolder, quadrilateral);
				break;
			case Indicator indicator:
				var data = DefuserConnector.Instance.ReadWidget(screenshot, lightsState, indicator, quadrilateral);
				GameState.Current.Indicators.Add(new(data.IsLit, data.Label));
				break;
			case PortPlate portPlate:
				var ports = DefuserConnector.Instance.ReadWidget(screenshot, lightsState, portPlate, quadrilateral);
				GameState.Current.PortPlates.Add((PortTypes) ports.Value);
				break;
			case SerialNumber serialNumber:
				GameState.Current.SerialNumber = DefuserConnector.Instance.ReadWidget(screenshot, lightsState, serialNumber, quadrilateral);
				break;
		}
	}

	internal static void RegisterWidgets(AimlAsyncContext context, bool isSide, Image<Rgba32> screenshot) {
		var lightsState = DefuserConnector.Instance.GetLightsState(screenshot);
		if (lightsState != LightsState.On) throw new ArgumentException($"Can't identify widgets on lights state {lightsState}.");
		Quadrilateral[] quadrilaterals;
		if (isSide) {
			var adjustment = DefuserConnector.Instance.GetSideWidgetAdjustment(screenshot);
			quadrilaterals = new Quadrilateral[4];
			for (var i = 0; i < quadrilaterals.Length; i++) {
				var quadrilateral = Utils.SideWidgetAreas[i];
				quadrilateral.TopLeft.X += adjustment;
				quadrilateral.TopRight.X += adjustment;
				quadrilateral.BottomLeft.X += adjustment;
				quadrilateral.BottomRight.X += adjustment;
				quadrilaterals[i] = quadrilateral;
			}
		} else
			quadrilaterals = Utils.TopBottomWidgetAreas;
		var widgets = quadrilaterals.Select(p => DefuserConnector.Instance.GetWidgetReader(screenshot, p)).ToList();
		for (var i = 0; i < widgets.Count; i++) {
			var widget = widgets[i];
			RegisterWidget(widget, screenshot, lightsState, quadrilaterals[i]);  // TODO: This assumes the vanilla bomb layout. It will need to be updated for other layouts.
		}
	}

	[AimlCategory("edgework"), EditorBrowsable(EditorBrowsableState.Never)]
	internal static void EdgeworkRequest(AimlAsyncContext context) {
		if (GameState.Current.Modules.All(m => m.Type <= ModuleType.WireSequence)) {
			// Report simplified edgework information for vanilla-only games.
			var batteries = GameState.Current.BatteryCount switch {
				0 => "No batteries.",
				1 => "1 battery.",
				_ => $"{GameState.Current.BatteryCount} batteries."
			};
			var relevantIndicators = GameState.Current.Indicators.Where(i => i is { IsLit: true, Label: "CAR" or "FRK" });
			var indicators = GameState.Current.Indicators.Count > 0
				? relevantIndicators.Any()
				? $"Indicators: {string.Join(", ", from i in relevantIndicators select $"lit {NATO.Speak(i.Label)}")}."
				: "No relevant indicators."
				: "No indicators.";
			var ports = GameState.Current.PortPlates.Any(p => p.HasFlag(PortTypes.Parallel)) ? "Parallel port." : "No relevant port.";
			context.Reply($"{batteries} {indicators} {ports} Serial number {(GameState.Current.SerialNumberHasVowel ? "has a vowel" : "has no vowel")} and is {(GameState.Current.SerialNumberIsOdd ? "odd" : "even")}.");
		} else {
			var batteries = GameState.Current.BatteryCount switch {
				0 => "No batteries.",
				1 => "1 battery in 1 holder.",
				_ => $"{GameState.Current.BatteryCount} batteries in {GameState.Current.BatteryHolderCount} {(GameState.Current.BatteryHolderCount == 1 ? "holder" : "holders")}."
			};
			var indicators = GameState.Current.Indicators.Count > 0
				? $"Indicators: {string.Join(", ", from i in GameState.Current.Indicators select $"{(i.IsLit ? "lit" : "unlit")} {NATO.Speak(i.Label)}")}."
				: "No indicators.";
			string ports;
			if (GameState.Current.PortPlates.Count > 0) {
				var emptyPlates = GameState.Current.PortPlates.Count(p => p == 0);
				var emptyPlatesDesc = emptyPlates switch { 0 => "", 1 => "; an empty port plate", _ => $"{emptyPlates} empty port plates" };
				var list = string.Join("; ", from p in GameState.Current.PortPlates where p != 0 select $"plate: {string.Join(' ', from t in GetPortTypes(p) select t switch { PortTypes.DviD => "DVI", PortTypes.PS2 => "PS2", PortTypes.RJ45 => "RJ45", PortTypes.StereoRca => "RCA", _ => t.ToString() })}");
				ports = $"Ports: {list}{emptyPlatesDesc}.";
			} else
				ports = "No ports.";
			var serial = NATO.Speak(GameState.Current.SerialNumber);
			context.Reply($"{batteries} {indicators} {ports} Serial number: {serial}.");
		}
		if (GameState.Current.FocusState == FocusState.Bomb)
			context.Reply("<reply>first module</reply><reply>vanilla modules</reply><reply>specific module…</reply>");
	}

	private static IEnumerable<PortTypes> GetPortTypes(PortTypes ports) {
		if (ports.HasFlag(PortTypes.DviD)) yield return PortTypes.DviD;
		if (ports.HasFlag(PortTypes.Parallel)) yield return PortTypes.Parallel;
		if (ports.HasFlag(PortTypes.PS2)) yield return PortTypes.PS2;
		if (ports.HasFlag(PortTypes.RJ45)) yield return PortTypes.RJ45;
		if (ports.HasFlag(PortTypes.Serial)) yield return PortTypes.Serial;
		if (ports.HasFlag(PortTypes.StereoRca)) yield return PortTypes.StereoRca;
	}

	#region Log templates
	
	[LoggerMessage(LogLevel.Warning, "Registering widget: {Name}")]
	private static partial void LogRegisteringWidget(ILogger logger, string name);
	
	#endregion
}
