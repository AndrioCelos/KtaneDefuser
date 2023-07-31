using System.Text;
using static BombDefuserConnector.Components.ComplicatedWires;

namespace BombDefuserScripts.Modules;
[AimlInterface("ComplicatedWires")]
internal class ComplicatedWires : ModuleScript<BombDefuserConnector.Components.ComplicatedWires> {
	public override string IndefiniteDescription => "Complicated Wires";

	private static readonly bool?[] shouldCut = new bool?[16];
	private (WireFlags flags, bool isCut)[]? wires;
	private WireFlags? currentFlags;

	public override async void Enter(AimlAsyncContext context) {
		await AimlTasks.Delay(1);
		Read(context);
	}

	[AimlCategory("read")]
	internal static async void Read(AimlAsyncContext context) {
		var data = await ReadCurrentAsync(GetProcessor());
		context.RequestProcess.Log(Aiml.LogLevel.Info, $"Complicated wires: [{string.Join("] [", data.Wires)}]");
		var module = GameState.Current.SelectedModule!;
		var script = GameState.Current.CurrentScript<ComplicatedWires>();
		script.wires ??= (from w in data.Wires select (w, false)).ToArray();
		if (data.CurrentWire is not null) module.X = data.CurrentWire.Value;
		await script.FindWiresToCut(context);
	}

	private async Task FindWiresToCut(AimlAsyncContext context) {
		var module = GameState.Current.SelectedModule!;

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
				foreach (var i in toCut) {
					var builder = new StringBuilder();
					context.RequestProcess.Log(Aiml.LogLevel.Info, $"Cutting wire {i + 1}");
					while (module.X < i) {
						builder.Append("right ");
						module.X++;
					}
					while (module.X > i) {
						builder.Append("left ");
						module.X--;
					}
					builder.Append('a');
					this.wires[i].isCut = true;
					var result = await interrupt.SubmitAsync(builder.ToString());
					if (result == ModuleLightState.Strike) {
						shouldCut[(int) this.wires[i].flags] = false;
						break;
					} else if (result == ModuleLightState.Solved)
						return;
				}
			} else if (firstUnknown != null) {
				currentFlags = firstUnknown.Value;
				context.Reply(firstUnknown == 0 ? "plain white" : $"{(firstUnknown.Value.HasFlag(WireFlags.Red) ? "red " : "")}{(firstUnknown.Value.HasFlag(WireFlags.Blue) ? "blue " : "")}{(firstUnknown.Value.HasFlag(WireFlags.Star) ? "star " : "")}{(firstUnknown.Value.HasFlag(WireFlags.Light) ? "light " : "")}.");
				context.Reply("<reply>cut</reply><reply>do not cut</reply>");
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

	[AimlCategory("cut")]
	internal static async Task Cut(AimlAsyncContext context) {
		var script = GameState.Current.CurrentScript<ComplicatedWires>();
		if (script.currentFlags == null) return;
		shouldCut[(int) script.currentFlags] = true;
		await script.FindWiresToCut(context);
	}

	[AimlCategory("do not cut")]
	[AimlCategory("skip")]
	[AimlCategory("next")]
	internal static async Task DoNotCut(AimlAsyncContext context) {
		var script = GameState.Current.CurrentScript<ComplicatedWires>();
		if (script.currentFlags == null) return;
		shouldCut[(int) script.currentFlags] = false;
		await script.FindWiresToCut(context);
	}
}
