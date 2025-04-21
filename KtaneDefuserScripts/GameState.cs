using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using KtaneDefuserConnector.DataTypes;

namespace KtaneDefuserScripts;
public class GameState(ILoggerFactory loggerFactory) {
	private static GameState? current;

	public static GameState Current {
		get => current ?? throw new InvalidOperationException("Game has not been initialised.");
		set => current = value;
	}

	internal ILoggerFactory LoggerFactory = loggerFactory;

	/// <summary>Indicates the tyoe of the bomb casing.</summary>
	public BombType BombType { get; set; }
	/// <summary>Indicates whether we are waiting for the lights to turn on at the start of the game.</summary>
	public bool WaitingForLights { get; set; }
	/// <summary>The current number of strikes. In Time mode, this is always zero.</summary>
	public int Strikes { get; set; }
	/// <summary>The location of the timer, or <see langword="null"/> if it is unknown.</summary>
	public Slot? TimerSlot { get; set; }
	/// <summary>The time on the timer at the moment the <see cref="TimerStopwatch"/> was started.</summary>
	public TimeSpan TimerBaseTime { get; set; } = TimeSpan.Zero;
	/// <summary>A <see cref="Stopwatch"/> that keeps track of the bomb time.</summary>
	public Stopwatch TimerStopwatch { get; set; } = Stopwatch.StartNew();
	/// <summary>The index of the currently-selected bomb face.</summary>
	public int SelectedFaceNum { get; set; }
	/// <summary>Returns a <see cref="BombFace"/> instance representing the currently-selected bomb face.</summary>
	public BombFace SelectedFace => Faces[SelectedFaceNum];
	/// <summary>Whether we are looking at a side of the bomb at the start of the game.</summary>
	public bool LookingAtSide { get; set; }
	/// <summary>The currently-selected module number, or <see langword="null"/> if no module is selected.</summary>
	public int? SelectedModuleNum { get; set; }
	/// <summary>Returns a <see cref="ModuleState"/> instance representing the currently-selected module, or <see langword="null"/> if no module is selected.</summary>
	public ModuleState? SelectedModule => SelectedModuleNum is not null ? Modules[SelectedModuleNum.Value] : null;

	/// <summary>The number of the module we're currently discussing with the expert, or <see langword="null"/> if no module is in progress.</summary>
	public int? CurrentModuleNum { get; set; }
	/// <summary>Returns a <see cref="ModuleState"/> instance representing the module we're discussing with the expert, or <see langword="null"/> if no module is in progress.</summary>
	public ModuleState? CurrentModule => CurrentModuleNum is not null ? Modules[CurrentModuleNum.Value] : null;
	/// <summary>A queue of module numbers that will be handled after the current one.</summary>
	public Queue<int> NextModuleNums { get; } = new();
	/// <summary>Returns a <see cref="ModuleScript{TReader}"/> instance of the specified type for the module we're currently discussing with the expert.</summary>
	/// <exception cref="InvalidOperationException">The specified script type is not in progress.</exception>
	public T CurrentScript<T>() where T : ModuleScript => CurrentModule?.Script as T ?? throw new InvalidOperationException("Specified script is not in progress.");

	internal FocusState FocusState { get; set; }
	internal BombFace[] Faces { get; } = [new(), new()];
	internal List<ModuleState> Modules { get; } = [];
	internal List<WidgetReader> Widgets { get; } = [];

	// Edgework
	public int BatteryHolderCount { get; set; }
	public int BatteryCount { get; set; }
	public int AABatteryCount => 2 * BatteryCount - 2 * BatteryHolderCount;
	public int DBatteryCount => 2 * BatteryHolderCount - BatteryCount;

	public List<IndicatorData> Indicators { get; } = [];
	public int IndicatorUnlitCount => Indicators.Count(i => !i.IsLit);
	public int IndicatorLitCount => Indicators.Count(i => i.IsLit);

	public bool HasIndicator(string label) => Indicators.Any(i => i.Label == label);
	public bool HasIndicator(bool isLit, string label) => Indicators.Any(i => i.IsLit == isLit && i.Label == label);

