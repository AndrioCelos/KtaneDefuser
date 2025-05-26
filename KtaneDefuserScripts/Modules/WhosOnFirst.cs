namespace KtaneDefuserScripts.Modules;
[AimlInterface("WhosOnFirst")]
internal class WhosOnFirst() : ModuleScript<KtaneDefuserConnector.Components.WhosOnFirst>(2, 3) {
	public override string IndefiniteDescription => "Who's on First";

	private bool _readyToRead;
	private Phrase[] _keyLabels = new Phrase[6];

	protected internal override void Started(AimlAsyncContext context) => _readyToRead = true;

	protected internal override void ModuleSelected(Interrupt interrupt) {
		if (!_readyToRead) return;
		_readyToRead = false;
		Read(interrupt);
	}

	private async Task WaitRead(Interrupt interrupt) {
		await Delay(3);
		Read(interrupt);
	}

	private void Read(Interrupt interrupt) {
		var data = interrupt.Read(Reader);
		_keyLabels = data.Keys.Select(s => AimlInterface.TryParseSetEnum<Phrase>(s.Replace('’', '\''), out var p) ? p : throw new ArgumentException("Unknown button label")).ToArray();
		interrupt.Context.Reply(data.Display == ""
			? "The display is literally empty."
			: $"The display reads {PronouncePhrase(AimlInterface.TryParseSetEnum<Phrase>(data.Display.Replace('’', '\''), out var p) ? p : throw new ArgumentException("Unknown display"))}.");
		interrupt.Context.Reply("<reply>top left</reply><reply>top right</reply><reply>middle left</reply><reply>middle right</reply><reply>bottom left</reply><reply>bottom right</reply>");
	}

	private void ReadButton(AimlAsyncContext context, int keyIndex) {
		context.Reply(PronouncePhrase(_keyLabels[keyIndex]));
		context.Reply("<reply>press …</reply>");
	}

	[AimlCategory("press …")]
	public static string PressMenu() => "Please describe the label to press.";

	[AimlCategory("top left")]
	public static void ReadButton1(AimlAsyncContext context) => GameState.Current.CurrentScript<WhosOnFirst>().ReadButton(context, 0);
	[AimlCategory("top right")]
	public static void ReadButton2(AimlAsyncContext context) => GameState.Current.CurrentScript<WhosOnFirst>().ReadButton(context, 1);
	[AimlCategory("middle left")]
	public static void ReadButton3(AimlAsyncContext context) => GameState.Current.CurrentScript<WhosOnFirst>().ReadButton(context, 2);
	[AimlCategory("middle right")]
	public static void ReadButton4(AimlAsyncContext context) => GameState.Current.CurrentScript<WhosOnFirst>().ReadButton(context, 3);
	[AimlCategory("bottom left")]
	public static void ReadButton5(AimlAsyncContext context) => GameState.Current.CurrentScript<WhosOnFirst>().ReadButton(context, 4);
	[AimlCategory("bottom right")]
	public static void ReadButton6(AimlAsyncContext context) => GameState.Current.CurrentScript<WhosOnFirst>().ReadButton(context, 5);

	// This will be called from WhosOnFirst.aiml
	[AimlCategory("HandleButton <set>WhosOnFirstPhrase</set>")]
	public static async Task Press(AimlAsyncContext context, Phrase phrase) {
		var script = GameState.Current.CurrentScript<WhosOnFirst>();
		var index = Array.IndexOf(script._keyLabels, phrase);
		if (index < 0)
			context.Reply("No.");
		else {
			context.Reply("Yes.");
			using var interrupt = await CurrentModuleInterruptAsync(context);
			await script.InteractWaitAsync(interrupt, index % 2, index / 2);
			var result = await interrupt.CheckStatusAsync();
			if (result != ModuleStatus.Solved) await script.WaitRead(interrupt);
		}
	}

