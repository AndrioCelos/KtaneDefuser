using AimlCSharpInterface;

namespace BombDefuserScripts;
[AimlSet]
public enum ModuleType {
	[AimlSetItem("The Button"), AimlSetItem("Button")]
	ButtonModule,
	[AimlSetItem("Complicated Wires")]
	ComplicatedWires,
	Keypad,
	Maze,
	Memory,
	[AimlSetItem("Morse Code")]
	MorseCode,
	[AimlSetItem("Capacitor"), AimlSetItem("Needy Capacitor"), AimlSetItem("Capacitor Discharge")]
	NeedyCapacitor,
	[AimlSetItem("Knob"), AimlSetItem("Needy Knob")]
	NeedyKnob,
	[AimlSetItem("Needy Vent Gas"), AimlSetItem("Needy Venting Gas"), AimlSetItem("Vent Gas"), AimlSetItem("Venting Gas")]
	NeedyVentGas,
	Password,
	[AimlSetItem("Simon Says"), AimlSetItem("Simon")]
	SimonSays,
	[AimlSetItem("Who's on First"), AimlSetItem("Who is on First")]
	WhosOnFirst,
	Wires,
	[AimlSetItem("Wire Sequence")]
	WireSequence,
	Unknown = -1
}
