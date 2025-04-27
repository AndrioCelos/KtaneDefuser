using JetBrains.Annotations;

namespace KtaneDefuserConnectorApi;

[PublicAPI]
public enum NeedyState {
	InitialSetup,
	AwaitingActivation,
	Running,
	Cooldown,
	Terminated,
	BombComplete
}