	private static string PronouncePhrase(Phrase phrase) => phrase switch {
		Phrase.Empty => "literally empty",
		Phrase.BLANK => "<speak><sub alias=\"word blank\">'BLANK'</sub></speak>",
		Phrase.C => "<speak><sub alias=\"letter C\">'C'</sub></speak>",
		Phrase.CEE => "<speak><sub alias=\"Cee Spain\">'CEE'</sub></speak>",
		Phrase.DISPLAY => "<speak><sub alias=\"display\">'DISPLAY'</sub></speak>",
		Phrase.DONE => "<speak><sub alias=\"done\">'DONE'</sub></speak>",
		Phrase.FIRST => "<speak><sub alias=\"first\">'FIRST'</sub></speak>",
		Phrase.HOLD => "<speak><sub alias=\"hold\">'HOLD'</sub></speak>",
		Phrase.HOLD_ON => "<speak><sub alias=\"hold on\">'HOLD ON'</sub></speak>",
		Phrase.LEAD => "<speak><sub alias=\"lead with alfa\">'LEAD'</sub></speak>",
		Phrase.LED => "<speak><sub alias=\"led 3 letters\">'LED'</sub></speak>",
		Phrase.LEED => "<speak><sub alias=\"leed language\">'LEED'</sub></speak>",
		Phrase.LEFT => "<speak><sub alias=\"left\">'LEFT'</sub></speak>",
		Phrase.LIKE => "<speak><sub alias=\"like\">'LIKE'</sub></speak>",
		Phrase.MIDDLE => "<speak><sub alias=\"middle\">'MIDDLE'</sub></speak>",
		Phrase.NEXT => "<speak><sub alias=\"next\">'NEXT'</sub></speak>",
		Phrase.NO => "<speak><sub alias=\"no\">'NO'</sub></speak>",
		Phrase.NOTHING => "<speak><sub alias=\"nothing\">'NOTHING'</sub></speak>",
		Phrase.OKAY => "<speak><sub alias=\"okay\">'OKAY'</sub></speak>",
		Phrase.PRESS => "<speak><sub alias=\"press\">'PRESS'</sub></speak>",
		Phrase.READ => "<speak><sub alias=\"read with alfa\">'READ'</sub></speak>",
		Phrase.READY => "<speak><sub alias=\"ready\">'READY'</sub></speak>",
		Phrase.RED => "<speak><sub alias=\"red 3 letters\">'RED'</sub></speak>",
		Phrase.REED => "<speak><sub alias=\"reed with two 'E's\">'REED'</sub></speak>",
		Phrase.RIGHT => "<speak><sub alias=\"right\">'RIGHT'</sub></speak>",
		Phrase.SAYS => "<speak><sub alias=\"says\">'SAYS'</sub></speak>",
		Phrase.SEE => "<speak><sub alias=\"see a movie\">'SEE'</sub></speak>",
		Phrase.SURE => "<speak><sub alias=\"sure\">'SURE'</sub></speak>",
		Phrase.THEIR => "<speak><sub alias=\"their with india\">'THEIR'</sub></speak>",
		Phrase.THERE => "<speak><sub alias=\"there with two 'E's\">'THERE'</sub></speak>",
		Phrase.THEY_RE => "<speak><sub alias=\"they're with apostrophe\">'THEY'RE'</sub></speak>",
		Phrase.THEY_ARE => "<speak><sub alias=\"they are words\">'THEY ARE'</sub></speak>",
		Phrase.U => "<speak><sub alias=\"letter U\">'U'</sub></speak>",
		Phrase.UH_HUH => "<speak><sub alias=\"uh huh positive\">'UH HUH'</sub></speak>",
		Phrase.UH_UH => "<speak><sub alias=\"uh uh negative\">'UH UH'</sub></speak>",
		Phrase.UHHH => "<speak><sub alias=\"U triple H\">'UHHH'</sub></speak>",
		Phrase.UR => "<speak><sub alias=\"U R letters\">'UR'</sub></speak>",
		Phrase.WAIT => "<speak><sub alias=\"wait\">'WAIT'</sub></speak>",
		Phrase.WHAT => "<speak><sub alias=\"what no question\">'WHAT'</sub></speak>",
		Phrase.WHATQ => "<speak><sub alias=\"what question mark\">'WHAT?'</sub></speak>",
		Phrase.YES => "<speak><sub alias=\"yes\">'YES'</sub></speak>",
		Phrase.YOU => "<speak><sub alias=\"word you\">'YOU'</sub></speak>",
		Phrase.YOU_ARE => "<speak><sub alias=\"you are words\">'YOU ARE'</sub></speak>",
		Phrase.YOU_RE => "<speak><sub alias=\"you're with apostrophe\">'YOU'RE'</sub></speak>",
		Phrase.YOUR => "<speak><sub alias=\"your possessive\">'YOUR'</sub></speak>",
		_ => "unknown"
	};