	public List<PortTypes> PortPlates { get; } = [];
	public bool PortEmptyPlate => PortPlates.Contains(0);
	public int PortCount => PortPlates.Sum(p => (p.HasFlag(PortTypes.Parallel) ? 1 : 0) + (p.HasFlag(PortTypes.Serial) ? 1 : 0) + (p.HasFlag(PortTypes.StereoRCA) ? 1 : 0) + (p.HasFlag(PortTypes.DviD) ? 1 : 0) + (p.HasFlag(PortTypes.PS2) ? 1 : 0) + (p.HasFlag(PortTypes.RJ45) ? 1 : 0));

	public bool HasPort(PortTypes portType) => PortPlates.Any(p => p.HasFlag(portType));
	public int PortCountOfType(PortTypes portType) => PortPlates.Count(p => p.HasFlag(portType));

	public string SerialNumber { get; set; } = "";
	public bool SerialNumberHasVowel => SerialNumber.Any(c => c is 'A' or 'E' or 'I' or 'O' or 'U');
	public bool SerialNumberIsOdd => SerialNumber[^1] is '1' or '3' or '5' or '7' or '9';

	public GameMode GameMode { get; set; }

	public bool IsDefused => Modules.Where(m => !m.Script.PriorityCategory.HasFlag(PriorityCategory.Needy)).All(m => m.IsSolved);

	/// <summary>Returns the current bomb time.</summary>
	public TimeSpan Time => GameMode is GameMode.Zen or GameMode.Training
		? TimerBaseTime + TimerStopwatch.Elapsed
		: TimerBaseTime - TimerStopwatch.Elapsed;

	public readonly Dictionary<Slot, NeedyState> UnknownNeedyStates = [];

	public event EventHandler<StrikeEventArgs>? Strike;
	public event EventHandler<StrikeEventArgs>? ModuleSolved;
	public event EventHandler? Defuse;

	internal bool TryMarkModuleSolved(AimlAsyncContext context, int index) {
		var module = Modules[index];
		if (module.IsSolved) return false;
		if (module.Script.PriorityCategory == PriorityCategory.Needy) throw new ArgumentException("Cannot mark a needy module as solved.");

		module.IsSolved = true;
		ModuleSolved?.Invoke(this, new(context, module.Slot, module));

		var defused = IsDefused;
		if (defused) Defuse?.Invoke(this, EventArgs.Empty);
		return defused;
	}

	internal void OnStrike(StrikeEventArgs e) => Strike?.Invoke(this, e);
}

public enum BombType {
	Standard,
	Centurion
}

public struct IndicatorData(bool isLit, string label) {
	public bool IsLit = isLit;
	public string Label = label ?? throw new ArgumentNullException(nameof(label));
}

[Flags]
public enum PortTypes {
	Parallel = 1,
	Serial = 2,
	StereoRCA = 4,
	DviD = 8,
	PS2 = 16,
	RJ45 = 32
}

public class ModuleState(Slot slot, ComponentReader reader, ModuleScript script) {
	/// <summary>The slot that this module is in.</summary>
	public Slot Slot { get; } = slot;
	/// <summary>A <see cref="ModuleType"/> value indicating the type of module.</summary>
	public ModuleType Type { get; } = Enum.Parse<ModuleType>(script.GetType().Name);
	/// <summary>The <see cref="ComponentReader"/> instance corresponding to the module type.</summary>
	public ComponentReader Reader { get; } = reader ?? throw new ArgumentNullException(nameof(reader));
	/// <summary>A <see cref="ModuleScript"/> instance handling this module.</summary>
	public ModuleScript Script { get; } = script;
	/// <summary>Whether the module is solved.</summary>
	public bool IsSolved { get; set; }
}

public class BombFace {
	public bool HasModules { get; set; }
	public Slot SelectedSlot;

	private readonly ModuleState?[,] slots = new ModuleState?[3, 2];

	public ModuleState? this[Slot slot] {
		get => slots[slot.X, slot.Y];
		set => slots[slot.X, slot.Y] = value;
	}
	public ModuleState? this[int x, int y] {
		get => slots[x, y];
		set => slots[x, y] = value;
	}
}
