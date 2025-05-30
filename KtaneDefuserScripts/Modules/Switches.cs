﻿namespace KtaneDefuserScripts.Modules;
[AimlInterface("Switches")]
internal class Switches() : ModuleScript<KtaneDefuserConnector.Components.Switches>(5, 1) {
	public override string IndefiniteDescription => "Switches";

	protected internal override void Started(AimlAsyncContext context) => context.AddReply("ready");

	[AimlCategory("read")]
	internal static Task Read(AimlAsyncContext context) => GameState.Current.CurrentScript<Switches>().ReadAsync(context);

	private async Task ReadAsync(AimlAsyncContext context) {
		using var interrupt = await CurrentModuleInterruptAsync(context);
		var data = interrupt.Read(Reader);
		if (data.Selection is { } selection) Selection = selection;
		context.Reply($"Current state: {Convert(data.CurrentState)}. Target state: {Convert(data.TargetState)}.");
		return;

		static string Convert(bool[] state) => string.Join(' ', from b in state select b ? "up" : "down");
	}

	[AimlCategory("<set>number</set> ^")]
	internal static Task Submit(AimlAsyncContext context) {
		var tokens = context.RequestProcess.Sentence.Text.Split((char[]?) null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		var nums = new int[tokens.Length];
		for (var i = 0; i < tokens.Length; i++) {
			if (!int.TryParse(tokens[i], out var n) || n is < 1 or > 5) {
				context.Reply("Those are not valid numbers.");
				return Task.CompletedTask;
			}
			nums[i] = n - 1;
		}
		
		return GameState.Current.CurrentScript<Switches>().Submit(context, nums);
	}

	private async Task Submit(AimlAsyncContext context, int[] nums) {
		using var interrupt = await CurrentModuleInterruptAsync(context);
		foreach (var n in nums) {
			await InteractWaitAsync(interrupt, n, 0);
			if (interrupt.HasStrikeOccurred) return;
		}

		await interrupt.CheckStatusAsync();
	}
}
