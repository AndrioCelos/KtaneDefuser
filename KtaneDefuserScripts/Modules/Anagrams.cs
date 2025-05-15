namespace KtaneDefuserScripts.Modules;
[AimlInterface("Anagrams")]
internal class Anagrams : ModuleScript<KtaneDefuserConnector.Components.Anagrams> {
	public override string IndefiniteDescription => "Anagrams";
	
	private readonly WordScramble.Processor _processor = new(i => i.Read(Reader));
	
	protected internal override void Started(AimlAsyncContext context) => context.AddReply("ready");

	[AimlCategory("read")]
	internal static async Task Read(AimlAsyncContext context) {
		var script = GameState.Current.CurrentScript<Anagrams>();
		using var interrupt = await script.ModuleInterruptAsync(context);
		await script._processor.ReadAsync(interrupt);
	}

	[AimlCategory("<set>NATO</set> <set>NATO</set> <set>NATO</set> <set>NATO</set> <set>NATO</set> <set>NATO</set>")]
	internal static async Task SubmitLetters(AimlAsyncContext context, string nato1, string nato2, string nato3, string nato4, string nato5, string nato6) {
		using var interrupt = await CurrentModuleInterruptAsync(context);
		await GameState.Current.CurrentScript<Anagrams>()._processor.SubmitLetters(interrupt, NATO.DecodeChar(nato1), NATO.DecodeChar(nato2), NATO.DecodeChar(nato3), NATO.DecodeChar(nato4), NATO.DecodeChar(nato5), NATO.DecodeChar(nato6));
	}
}
