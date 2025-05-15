namespace KtaneDefuserScripts;
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

	Anagrams,
	[AimlSetItem("Colour Flash")]
	ColourFlash,
	[AimlSetItem("Crazy Talk")]
	CrazyTalk,
	[AimlSetItem("Emoji Math")]
	EmojiMath,
	[AimlSetItem("Letter Keys")]
	LetterKeys,
	[AimlSetItem("Lights Out")]
	LightsOut,
	[AimlSetItem("Needy Math"), AimlSetItem("Math")]
	NeedyMath,
	[AimlSetItem("Piano Keys")]
	PianoKeys,
	Semaphore,
	Switches,
	[AimlSetItem("Turn the Keys")]
	TurnTheKeys,
	[AimlSetItem("Word Scramble")]
	WordScramble,

	Unknown = -1
}
