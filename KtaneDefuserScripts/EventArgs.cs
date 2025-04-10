namespace KtaneDefuserScripts;

public class AimlInterfaceEventArgs(AimlAsyncContext context) : EventArgs {
	public AimlAsyncContext Context { get; set; } = context ?? throw new ArgumentNullException(nameof(context));
}

public class StrikeEventArgs(AimlAsyncContext context, Slot slot, ModuleState? moduleState) : AimlInterfaceEventArgs(context) {
	public Slot Slot { get; } = slot;
	public ModuleState? ModuleState { get; } = moduleState;
}