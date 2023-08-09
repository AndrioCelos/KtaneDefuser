namespace BombDefuserConnectorApi;

public enum NeedyState {
	InitialSetup,
	AwaitingActivation,
	Running,
	Cooldown,
	Terminated,
	BombComplete
}