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
		GameState.Current.CurrentModule?.Script.Stopped(context);
		GameState.Current.CurrentModuleNum = number;
		var script = GameState.Current.CurrentModule!.Script;
		context.RequestProcess.Log(Aiml.LogLevel.Info, $"Selected module {number} ({GameState.Current.CurrentModule.Reader.Name})");
		context.RequestProcess.User.Topic = script.Topic;
		context.Reply($"<oob><queue/><setgrammar>{script.Topic}</setgrammar></oob> Module {number} is {script.IndefiniteDescription}.");
		script.Started(context);
		// If we aren't in another interrupt, select this module. (If we are, Interrupt.Exit will select this module afterward if it is still the current module.)
		if (Interrupt.EnableInterrupts) {
			using var interrupt = await Interrupt.EnterAsync(context);
			await Utils.SelectModuleAsync(interrupt, GameState.Current.CurrentModuleNum.Value);
			await AimlTasks.Delay(0.75);
			script.ModuleSelected(context);
		}
	}
}
