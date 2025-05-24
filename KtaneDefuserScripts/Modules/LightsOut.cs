using System.Collections.Specialized;

namespace KtaneDefuserScripts.Modules;
[AimlInterface("LightsOut")]
internal class LightsOut() : ModuleScript<KtaneDefuserConnector.Components.LightsOut>(3, 3) {
	private static readonly Dictionary<int, BitVector32> CachedSolutions = [];

	public override string IndefiniteDescription => "Lights Out";
	public override PriorityCategory PriorityCategory => PriorityCategory.Needy;

	static LightsOut() {
		for (var i = 0; i < 512; i++) {
			var presses = new BitVector32(i);
			var state = new BitVector32 {
				[1 << 0] = presses[1 << 0] ^ presses[1 << 1] ^ presses[1 << 3],
				[1 << 1] = presses[1 << 0] ^ presses[1 << 1] ^ presses[1 << 2] ^ presses[1 << 4],
				[1 << 2] = presses[1 << 1] ^ presses[1 << 2] ^ presses[1 << 5],
				[1 << 3] = presses[1 << 0] ^ presses[1 << 3] ^ presses[1 << 4] ^ presses[1 << 6],
				[1 << 4] = presses[1 << 1] ^ presses[1 << 3] ^ presses[1 << 4] ^ presses[1 << 5] ^ presses[1 << 7],
				[1 << 5] = presses[1 << 2] ^ presses[1 << 4] ^ presses[1 << 5] ^ presses[1 << 8],
				[1 << 6] = presses[1 << 3] ^ presses[1 << 6] ^ presses[1 << 7],
				[1 << 7] = presses[1 << 4] ^ presses[1 << 6] ^ presses[1 << 7] ^ presses[1 << 8],
				[1 << 8] = presses[1 << 5] ^ presses[1 << 7] ^ presses[1 << 8]
			};
			if (CachedSolutions.TryGetValue(state.Data, out var oldSolution) && GetPressCount(oldSolution) <= GetPressCount(presses)) continue;
			CachedSolutions[state.Data] = presses;
		}
		return;

		static int GetPressCount(BitVector32 v) => Enumerable.Range(0, 9).Count(i => v[1 << i]);
	}

	protected internal override async void NeedyStateChanged(AimlAsyncContext context, NeedyState newState) {
		try {
			if (newState != NeedyState.Running) return;
			await Delay(25);
			using var interrupt = await ModuleInterruptAsync(context);

			var data = interrupt.Read(Reader);
			if (!data.Lights.Any(b => b)) return;

			var key = Enumerable.Range(0, 9).Where(i => data.Lights[i]).Aggregate(0, (a, i) => a | (1 << i));
			var solution = CachedSolutions[key];
			for (var i = 0; i < 9; i++) {
				if (!solution[1 << i]) continue;
				Interact(interrupt, i % 3, i / 3);
			}
		} catch (Exception ex) {
			LogException(ex);
		}
	}
}
