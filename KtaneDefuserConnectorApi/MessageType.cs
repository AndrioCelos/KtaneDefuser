namespace KtaneDefuserConnectorApi;
public enum MessageType {
	LegacyCommand,
	LegacyEvent,
	ScreenshotResponse,
	CheatReadResponse,
	CheatReadError,
	LegacyInputCallback,
	InputCommand,
	InputCallback,
	CancelCommand,
	ScreenshotCommand,
	CheatGetModuleTypeCommand,
	CheatGetModuleTypeResponse,
	CheatGetModuleTypeError,
	CheatReadCommand,

	// Event types
	GameStart = 0x80,
	GameEnd,
	NewBomb,
	Strike,
	AlarmClockChange,
	LightsStateChange,
	NeedyStateChange,
	BombExplode,
	BombDefuse
}
