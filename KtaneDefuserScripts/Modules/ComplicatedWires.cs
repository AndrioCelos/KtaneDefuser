﻿using KtaneDefuserConnector;
using KtaneDefuserConnectorApi;
using static KtaneDefuserConnector.Components.ComplicatedWires;

namespace KtaneDefuserScripts.Modules;
[AimlInterface("ComplicatedWires")]
internal partial class ComplicatedWires : ModuleScript<KtaneDefuserConnector.Components.ComplicatedWires> {
	public override string IndefiniteDescription => "Complicated Wires";

	private bool readyToRead;
	private static readonly bool?[] shouldCut = new bool?[16];
	private (WireFlags flags, bool isCut)[]? wires;
	private WireFlags? currentFlags;
	private int highlight;

	protected internal override void Started(AimlAsyncContext context) => this.readyToRead = true;

	protected internal override async void ModuleSelected(AimlAsyncContext context) {
		if (this.readyToRead) {
			this.readyToRead = false;
			await this.Read(context);
		}
	}

	[AimlCategory("read")]
	internal async Task Read(AimlAsyncContext context) {
		using var interrupt = await CurrentModuleInterruptAsync(context);
		var data = interrupt.Read(Reader);
		this.LogWires($"[{string.Join("] [", data.Wires)}]");
		var module = GameState.Current.SelectedModule!;
		var script = GameState.Current.CurrentScript<ComplicatedWires>();
		script.wires ??= (from w in data.Wires select (w, false)).ToArray();
		await script.FindWiresToCut(context, false);
	}

	private async Task FindWiresToCut(AimlAsyncContext context, bool fromUserInput) {
		while (true) {
			// Find any wires we already know should be cut.
			// TODO: Clear this data when starting a new bomb.
			var toCut = new List<int>();
			WireFlags? firstUnknown = null;
			var allUnknownAreSame = true;
			for (var i = 0; i < this.wires!.Length; i++) {
				if (this.wires[i].isCut) continue;
				var cut = shouldCut[(int) this.wires[i].flags];
				if (cut == true)
					toCut.Add(i);
				else if (cut == null) {
					if (firstUnknown != null && firstUnknown != this.wires[i].flags)
						allUnknownAreSame = false;
					else
						firstUnknown ??= this.wires[i].flags;
				}
			}

			if (toCut.Count == 0 && firstUnknown != null && allUnknownAreSame) {
				// If we don't see any wires that need to be cut, but there's only one wire we still don't know about or multiple that are identical, cut those wires.
				shouldCut[(int) firstUnknown.Value] = true;
				toCut.AddRange(Enumerable.Range(0, this.wires.Length).Where(i => this.wires[i].flags == firstUnknown.Value));
			}
			if (toCut.Count > 0) {
				using var interrupt = await Interrupt.EnterAsync(context);
				context = interrupt.Context;
				var buttons = new List<Button>();
				for (var i = 0; i < toCut.Count; i++) {
					var wireIndex = toCut[i];
					this.LogCuttingWire(wireIndex + 1);
					while (this.highlight < wireIndex) {
						buttons.Add(Button.Right);
						this.highlight++;
					}
					while (this.highlight > wireIndex) {
						buttons.Add(Button.Left);
						this.highlight--;
					}
					buttons.Add(Button.A);
					this.wires[wireIndex].isCut = true;
					if ((fromUserInput && i == 0) || i + 1 >= toCut.Count) {
						// Check whether this was a strike or solve. If in response to the user saying to cut a wire, check after the first wire. Otherwise, only check after all wires for speed.
						var result = await interrupt.SubmitAsync(buttons);
						buttons.Clear();
						if (result == ModuleLightState.Strike) {
							shouldCut[(int) this.wires[wireIndex].flags] = false;
							break;
						} else if (result == ModuleLightState.Solved)
							return;
					}
				}
			} else if (firstUnknown != null) {
				this.currentFlags = firstUnknown.Value;
				context.Reply(firstUnknown == 0 ? "plain white" : $"{(firstUnknown.Value.HasFlag(WireFlags.Red) ? "red " : "")}{(firstUnknown.Value.HasFlag(WireFlags.Blue) ? "blue " : "")}{(firstUnknown.Value.HasFlag(WireFlags.Star) ? "star " : "")}{(firstUnknown.Value.HasFlag(WireFlags.Light) ? "light " : "")}.");
				context.Reply("<reply>cut</reply><reply>don't cut</reply>");
				return;
			} else {
				// Uh oh, we thought we knew all remaining wires shouldn't be cut, but the module isn't solved.
				foreach (var (flags, isCut) in this.wires) {
					if (!isCut)
						shouldCut[(int) flags] = null;
				}
			}
		}
	}

	[AimlCategory("cut ^")]
	internal static async Task Cut(AimlAsyncContext context) {
		var script = GameState.Current.CurrentScript<ComplicatedWires>();
		if (script.currentFlags == null) return;
		shouldCut[(int) script.currentFlags] = true;
		await script.FindWiresToCut(context, true);
	}

	[AimlCategory("don't cut ^")]
	[AimlCategory("do not cut ^")]
	[AimlCategory("skip")]
	[AimlCategory("next")]
	internal static async Task DoNotCut(AimlAsyncContext context) {
		var script = GameState.Current.CurrentScript<ComplicatedWires>();
		if (script.currentFlags == null) return;
		shouldCut[(int) script.currentFlags] = false;
		await script.FindWiresToCut(context, false);
	}
	
	#region Log templates
	
	[LoggerMessage(LogLevel.Information, "{Wires}")]
	private partial void LogWires(string wires);
	
	[LoggerMessage(LogLevel.Information, "Cutting wire {Number}")]
	private partial void LogCuttingWire(int number);
	
	#endregion
}