	[AimlSet("WhosOnFirstPhrase")]
	internal enum Phrase {
		[AimlSetItem("empty"), AimlSetItem("nil"), AimlSetItem("literally empty"), AimlSetItem("literally blank"), AimlSetItem("literally nothing"), AimlSetItem("literal blank"), AimlSetItem("literal nothing")]
		Empty,
		[AimlSetItem("word blank"), AimlSetItem("blank word")]
		BLANK,
		[AimlSetItem("c"), AimlSetItem("letter c"), AimlSetItem("letter sea"), AimlSetItem("letter see"), AimlSetItem("c letter"), AimlSetItem("see letter"), AimlSetItem("sea letter")]
		C,
		[AimlSetItem("cee"), AimlSetItem("Cee Spain"), AimlSetItem("c Spain"), AimlSetItem("see Spain"), AimlSetItem("c e e")]
		CEE,
		DISPLAY,
		DONE,
		FIRST,
		HOLD,
		[AimlSetItem("hold on")]
		HOLD_ON,
		[AimlSetItem("lead"), AimlSetItem("lead guitar"), AimlSetItem("l e a d"), AimlSetItem("lead a"), AimlSetItem("lead alfa"), AimlSetItem("lead alpha"), AimlSetItem("led a"), AimlSetItem("led alfa"), AimlSetItem("led alpha")]
		LEAD,
		[AimlSetItem("led"), AimlSetItem("led no a"), AimlSetItem("led no alfa"), AimlSetItem("led no alpha"), AimlSetItem("lead no a"), AimlSetItem("lead no alfa"), AimlSetItem("lead no alpha"), AimlSetItem("l e d")]
		LED,
		[AimlSetItem("leed"), AimlSetItem("leed language"), AimlSetItem("leed two E"), AimlSetItem("leed two Es"), AimlSetItem("lead language"), AimlSetItem("lead two E"), AimlSetItem("lead two Es"), AimlSetItem("Leeds England"), AimlSetItem("l double e d"), AimlSetItem("lima double echo delta"), AimlSetItem("l e e d")]
		LEED,
		LEFT,
		LIKE,
		MIDDLE,
		NEXT,
		NO,
		[AimlSetItem("word nothing"), AimlSetItem("nothing word")]
		NOTHING,
		OKAY,
		PRESS,
		[AimlSetItem("read"), AimlSetItem("read a book"), AimlSetItem("r e a d"), AimlSetItem("read a"), AimlSetItem("read alfa"), AimlSetItem("read alpha"), AimlSetItem("red a"), AimlSetItem("red alfa"), AimlSetItem("red alpha"), AimlSetItem("reed a"), AimlSetItem("reed alfa"), AimlSetItem("reed alpha")]
		READ,
		READY,
		[AimlSetItem("red"), AimlSetItem("red colour"), AimlSetItem("colour red"), AimlSetItem("red color"), AimlSetItem("color red"), AimlSetItem("read colour"), AimlSetItem("colour read"), AimlSetItem("read color"), AimlSetItem("color read"), AimlSetItem("r e d"), AimlSetItem("red 3 letters"), AimlSetItem("red three letters"), AimlSetItem("read 3 letters"), AimlSetItem("read three letters")]
		RED,
		[AimlSetItem("reed"), AimlSetItem("reed instrument")]
		REED,
		RIGHT,
		SAYS,
		[AimlSetItem("see"), AimlSetItem("s e e"), AimlSetItem("see a movie")]
		SEE,
		SURE,
		[AimlSetItem("their"), AimlSetItem("their possessive"), AimlSetItem("their with I"), AimlSetItem("t h e i r")]
		THEIR,
		[AimlSetItem("there"), AimlSetItem("there location"), AimlSetItem("there two E"), AimlSetItem("there two Es")]
		THERE,
		[AimlSetItem("they're"), AimlSetItem("they're apostrophe"), AimlSetItem("they're contraction"), AimlSetItem("they are apostrophe"), AimlSetItem("they are contraction")]
		THEY_RE,
		[AimlSetItem("they are"), AimlSetItem("they are no apostrophe"), AimlSetItem("they are words"), AimlSetItem("they are two words"), AimlSetItem("theyare")]
		THEY_ARE,
		[AimlSetItem("letter u"), AimlSetItem("u letter"), AimlSetItem("letter uniform"), AimlSetItem("uniform letter")]
		U,
		[AimlSetItem("uh huh"), AimlSetItem("uh huh positive"), AimlSetItem("uhhuh"), AimlSetItem("uh huh 5 letters"), AimlSetItem("uh huh five letters")]
		UH_HUH,
		[AimlSetItem("uh uh"), AimlSetItem("uh uh negative"), AimlSetItem("uhuh"), AimlSetItem("uh uh 4 letters"), AimlSetItem("uh uh four letters")]
		UH_UH,
		[AimlSetItem("uhhh"), AimlSetItem("u triple h"), AimlSetItem("uniform triple hotel")]
		UHHH,
		[AimlSetItem("ur"), AimlSetItem("u r letters"), AimlSetItem("uniform romeo letters"), AimlSetItem("u r two letters")]
		UR,
		WAIT,
		[AimlSetItem("what"), AimlSetItem("what no question mark"), AimlSetItem("what no question")]
		WHAT,
		[AimlSetItem("what?"), AimlSetItem("what question mark"), AimlSetItem("what question")]
		WHATQ,
		YES,
		[AimlSetItem("you"), AimlSetItem("word you"), AimlSetItem("you word")]
		YOU,
		[AimlSetItem("you are"), AimlSetItem("youare"), AimlSetItem("you are words"), AimlSetItem("you are two words")]
		YOU_ARE,
		[AimlSetItem("you're"), AimlSetItem("youre"), AimlSetItem("you're contraction"), AimlSetItem("you're apostrophe"), AimlSetItem("you're with apostrophe"), AimlSetItem("you're 5 letters"), AimlSetItem("you're five letters"), AimlSetItem("you are contraction"), AimlSetItem("you are apostrophe"), AimlSetItem("you are with apostrophe"), AimlSetItem("you are 5 letters"), AimlSetItem("you are five letters")]
		YOU_RE,
		[AimlSetItem("your"), AimlSetItem("your possessive"), AimlSetItem("your word"), AimlSetItem("word your"), AimlSetItem("your no apostrophe"), AimlSetItem("your 4 letters"), AimlSetItem("your four letters")]
		YOUR
	}
}
