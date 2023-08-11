namespace BombDefuserScripts;

public class AimlInterfaceEventArgs : EventArgs {
	public AimlAsyncContext Context { get; set; }

	public AimlInterfaceEventArgs(AimlAsyncContext context) => this.Context = context ?? throw new ArgumentNullException(nameof(context));
}

public class StrikeEventArgs : AimlInterfaceEventArgs {
	public Slot Slot { get; }
	public ModuleState? ModuleState { get; }

	public StrikeEventArgs(AimlAsyncContext context, Slot slot, ModuleState? moduleState) : base(context) {
		this.Slot = slot;
		this.ModuleState = moduleState;
	}
}