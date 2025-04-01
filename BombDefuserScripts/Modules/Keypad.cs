using static BombDefuserConnector.Components.Keypad;

namespace BombDefuserScripts.Modules;
[AimlInterface("Keypad")]
internal class Keypad : ModuleScript<BombDefuserConnector.Components.Keypad> {
	public static Dictionary<Symbol, string> SymbolDescriptions { get; } = new() {
		{ Symbol.Copyright   , "copyright symbol" },
		{ Symbol.FilledStar  , "filled star" },
		{ Symbol.HollowStar  , "hollow star" },
		{ Symbol.SmileyFace  , "smiley face" },
		{ Symbol.DoubleK     , "double K" },
		{ Symbol.Omega       , "Omega" },
		{ Symbol.SquidKnife  , "squid with a knife" },
		{ Symbol.Pumpkin     , "pumpkin" },
		{ Symbol.HookN       , "curly H" },
		{ Symbol.Teepee      , "tent" },
		{ Symbol.Six         , "flat-topped six" },
		{ Symbol.SquigglyN   , "lightning bolt" },
		{ Symbol.AT          , "pyramid" },
		{ Symbol.Ae          , "a e" },
		{ Symbol.MeltedThree , "melted three" },
		{ Symbol.Euro        , "backward E" },
		{ Symbol.Circle      , "circle of hills" },
		{ Symbol.NWithHat    , "upside-down N with a hat" },
		{ Symbol.Dragon      , "dragon" },
		{ Symbol.QuestionMark, "question mark" },
		{ Symbol.Paragraph   , "paragraph mark" },
		{ Symbol.RightC      , "forward C" },
		{ Symbol.LeftC       , "backward C" },
		{ Symbol.Trident     , "trident" },
		{ Symbol.Cursive     , "cursive loop" },
		{ Symbol.Tracks      , "square train track" },
		{ Symbol.Balloon     , "balloon" },
		{ Symbol.WeirdNose   , "zeta" },
		{ Symbol.UpsideDownY , "lambda" },
		{ Symbol.BT          , "B and T" }
	};

	public override string IndefiniteDescription => "a Keypad";

	private readonly bool[] pressed = new bool[4];
	private Symbol[]? symbols;
	private int highlight;
	protected internal override void Started(AimlAsyncContext context) => context.AddReply("ready");

	[AimlCategory("read")]
	internal static async Task Read(AimlAsyncContext context) {
		using var interrupt = await CurrentModuleInterruptAsync(context);
		var data = interrupt.Read(Reader);
		GameState.Current.CurrentScript<Keypad>().symbols = data.Symbols;
		interrupt.Context.Reply(string.Join(", ", from s in data.Symbols select SymbolDescriptions[s]));
		interrupt.Context.AddReplies(from s in data.Symbols select new AngelAiml.Media.Reply(SymbolDescriptions[s], $"press {SymbolDescriptions[s]}"));
	}

	[AimlCategory("press *")]
	internal static async Task Press(AimlAsyncContext context, string keys) {
		var script = GameState.Current.CurrentScript<Keypad>();
		var symbols = GameState.Current.CurrentScript<Keypad>().symbols;
		if (symbols == null) {
			context.Reply("We need to read the module first.");
			return;
		}
		var presses = new List<(IEnumerable<Button> inputs, int index)>();
		var cursorIndex = script.highlight;
		var descriptions = keys.Split(" then ", StringSplitOptions.TrimEntries);
		foreach (var desc in descriptions) {
			var symbol = Enum.Parse<Symbol>(context.RequestProcess.Srai($"GetKeypadGlyphName {desc}"), true);
			var index = Array.IndexOf(symbols, symbol);
			if (index < 0) {
				context.Reply($"{SymbolDescriptions[symbol]} is not on the keypad.");
				return;
			}
			var buttons = new List<Button>();
			if (index / 2 < cursorIndex / 2)
				buttons.Add(Button.Up);
			else if (index / 2 > cursorIndex / 2)
				buttons.Add(Button.Down);
			if (index % 2 < cursorIndex % 2)
				buttons.Add(Button.Left);
			else if (index % 2 > cursorIndex % 2)
				buttons.Add(Button.Right);
			buttons.Add(Button.A);
			presses.Add((buttons, index));
			cursorIndex = index;
		}
		using var interrupt = await Interrupt.EnterAsync(context);
		foreach (var (inputs, index) in presses) {
			script.highlight = index;
			var result = await interrupt.SubmitAsync(inputs);
			if (result != ModuleLightState.Strike) script.pressed[index] = true;
			if (result != ModuleLightState.Off) return;
		}
		if (script.symbols is null) return;
		for (var i = 0; i < 4; i++) {
			if (!script.pressed[i]) interrupt.Context.AddReply(SymbolDescriptions[script.symbols[i]], $"press {SymbolDescriptions[script.symbols[i]]}");
		}
	}
}
