using System.Diagnostics;

namespace BombDefuserScripts;
public class GameState {
	private int? selectedModuleNum;

	public static GameState Current { get; set; } = new();

	public bool WaitingForLights { get; set; }
	public ComponentSlot? TimerSlot { get; set; }
	public TimeSpan TimerBaseTime { get; set; } = TimeSpan.Zero;
	public Stopwatch TimerStopwatch { get; set; } = Stopwatch.StartNew();
	public int SelectedFaceNum { get; set; }
	public BombFace SelectedFace => this.Faces[this.SelectedFaceNum];
	public int? SelectedModuleNum {
		get => selectedModuleNum;
		set {
			if (value == selectedModuleNum) return;
			if (selectedModuleNum != null)
				this.ModuleDeselected?.Invoke(null, EventArgs.Empty);
			selectedModuleNum = value;
			if (value != null)
				this.ModuleSelected?.Invoke(null, EventArgs.Empty);
		}
	}
	public ModuleState? SelectedModule => this.SelectedModuleNum is not null ? this.Modules[this.SelectedModuleNum.Value] : null;
	public T CurrentScript<T>() where T : ModuleScript => this.SelectedModule?.Script as T ?? throw new InvalidOperationException("Specified script is not in progress.");

	internal FocusState FocusState { get; set; }
	internal BombFace[] Faces { get; } = new BombFace[] { new(), new() };
	internal List<ModuleState> Modules { get; } = new();
	internal List<WidgetProcessor> Widgets { get; } = new();

	// Edgework
	public int BatteryHolderCount { get; set; }
	public int BatteryCount { get; set; }
	public int AABatteryCount => 2 * this.BatteryCount - 2 * this.BatteryHolderCount;
	public int DBatteryCount => 2 * this.BatteryHolderCount - this.BatteryCount;

	public List<IndicatorData> Indicators { get; } = new();
	public int IndicatorUnlitCount => this.Indicators.Count(i => !i.IsLit);
	public int IndicatorLitCount => this.Indicators.Count(i => i.IsLit);

	public bool HasIndicator(string label) => this.Indicators.Any(i => i.Label == label);
	public bool HasIndicator(bool isLit, string label) => this.Indicators.Any(i => i.IsLit == isLit && i.Label == label);

	public List<PortTypes> PortPlates { get; } = new();
	public bool PortEmptyPlate => this.PortPlates.Contains(0);
	public int PortCount => this.PortPlates.Sum(p => (p.HasFlag(PortTypes.Parallel) ? 1 : 0) + (p.HasFlag(PortTypes.Serial) ? 1 : 0) + (p.HasFlag(PortTypes.StereoRCA) ? 1 : 0) + (p.HasFlag(PortTypes.DviD) ? 1 : 0) + (p.HasFlag(PortTypes.PS2) ? 1 : 0) + (p.HasFlag(PortTypes.RJ45) ? 1 : 0));

	public bool HasPort(PortTypes portType) => this.PortPlates.Any(p => p.HasFlag(portType));
	public int PortCountOfType(PortTypes portType) => this.PortPlates.Count(p => p.HasFlag(portType));

	public string SerialNumber { get; set; } = "";
	public bool SerialNumberHasVowel => this.SerialNumber.Any(c => c is 'A' or 'E' or 'I' or 'O' or 'U');
	public bool SerialNumberIsOdd => this.SerialNumber[^1] is '1' or '3' or '5' or '7' or '9';

	public BombDefuserConnector.Components.Timer.GameMode GameMode { get; set; }

	public event EventHandler? ModuleDeselected;
	public event EventHandler? ModuleSelected;

	public TimeSpan Time => this.GameMode is BombDefuserConnector.Components.Timer.GameMode.Zen or BombDefuserConnector.Components.Timer.GameMode.Training
		? this.TimerBaseTime + this.TimerStopwatch.Elapsed
		: this.TimerBaseTime - this.TimerStopwatch.Elapsed;
}

public struct IndicatorData {
	public bool IsLit;
	public string Label;

	public IndicatorData(bool isLit, string label) {
		this.IsLit = isLit;
		this.Label = label ?? throw new ArgumentNullException(nameof(label));
	}
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

public class ModuleState {
	public ComponentSlot Slot { get; }
	public ComponentProcessor Processor { get; }
	public ModuleScript? Script { get; }  // Will be null for the timer.

	public ModuleState(ComponentSlot slot, ComponentProcessor processor, ModuleScript? script) {
		this.Slot = slot;
		this.Processor = processor ?? throw new ArgumentNullException(nameof(processor));
		this.Script = script;
	}
}

public class BombFace {
	public bool HasModules;
	public ComponentSlot SelectedSlot;

	private readonly ModuleState?[,] slots = new ModuleState?[3, 2];

	public ModuleState? this[ComponentSlot slot] {
		get => this.slots[slot.X, slot.Y];
		set => this.slots[slot.X, slot.Y] = value;
	}
	public ModuleState? this[int x, int y] {
		get => this.slots[x, y];
		set => this.slots[x, y] = value;
	}
}
