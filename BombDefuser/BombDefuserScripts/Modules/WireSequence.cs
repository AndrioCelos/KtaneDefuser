using System.Text;
using static BombDefuserConnector.Components.WireSequence;

namespace BombDefuserScripts.Modules;
[AimlInterface("WireSequence")]
internal class WireSequence : ModuleScript<BombDefuserConnector.Components.WireSequence> {
	public override string IndefiniteDescription => "a Wire Sequence";
	
	private readonly int[] wireCounts = new int[3];
	private WireColour?[] currentPageColours = new WireColour?[3];
	private int highlight = -1;  // For this script, -1 => previous button, 0~2 => wire slots, 3 => next button, < -1 => unknown

	protected internal override async void ModuleSelected(AimlAsyncContext context) {
		using var interrupt = await Interrupt.EnterAsync(context);
		// The highlight starts on the previous button, so move down first.
		if (this.highlight == -1) {
			await interrupt.Context.SendInputsAsync("down");
			this.highlight = -2;
		}
		await this.ContinuePageAsync(interrupt);
	}

	private async Task ContinuePageAsync(Interrupt interrupt) {
		var data = await this.ReadAsync(interrupt);
		if (data.HighlightedButton > 0) {
			// If we've reached the next button, or there are no wires on the first page, go ahead and switch to the next page.
			await this.MoveToNextPageAsync(interrupt);
		} else
			this.ReadCurrentWire(interrupt, data);
	}

	private void ReadCurrentWire(Interrupt interrupt, ReadData data) {
		if (data.HighlightedWire is not null) {
			var colour = data.WireColours[data.HighlightedWire.From]!;
			ref var count = ref this.wireCounts[(int) colour];
			count++;
			interrupt.Context.Reply($"{Utils.ToOrdinal(count)} {colour} to {NATO.Speak(new[] { data.HighlightedWire.To })}");
		}
	}

	private async Task MoveToNextPageAsync(Interrupt interrupt) {
		var result = await interrupt.SubmitAsync("a");
		switch (result) {
			case ModuleLightState.Solved:
			case ModuleLightState.Strike:
				return;
			default:
				await AimlTasks.Delay(2);
				await this.ReadAsync(interrupt);
				var i = Array.FindIndex(this.currentPageColours, c => c is not null);
				if (i < 0) {
					// It's possible for a page to not contain any wires. In this case, just move to the next page.
					await this.MoveToNextPageAsync(interrupt);
				} else {
					// Highlight the first wire and read it.
					var builder = new StringBuilder();
					for (var n = this.currentPageColours.Count(c => c is not null); n > 0; n--) {
						builder.Append("up ");
					}
					this.highlight = i;
					await interrupt.Context.SendInputsAsync(builder.ToString());
					await this.ContinuePageAsync(interrupt);
				}
				break;
		}
	}

	private async Task<ReadData> ReadAsync(Interrupt interrupt) {
		var data = ReadCurrent(GetReader());
		interrupt.Context.RequestProcess.Log(Aiml.LogLevel.Info, $"Highlighted button: {data.HighlightedButton}; wires: {string.Join(", ", data.WireColours)}");
		currentPageColours = data.WireColours;
		switch (data.HighlightedButton) {
			case -1: this.highlight = -1; break;
			case 1: this.highlight = 3; break;
			default:
				while (data.HighlightedWire is null) {
					// Currently we are only able to read the highlighted wire, and only when the selection highlight is within a strict range of intensities.
					// Keep looking at the module until that condition is met.
					interrupt.Context.RequestProcess.Log(Aiml.LogLevel.Info, $"Reading highlighted wire...");
					await AimlTasks.Delay(0.05);
					data = ReadCurrent(GetReader());
				}
				interrupt.Context.RequestProcess.Log(Aiml.LogLevel.Info, $"Highlighted wire: {data.HighlightedWire}");
				this.highlight = data.HighlightedWire.From;
				break;
		}
		return data;
	}

	private async Task ActionAsync(AimlAsyncContext context, bool cut) {
		using var interrupt = await Interrupt.EnterAsync(context);
		if (cut)
			await interrupt.SubmitAsync("a down");
		else
			await interrupt.Context.SendInputsAsync("down");
		do { this.highlight++; } while (this.highlight < 3 && this.currentPageColours[this.highlight] is null);
		await this.ContinuePageAsync(interrupt);
	}

	[AimlCategory("cut ^")]
	internal static Task Cut(AimlAsyncContext context) => GameState.Current.CurrentScript<WireSequence>().ActionAsync(context, true);

	[AimlCategory("don't cut ^")]
	[AimlCategory("do not cut ^")]
	[AimlCategory("skip")]
	[AimlCategory("next")]
	internal static Task DoNotCut(AimlAsyncContext context) => GameState.Current.CurrentScript<WireSequence>().ActionAsync(context, false);
}
