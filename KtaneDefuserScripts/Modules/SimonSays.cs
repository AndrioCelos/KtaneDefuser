using KtaneDefuserConnector.DataTypes;

namespace KtaneDefuserScripts.Modules;
[AimlInterface("SimonSays")]
internal partial class SimonSays : ModuleScript<KtaneDefuserConnector.Components.SimonSays> {
	public override string IndefiniteDescription => "a Simon";

	private static readonly SimonColour?[] ButtonMap = new SimonColour?[4];

	private bool _readyToRead;
	private SimonColour _highlight = SimonColour.Blue;
	private int _stagesCleared;
	private readonly List<SimonColour> _pattern = new(5);
	private SimonColour? _currentColour;

	protected internal override void Initialise(Interrupt interrupt) => GameState.Current.Strike += (_, _) => Array.Clear(ButtonMap);  // A strike invalidates the whole mapping.

	protected internal override void Started(AimlAsyncContext context) => _readyToRead = true;

	protected internal override async void ModuleSelected(Interrupt interrupt) {
		try {
			if (!_readyToRead) return;
			_readyToRead = false;
			if (_pattern.Count == 0) {
				var colour = await ReadLastColourAsync(interrupt);
				_pattern.Add(colour);
			}

			await LoopAsync(interrupt);
		} catch (Exception ex) {
			LogException(ex);
		}
	}

	private async Task LoopAsync(Interrupt interrupt) {
		var buttons = new List<Button>();
		while (true) {
			var highlight = _highlight;
			foreach (var patternColour in _pattern) {
				var correctColour = ButtonMap[(int) patternColour];
				if (correctColour is null) {
					_currentColour = patternColour;
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
			_highlight = highlight;
			var result = await interrupt.SubmitAsync(buttons);
			if (result != ModuleLightState.Strike) {
				_stagesCleared++;
				if (result == ModuleLightState.Solved) return;

				await Delay(1);
				var colour = await ReadLastColourAsync(interrupt);
				_pattern.Add(colour);
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
			if (coloursSeen >= _stagesCleared) {
				LogLight(data.Colour.Value);
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
		if (script._currentColour is not { } currentColour) return;
		ButtonMap[(int) currentColour] = Enum.Parse<SimonColour>(colour, true);
		script._currentColour = null;
		using var interrupt = await CurrentModuleInterruptAsync(context);
		await script.LoopAsync(interrupt);
	}
	
	#region Log templates
	
	[LoggerMessage(LogLevel.Information, "Read light: {Colour}")]
	private partial void LogLight(SimonColour colour);

	#endregion
}
