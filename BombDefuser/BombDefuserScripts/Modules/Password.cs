using System.Text;

namespace BombDefuserScripts.Modules;
[AimlInterface("Password")]
internal class Password : ModuleScript<BombDefuserConnector.Components.Password> {
	public override string IndefiniteDescription => "a Password";

	private int highlightX;
	private int highlightY;
	private readonly char[]?[] columns = new char[]?[5];
	private readonly int[] columnPositions = new int[5];

	private async Task ReadColumn(AimlAsyncContext context, int column) {
		using var interrupt = await Interrupt.EnterAsync(context);
		var builder = new StringBuilder();
		var letters = new char[6];
		await this.MoveToDownButtonRowAsync(interrupt);
		for (var i = 0; i < 6; i++) {
			if (i > 0) await this.CycleColumnAsync(interrupt, column);
			var data = ReadCurrent(Reader);
			letters[i] = data.Display[column];
		}
		this.columns[column] = letters;
		this.columnPositions[column] = 5;
		interrupt.Context.Reply(NATO.Speak(letters));
	}

	private async Task MoveToDownButtonRowAsync(Interrupt interrupt) {
		var builder = new StringBuilder();
		switch (this.highlightY) {
			case 0:
				builder.Append("down ");
				break;
			case 2:
				builder.Append("up ");
				break;
		}
		this.highlightY = 1;
		await interrupt.Context.SendInputsAsync(builder.ToString());
	}

	private async Task CycleColumnAsync(Interrupt interrupt, int column) {
		var builder = new StringBuilder();
		while (column < this.highlightX) {
			builder.Append("left ");
			this.highlightX--;
		}
		while (column > this.highlightX) {
			builder.Append("right ");
			this.highlightX++;
		}
		builder.Append('a');
		await interrupt.Context.SendInputsAsync(builder.ToString());
	}

	private async Task SubmitAsync(Interrupt interrupt, string word) {
		await this.MoveToDownButtonRowAsync(interrupt);
		for (var i = 0; i < 6; i++) {
			var data = ReadCurrent(Reader);
			interrupt.Context.RequestProcess.Log(Aiml.LogLevel.Info, $"Password display: {new string(data.Display)}");
			var anyMismatch = false;
			for (var x = 0; x < 5; x++) {
				if (data.Display[x] != char.ToUpper(word[x])) {
					anyMismatch = true;
					await CycleColumnAsync(interrupt, x);
					this.columnPositions[x]++;
					if (this.columnPositions[x] >= 6) this.columnPositions[x] = 0;
				}
			}
			if (!anyMismatch) {
				this.highlightX = 2;
				this.highlightY = 2;
				await interrupt.SubmitAsync("down a");
				return;
			}
		}
		interrupt.Context.Reply("Could not submit that word.");
	}

	[AimlCategory("<set>ordinal</set> column")]
	public static Task Read1(AimlAsyncContext context, Ordinal ordinal) {
		var script = GameState.Current.CurrentScript<Password>();
		return script.ReadColumn(context, (int) ordinal - 1);
	}

	[AimlCategory("last column")]
	public static Task ReadLast(AimlAsyncContext context) {
		var script = GameState.Current.CurrentScript<Password>();
		return script.ReadColumn(context, 4);
	}

	[AimlCategory("column <set>number</set>")]
	public static Task Read2(AimlAsyncContext context, int num) {
		var script = GameState.Current.CurrentScript<Password>();
		return script.ReadColumn(context, num - 1);
	}

	[AimlCategory("submit *")]
	[AimlCategory("password is *")]
	[AimlCategory("the password is *")]
	public static async Task Submit(AimlAsyncContext context, string word) {
		var script = GameState.Current.CurrentScript<Password>();
		using var interrupt = await Interrupt.EnterAsync(context);
		await script.SubmitAsync(interrupt, word);
	}

	[AimlCategory("<set>NATO</set> <set>NATO</set> <set>NATO</set> <set>NATO</set> <set>NATO</set>")]
	[AimlCategory("submit <set>NATO</set> <set>NATO</set> <set>NATO</set> <set>NATO</set> <set>NATO</set>")]
	[AimlCategory("password is <set>NATO</set> <set>NATO</set> <set>NATO</set> <set>NATO</set> <set>NATO</set>")]
	[AimlCategory("the password is <set>NATO</set> <set>NATO</set> <set>NATO</set> <set>NATO</set> <set>NATO</set>")]
	public static async Task SubmitNato(AimlAsyncContext context, string nato1, string nato2, string nato3, string nato4, string nato5) {
		var script = GameState.Current.CurrentScript<Password>();
		using var interrupt = await Interrupt.EnterAsync(context);
		await script.SubmitAsync(interrupt, $"{NATO.DecodeChar(nato1)}{NATO.DecodeChar(nato2)}{NATO.DecodeChar(nato3)}{NATO.DecodeChar(nato4)}{NATO.DecodeChar(nato5)}");
	}
}
