using System.Text;
using BombDefuserConnector.DataTypes;

namespace BombDefuserScripts.Modules;
[AimlInterface("SimonSays")]
internal class SimonSays : ModuleScript<BombDefuserConnector.Components.SimonSays> {
	public override string IndefiniteDescription => "a Simon";

	private static readonly SimonColour?[] buttonMap = new SimonColour?[4];

	private SimonColour highlight = SimonColour.Blue;
	private int stagesCleared;
	private readonly List<SimonColour> pattern = new(5);
	private SimonColour? currentColour;

	protected internal override async void ModuleSelected(AimlAsyncContext context) {
		using var interrupt = await Interrupt.EnterAsync(context);
		if (this.pattern.Count == 0) {
			var colour = await this.ReadLastColourAsync(interrupt);
			pattern.Add(colour);
		}
		await this.LoopAsync(interrupt);
	}

	private async Task LoopAsync(Interrupt interrupt) {
		var builder = new StringBuilder();
		while (true) {
			var highlight = this.highlight;
			foreach (var patternColour in this.pattern) {
				var correctColour = buttonMap[(int) patternColour];
				if (correctColour is null) {
					this.currentColour = patternColour;
					interrupt.Context.Reply(patternColour.ToString());
					return;
				}
				if (highlight != correctColour) {
					highlight = correctColour.Value;
					builder.Append(correctColour switch {
						SimonColour.Red => "left ",
						SimonColour.Yellow => "right ",
						SimonColour.Green => "down ",
						_ => "up "
					});
				}
				builder.Append("a ");
			}
			this.highlight = highlight;
			var result = await interrupt.SubmitAsync(builder.ToString());
			if (result == ModuleLightState.Strike) {
				// A strike invalidates the whole mapping.
				Array.Clear(buttonMap);
			} else {
				this.stagesCleared++;
				if (result == ModuleLightState.Solved) return;

				await AimlTasks.Delay(1);
				var colour = await this.ReadLastColourAsync(interrupt);
				pattern.Add(colour);
			}
			builder.Clear();
		}
	}

	private async Task<SimonColour> ReadLastColourAsync(Interrupt interrupt) {
		var coloursSeen = 0;
		while (true) {
			var data = ReadCurrent(GetProcessor());
			if (data.Colour is null) {
				await AimlTasks.Delay(0.125);
				continue;
			}
			if (coloursSeen >= this.stagesCleared) {
				interrupt.Context.RequestProcess.Log(Aiml.LogLevel.Info, $"Read light: {data.Colour.Value}");
				return data.Colour.Value;
			}
			coloursSeen++;

			while (true) {
				await AimlTasks.Delay(0.125);
				var data2 = ReadCurrent(GetProcessor());
				if (data2.Colour != data.Colour) break;
			}
		}
	}

	[AimlCategory("<set>SimonColour</set>"), AimlCategory("press <set>SimonColour</set>")]
	public static async Task Colour(AimlAsyncContext context, string colour) {
		var script = GameState.Current.CurrentScript<SimonSays>();
		if (script.currentColour is not SimonColour currentColour) return;
		buttonMap[(int) currentColour] = Enum.Parse<SimonColour>(colour, true);
		script.currentColour = null;
		using var interrupt = await Interrupt.EnterAsync(context);
		await script.LoopAsync(interrupt);
	}
}
