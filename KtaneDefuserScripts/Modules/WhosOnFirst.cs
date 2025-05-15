namespace KtaneDefuserScripts.Modules;
[AimlInterface("WhosOnFirst")]
internal class WhosOnFirst : ModuleScript<KtaneDefuserConnector.Components.WhosOnFirst> {
	public override string IndefiniteDescription => "Who's on First";

	private bool readyToRead;
	private Phrase[] keyLabels = new Phrase[6];
	private int highlight;

	protected internal override void Started(AimlAsyncContext context) => readyToRead = true;

	protected internal override void ModuleSelected(Interrupt interrupt) {
		if (!readyToRead) return;
		readyToRead = false;
		Read(interrupt);
	}

	private async Task WaitRead(Interrupt interrupt) {
		await Delay(3);
		Read(interrupt);
	}

	private void Read(Interrupt interrupt) {
		var data = interrupt.Read(Reader);
		keyLabels = data.Keys.Select(s => AimlInterface.TryParseSetEnum<Phrase>(s.Replace('’', '\''), out var p) ? p : throw new ArgumentException("Unknown button label")).ToArray();
		if (data.Display == "")
			interrupt.Context.Reply("The display is literally empty.");
		else
			interrupt.Context.Reply($"The display reads {PronouncePhrase(AimlInterface.TryParseSetEnum<Phrase>(data.Display.Replace('’', '\''), out var p) ? p : throw new ArgumentException("Unknown display"))}.");
		interrupt.Context.Reply("<reply>top left</reply><reply>top right</reply><reply>middle left</reply><reply>middle right</reply><reply>bottom left</reply><reply>bottom right</reply>");
	}

	public void ReadButton(AimlAsyncContext context, int keyIndex) {
		context.Reply(PronouncePhrase(keyLabels[keyIndex]));
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
		var index = Array.IndexOf(script.keyLabels, phrase);
		if (index < 0)
			context.Reply("No.");
		else {
			context.Reply("Yes.");
			await script.PressButtonAsync(context, index);
		}
	}

	private async Task PressButtonAsync(AimlAsyncContext context, int index) {
		var buttons = new List<Button>();
		var highlight = this.highlight;
		if (highlight % 2 == 0 && index % 2 == 1) {
			highlight++;
			buttons.Add(Button.Right);
		} else if (highlight % 2 == 1 && index % 2 == 0) {
			highlight--;
			buttons.Add(Button.Left);
		}
		while (highlight < index) {
			highlight += 2;
			buttons.Add(Button.Down);
		}
		while (highlight > index) {
			highlight -= 2;
			buttons.Add(Button.Up);
		}
		buttons.Add(Button.A);
		this.highlight = highlight;
		using var interrupt = await ModuleInterruptAsync(context);
		var result = await interrupt.SubmitAsync(buttons);
		if (result != ModuleLightState.Solved) await WaitRead(interrupt);
	}

