namespace BombDefuserScripts;
[AimlInterface]
internal static class ModuleSelection {
	[AimlCategory("module <set>number</set>")]
	public static async Task SelectModule(AimlAsyncContext context, int number) {
		var index = number - 1;
		if (index < 0) {
			context.Reply("Not a valid number.");
			return;
		}
		if (index >= GameState.Current.Modules.Count) {
			context.Reply("There do not seem to be that many modules.");
			return;
		}
		var module = GameState.Current.Modules[index];
		var script = module.Script!;
		context.Reply($"<oob><queue/><setgrammar>{script.Topic}</setgrammar></oob> Module {number} is {script.IndefiniteDescription}.");
		await Utils.SelectModuleAsync(context, index);
		context.RequestProcess.User.Topic = script.Topic;
		script.Entering(context);
	}
}
