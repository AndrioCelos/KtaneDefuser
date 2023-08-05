using System.ComponentModel.Design;
using System.Text;
using AimlCSharpInterface;

namespace BombDefuserScripts.Modules;
[AimlInterface("WhosOnFirst")]
internal class WhosOnFirst : ModuleScript<BombDefuserConnector.Components.WhosOnFirst> {
	public override string IndefiniteDescription => "Who's on First";

	private Phrase[] keyLabels = new Phrase[6];
	private int highlight;

	protected internal override void ModuleSelected(AimlAsyncContext context) => this.Read(context);

	private async Task WaitRead(AimlAsyncContext context) {
		await AimlTasks.Delay(4);
		this.Read(context);
	}

	private void Read(AimlAsyncContext context) {
		var data = ReadCurrent(Reader);
		this.keyLabels = data.Keys.Select(s => AimlInterface.TryParseSetEnum<Phrase>(s.Replace('’', '\''), out var p) ? p : throw new ArgumentException("Unknown button label")).ToArray();
		if (data.Display == "")
			context.Reply("The display is literally empty.");
		else
			context.Reply($"The display reads {PronouncePhrase(AimlInterface.TryParseSetEnum<Phrase>(data.Display.Replace('’', '\''), out var p) ? p : throw new ArgumentException("Unknown display"))}.");
		context.Reply("<reply>top left</reply><reply>top right</reply><reply>middle left</reply><reply>middle right</reply><reply>bottom left</reply><reply>bottom right</reply>");
	}

