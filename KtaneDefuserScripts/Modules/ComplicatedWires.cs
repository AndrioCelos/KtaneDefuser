using static KtaneDefuserConnector.Components.ComplicatedWires;

namespace KtaneDefuserScripts.Modules;
[AimlInterface("ComplicatedWires")]
internal partial class ComplicatedWires() : ModuleScript<KtaneDefuserConnector.Components.ComplicatedWires>(6, 1) {
	private static readonly bool?[] ShouldCut = new bool?[16];

	public override string IndefiniteDescription => "Complicated Wires";

	private bool _readyToRead;
	private (WireFlags flags, bool isCut)[]? _wires;
	private WireFlags? _currentFlags;

	protected internal override void Started(AimlAsyncContext context) => _readyToRead = true;

	protected internal override async void ModuleSelected(Interrupt interrupt) {
		try {
			if (!_readyToRead) return;
			_readyToRead = false;
			using var interrupt2 = await CurrentModuleInterruptAsync(interrupt.Context);
			await Read(interrupt2);
		} catch (Exception ex) {
			LogException(ex);
		}
	}

	[AimlCategory("read")]
	internal static async Task Read(AimlAsyncContext context) {
		using var interrupt = await CurrentModuleInterruptAsync(context);
		var script = GameState.Current.CurrentScript<ComplicatedWires>();
		await script.Read(interrupt);
	}
	private async Task Read(Interrupt interrupt) {
		var data = interrupt.Read(Reader);
		LogWires($"[{string.Join("] [", data.Wires)}]");
		_wires ??= (from w in data.Wires select (w, false)).ToArray();
		SelectableSize = new(_wires.Length, 1);
		await FindWiresToCut(interrupt);
	}

	private async Task FindWiresToCut(Interrupt interrupt) {
		while (true) {
			// Find any wires we already know should be cut.
			// TODO: Clear this data when starting a new bomb.
			var toCut = new List<int>();
			WireFlags? firstUnknown = null;
			var allUnknownAreSame = true;
			for (var i = 0; i < _wires!.Length; i++) {
				if (_wires[i].isCut) continue;
				var cut = ShouldCut[(int) _wires[i].flags];
				switch (cut) {
					case true:
						toCut.Add(i);
						break;
					case null when firstUnknown is not null && firstUnknown != _wires[i].flags:
						allUnknownAreSame = false;
						break;
					case null:
						firstUnknown ??= _wires[i].flags;
						break;
				}
			}

			if (toCut.Count == 0 && firstUnknown != null && allUnknownAreSame) {
				// If we don't see any wires that need to be cut, but there's only one wire we still don't know about or multiple that are identical, cut those wires.
				ShouldCut[(int) firstUnknown.Value] = true;
				toCut.AddRange(Enumerable.Range(0, _wires.Length).Where(i => _wires[i].flags == firstUnknown.Value));
			}
			if (toCut.Count > 0) {
				foreach (var wireIndex in toCut) {
					LogCuttingWire(wireIndex + 1);
					await InteractWaitAsync(interrupt, wireIndex, 0);
					_wires[wireIndex].isCut = true;
					if (!interrupt.HasStrikeOccurred) continue;
					ShouldCut[(int) _wires[wireIndex].flags] = false;
					await FindWiresToCut(interrupt);
					return;
				}

				// Check whether the module is disarmed. If not, check again for more wires to cut or ask about.
				var result = await interrupt.CheckStatusAsync();
				if (result != ModuleStatus.Off) return;
			} else if (firstUnknown != null) {
				_currentFlags = firstUnknown.Value;
				interrupt.Context.Reply(firstUnknown == 0 ? "plain white" : $"{(firstUnknown.Value.HasFlag(WireFlags.Red) ? "red " : "")}{(firstUnknown.Value.HasFlag(WireFlags.Blue) ? "blue " : "")}{(firstUnknown.Value.HasFlag(WireFlags.Star) ? "star " : "")}{(firstUnknown.Value.HasFlag(WireFlags.Light) ? "light " : "")}.");
				interrupt.Context.Reply("<reply>cut</reply><reply>don't cut</reply>");
				return;
			} else {
				// Uh oh, we thought we knew all remaining wires shouldn't be cut, but the module isn't solved.
				foreach (var (flags, isCut) in _wires) {
					if (!isCut)
						ShouldCut[(int) flags] = null;
				}
			}
		}
	}

	[AimlCategory("cut ^")]
	internal static async Task Cut(AimlAsyncContext context) {
		var script = GameState.Current.CurrentScript<ComplicatedWires>();
		if (script._currentFlags == null) return;
		ShouldCut[(int) script._currentFlags] = true;
		using var interrupt = await CurrentModuleInterruptAsync(context);
		await script.FindWiresToCut(interrupt);
	}

	[AimlCategory("don't cut ^")]
	[AimlCategory("do not cut ^")]
	[AimlCategory("skip")]
	[AimlCategory("next")]
	internal static async Task DoNotCut(AimlAsyncContext context) {
		var script = GameState.Current.CurrentScript<ComplicatedWires>();
		if (script._currentFlags == null) return;
		ShouldCut[(int) script._currentFlags] = false;
		using var interrupt = await CurrentModuleInterruptAsync(context);
		await script.FindWiresToCut(interrupt);
	}
	
	#region Log templates
	
	[LoggerMessage(LogLevel.Information, "{Wires}")]
	private partial void LogWires(string wires);
	
	[LoggerMessage(LogLevel.Information, "Cutting wire {Number}")]
	private partial void LogCuttingWire(int number);
	
	#endregion
}
