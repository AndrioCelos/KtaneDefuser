using BombDefuserConnector.DataTypes;

namespace BombDefuserScripts.Modules;
[AimlInterface("SimonSays")]
internal class SimonSays : ModuleScript<BombDefuserConnector.Components.SimonSays> {
	public override string IndefiniteDescription => "a Simon";

	private static readonly SimonColour?[] buttonMap = new SimonColour?[4];

	private bool readyToRead;
	private SimonColour highlight = SimonColour.Blue;
	private int stagesCleared;
	private readonly List<SimonColour> pattern = new(5);
	private SimonColour? currentColour;

	protected internal override void Initialise(AimlAsyncContext context) => GameState.Current.Strike += (_, _) => Array.Clear(buttonMap);  // A strike invalidates the whole mapping.

	protected internal override void Started(AimlAsyncContext context) => this.readyToRead = true;

	protected internal override async void ModuleSelected(AimlAsyncContext context) {
		if (this.readyToRead) {
			this.readyToRead = false;
			using var interrupt = await this.ModuleInterruptAsync(context);
			if (this.pattern.Count == 0) {
				var colour = await this.ReadLastColourAsync(interrupt);
				pattern.Add(colour);
			}
			await this.LoopAsync(interrupt);
		}
	}

	private async Task LoopAsync(Interrupt interrupt) {
		var buttons = new List<Button>();
		while (true) {
			var highlight = this.highlight;
			foreach (var patternColour in this.pattern) {
				var correctColour = buttonMap[(int) patternColour];
				if (correctColour is null) {
					this.currentColour = patternColour;
					interrupt.Context.Reply(patternColour.ToString());
					interrupt.Context.AddReplies("red", "yellow", "green", "blue");
					return;
				}
				if (highlight != correctColour) {
					highlight = correctColour.Value;
					buttons.Add(correctColour switch {
						SimonColour.Red => Button.Left,
						SimonColour.Yellow => Button.Right,
						SimonColour.Green => Button.Down,
						_ => Button.Up
					});
				}
				buttons.Add(Button.A);
			}
			this.highlight = highlight;
			var result = await interrupt.SubmitAsync(buttons);
			if (result != ModuleLightState.Strike) {
				this.stagesCleared++;
				if (result == ModuleLightState.Solved) return;

				await Delay(1);
				var colour = await this.ReadLastColourAsync(interrupt);
				pattern.Add(colour);
			}
			buttons.Clear();
		}
	}

	private async Task<SimonColour> ReadLastColourAsync(Interrupt interrupt) {
		var coloursSeen = 0;
		while (true) {
			var data = interrupt.Read(Reader);
			if (data.Colour is null) {
				await Delay(0.125);
				continue;
			}
			if (coloursSeen >= this.stagesCleared) {
				interrupt.Context.RequestProcess.Log(Aiml.LogLevel.Info, $"Read light: {data.Colour.Value}");
				return data.Colour.Value;
			}
			coloursSeen++;

			while (true) {
				await Delay(0.125);
				var data2 = interrupt.Read(Reader);
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
		using var interrupt = await CurrentModuleInterruptAsync(context);
		await script.LoopAsync(interrupt);
	}
}