	public void ReadButton(AimlAsyncContext context, int keyIndex) {
		context.Reply(PronouncePhrase(this.keyLabels[keyIndex]));
		context.Reply("<reply>press …</reply>");
	}

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
		var builder = new StringBuilder();
		var highlight = this.highlight;
		if (highlight % 2 == 0 && index % 2 == 1) {
			highlight++;
			builder.Append("right ");
		} else if (highlight % 2 == 1 && index % 2 == 0) {
			highlight--;
			builder.Append("left ");
		}
		while (highlight < index) {
			highlight += 2;
			builder.Append("down ");
		}
		while (highlight > index) {
			highlight -= 2;
			builder.Append("up ");
		}
		builder.Append('a');
		this.highlight = highlight;
		using var interrupt = await Interrupt.EnterAsync(context);
		var result = await interrupt.SubmitAsync(builder.ToString());
		if (result != ModuleLightState.Solved) await this.WaitRead(interrupt.Context);
	}

	private static string PronouncePhrase(Phrase phrase) => phrase switch {
		Phrase.Empty => "literally empty",
		Phrase.BLANK => "<oob><speak><s>word blank</s></speak><alt>'BLANK'</alt></oob>",
		Phrase.C => "<oob><speak><s>letter C</s></speak><alt>'C'</alt></oob>",
		Phrase.CEE => "<oob><speak><s>Cee Spain</s></speak><alt>'CEE'</alt></oob>",
		Phrase.DISPLAY => "<oob><speak><s>display</s></speak><alt>'DISPLAY'</alt></oob>",
		Phrase.DONE => "<oob><speak><s>done</s></speak><alt>'DONE'</alt></oob>",
		Phrase.FIRST => "<oob><speak><s>first</s></speak><alt>'FIRST'</alt></oob>",
		Phrase.HOLD => "<oob><speak><s>hold</s></speak><alt>'HOLD'</alt></oob>",
		Phrase.HOLD_ON => "<oob><speak><s>hold on</s></speak><alt>'HOLD ON'</alt></oob>",
		Phrase.LEAD => "<oob><speak><s>lead with alfa</s></speak><alt>'LEAD'</alt></oob>",
		Phrase.LED => "<oob><speak><s>led 3 letters</s></speak><alt>'LED'</alt></oob>",
		Phrase.LEED => "<oob><speak><s>leed language</s></speak><alt>'LEED'</alt></oob>",
		Phrase.LEFT => "<oob><speak><s>left</s></speak><alt>'LEFT'</alt></oob>",
		Phrase.LIKE => "<oob><speak><s>like</s></speak><alt>'LIKE'</alt></oob>",
		Phrase.MIDDLE => "<oob><speak><s>middle</s></speak><alt>'MIDDLE'</alt></oob>",
		Phrase.NEXT => "<oob><speak><s>next</s></speak><alt>'NEXT'</alt></oob>",
		Phrase.NO => "<oob><speak><s>no</s></speak><alt>'NO'</alt></oob>",
		Phrase.NOTHING => "<oob><speak><s>nothing</s></speak><alt>'NOTHING'</alt></oob>",
		Phrase.OKAY => "<oob><speak><s>okay</s></speak><alt>'OKAY'</alt></oob>",
		Phrase.PRESS => "<oob><speak><s>press</s></speak><alt>'PRESS'</alt></oob>",
		Phrase.READ => "<oob><speak><s>read with alfa</s></speak><alt>'READ'</alt></oob>",
		Phrase.READY => "<oob><speak><s>ready</s></speak><alt>'READY'</alt></oob>",
		Phrase.RED => "<oob><speak><s>red 3 letters</s></speak><alt>'RED'</alt></oob>",
		Phrase.REED => "<oob><speak><s>reed with two Es</s></speak><alt>'REED'</alt></oob>",
		Phrase.RIGHT => "<oob><speak><s>right</s></speak><alt>'RIGHT'</alt></oob>",
		Phrase.SAYS => "<oob><speak><s>says</s></speak><alt>'SAYS'</alt></oob>",
		Phrase.SEE => "<oob><speak><s>see a movie</s></speak><alt>'SEE'</alt></oob>",
		Phrase.SURE => "<oob><speak><s>sure</s></speak><alt>'SURE'</alt></oob>",
		Phrase.THEIR => "<oob><speak><s>their with india</s></speak><alt>'THEIR'</alt></oob>",
		Phrase.THERE => "<oob><speak><s>there with two Es</s></speak><alt>'THERE'</alt></oob>",
		Phrase.THEY_RE => "<oob><speak><s>they're with apostrophe</s></speak><alt>'THEY'RE'</alt></oob>",
		Phrase.THEY_ARE => "<oob><speak><s>they are words</s></speak><alt>'THEY ARE'</alt></oob>",
		Phrase.U => "<oob><speak><s>letter U</s></speak><alt>'U'</alt></oob>",
		Phrase.UH_HUH => "<oob><speak><s>uh huh positive</s></speak><alt>'UH HUH'</alt></oob>",
		Phrase.UH_UH => "<oob><speak><s>uh uh negative</s></speak><alt>'UH UH'</alt></oob>",
		Phrase.UHHH => "<oob><speak><s>U triple H</s></speak><alt>'UHHH'</alt></oob>",
		Phrase.UR => "<oob><speak><s>U R letters</s></speak><alt>'UR'</alt></oob>",
		Phrase.WAIT => "<oob><speak><s>wait</s></speak><alt>'WAIT'</alt></oob>",
		Phrase.WHAT => "<oob><speak><s>what no question</s></speak><alt>'WHAT'</alt></oob>",
		Phrase.WHATQ => "<oob><speak><s>what question mark</s></speak><alt>'WHAT?'</alt></oob>",
		Phrase.YES => "<oob><speak><s>yes</s></speak><alt>'YES'</alt></oob>",
		Phrase.YOU => "<oob><speak><s>word you</s></speak><alt>'YOU'</alt></oob>",
		Phrase.YOU_ARE => "<oob><speak><s>you are words</s></speak><alt>'YOU ARE'</alt></oob>",
		Phrase.YOU_RE => "<oob><speak><s>you're with apostrophe</s></speak><alt>'YOU'RE'</alt></oob>",
		Phrase.YOUR => "<oob><speak><s>your no apostrophe</s></speak><alt>'YOUR'</alt></oob>",
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
		[AimlSetItem("your"), AimlSetItem("your 4 letters"), AimlSetItem("your four letters")]
		YOUR
	}
}