	private static string PronouncePhrase(Phrase phrase) => phrase switch {
		Phrase.Empty => "literally empty",
		Phrase.BLANK => "<speak><s>word blank</s><alt>'BLANK'</alt></speak>",
		Phrase.C => "<speak><s>letter C</s><alt>'C'</alt></speak>",
		Phrase.CEE => "<speak><s>Cee Spain</s><alt>'CEE'</alt></speak>",
		Phrase.DISPLAY => "<speak><s>display</s><alt>'DISPLAY'</alt></speak>",
		Phrase.DONE => "<speak><s>done</s><alt>'DONE'</alt></speak>",
		Phrase.FIRST => "<speak><s>first</s><alt>'FIRST'</alt></speak>",
		Phrase.HOLD => "<speak><s>hold</s><alt>'HOLD'</alt></speak>",
		Phrase.HOLD_ON => "<speak><s>hold on</s><alt>'HOLD ON'</alt></speak>",
		Phrase.LEAD => "<speak><s>lead with alfa</s><alt>'LEAD'</alt></speak>",
		Phrase.LED => "<speak><s>led 3 letters</s><alt>'LED'</alt></speak>",
		Phrase.LEED => "<speak><s>leed language</s><alt>'LEED'</alt></speak>",
		Phrase.LEFT => "<speak><s>left</s><alt>'LEFT'</alt></speak>",
		Phrase.LIKE => "<speak><s>like</s><alt>'LIKE'</alt></speak>",
		Phrase.MIDDLE => "<speak><s>middle</s><alt>'MIDDLE'</alt></speak>",
		Phrase.NEXT => "<speak><s>next</s><alt>'NEXT'</alt></speak>",
		Phrase.NO => "<speak><s>no</s><alt>'NO'</alt></speak>",
		Phrase.NOTHING => "<speak><s>nothing</s><alt>'NOTHING'</alt></speak>",
		Phrase.OKAY => "<speak><s>okay</s><alt>'OKAY'</alt></speak>",
		Phrase.PRESS => "<speak><s>press</s><alt>'PRESS'</alt></speak>",
		Phrase.READ => "<speak><s>read with alfa</s><alt>'READ'</alt></speak>",
		Phrase.READY => "<speak><s>ready</s><alt>'READY'</alt></speak>",
		Phrase.RED => "<speak><s>red 3 letters</s><alt>'RED'</alt></speak>",
		Phrase.REED => "<speak><s>reed with two Es</s><alt>'REED'</alt></speak>",
		Phrase.RIGHT => "<speak><s>right</s><alt>'RIGHT'</alt></speak>",
		Phrase.SAYS => "<speak><s>says</s><alt>'SAYS'</alt></speak>",
		Phrase.SEE => "<speak><s>see a movie</s><alt>'SEE'</alt></speak>",
		Phrase.SURE => "<speak><s>sure</s><alt>'SURE'</alt></speak>",
		Phrase.THEIR => "<speak><s>their with india</s><alt>'THEIR'</alt></speak>",
		Phrase.THERE => "<speak><s>there with two Es</s><alt>'THERE'</alt></speak>",
		Phrase.THEY_RE => "<speak><s>they're with apostrophe</s><alt>'THEY'RE'</alt></speak>",
		Phrase.THEY_ARE => "<speak><s>they are words</s><alt>'THEY ARE'</alt></speak>",
		Phrase.U => "<speak><s>letter U</s><alt>'U'</alt></speak>",
		Phrase.UH_HUH => "<speak><s>uh huh positive</s><alt>'UH HUH'</alt></speak>",
		Phrase.UH_UH => "<speak><s>uh uh negative</s><alt>'UH UH'</alt></speak>",
		Phrase.UHHH => "<speak><s>U triple H</s><alt>'UHHH'</alt></speak>",
		Phrase.UR => "<speak><s>U R letters</s><alt>'UR'</alt></speak>",
		Phrase.WAIT => "<speak><s>wait</s><alt>'WAIT'</alt></speak>",
		Phrase.WHAT => "<speak><s>what no question</s><alt>'WHAT'</alt></speak>",
		Phrase.WHATQ => "<speak><s>what question mark</s><alt>'WHAT?'</alt></speak>",
		Phrase.YES => "<speak><s>yes</s><alt>'YES'</alt></speak>",
		Phrase.YOU => "<speak><s>word you</s><alt>'YOU'</alt></speak>",
		Phrase.YOU_ARE => "<speak><s>you are words</s><alt>'YOU ARE'</alt></speak>",
		Phrase.YOU_RE => "<speak><s>you're with apostrophe</s><alt>'YOU'RE'</alt></speak>",
		Phrase.YOUR => "<speak><s>your no apostrophe</s><alt>'YOUR'</alt></speak>",
		_ => "unknown",
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
		[AimlSetItem("your"), AimlSetItem("your word"), AimlSetItem("word your"), AimlSetItem("your 4 letters"), AimlSetItem("your four letters")]
		YOUR
	}
}
