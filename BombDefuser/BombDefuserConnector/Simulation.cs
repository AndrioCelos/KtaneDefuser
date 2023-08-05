using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Timers;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace BombDefuserConnector;
internal partial class Simulation {
	private int roomX;
	private readonly Timer rxTimer = new(187.5);
	private bool rxBetweenFaces;
	private float ry;
	private FocusStates focusState;
	private int selectedFaceNum;
	private BombFaces currentFace;
	private readonly Queue<string> actionQueue = new();
	private readonly Timer queueTimer = new(1000 / 6);
	private readonly ComponentFace[] moduleFaces = new ComponentFace[2];
	private readonly WidgetFace[] widgetFaces = new WidgetFace[4];
	private bool isAlarmClockOn;

	internal static Image<Rgb24> DummyScreenshot { get; } = new(1, 1);

	private ComponentFace SelectedFace => this.moduleFaces[this.selectedFaceNum];

	public Simulation() {
		this.queueTimer.Elapsed += this.QueueTimer_Elapsed;
		this.rxTimer.Elapsed += this.RXTimer_Elapsed;

		this.moduleFaces[0] = new(new BombComponent?[,] {
			{ TimerComponent.Instance, null, null },
			{ null, null, null }
		});
		this.moduleFaces[1] = new(new BombComponent?[,] {
			{ null, null, null },
			{ null, null, null }
		});
		this.widgetFaces[0] = new(new[] { Widget.Create(new Widgets.SerialNumber(), "AB3DE6"), null, null, null });
		this.widgetFaces[1] = new(new[] { Widget.Create(new Widgets.Indicator(), new(false, "BOB")), Widget.Create(new Widgets.Indicator(), new(true, "FRQ")), null, null });
		this.widgetFaces[2] = new(new[] { Widget.Create(new Widgets.BatteryHolder(), 2), null, null, null, null, null });
		this.widgetFaces[3] = new(new[] { Widget.Create(new Widgets.PortPlate(), new Widgets.PortPlate.Ports(Widgets.PortPlate.PortType.Parallel | Widgets.PortPlate.PortType.Serial)), Widget.Create(new Widgets.PortPlate(), new Widgets.PortPlate.Ports(0)), null, null, null, null });
		Message("Simulation initialised.");

		for (var i = 0; i < this.moduleFaces.Length; i++) {
			for (var y = 0; y < this.moduleFaces[i].Slots.GetLength(0); y++) {
				for (var x = 0; x < this.moduleFaces[i].Slots.GetLength(1); x++) {
					if (this.moduleFaces[i].Slots[y, x] is Module module) {
						module.InitialiseHighlight();
						if (module is NeedyModule needyModule)
							needyModule.Initialise(i, x, y);
					}
				}
			}
		}
	}

	private void RXTimer_Elapsed(object? sender, ElapsedEventArgs e) {
		if (rxBetweenFaces)
			rxBetweenFaces = false;
		else {
			rxBetweenFaces = true;
			this.currentFace = (BombFaces) (((int) this.currentFace + 1) % 4);
			var faceNum = ((int) this.currentFace + 1) / 2 % 2;
			if (faceNum != this.selectedFaceNum) {
				this.selectedFaceNum = faceNum;
				if (this.focusState == FocusStates.Module) {
					this.focusState = FocusStates.Bomb;
					Message("Module deselected");
				}
			}
			Message($"Turned the bomb to {this.currentFace}");
		}
	}

	private static void Message(string s) {
		Console.ForegroundColor = ConsoleColor.Cyan;
		Console.WriteLine($"Simulation: {s}");
		Console.ResetColor();
	}

	private void QueueTimer_Elapsed(object? sender, ElapsedEventArgs e) => this.PopInputQueue();

	private void PopInputQueue() {
		if (!this.actionQueue.TryDequeue(out var action)) {
			this.queueTimer.Stop();
			return;
		}
		var tokens2 = action.Split(':');
		switch (tokens2[0].ToLowerInvariant()) {
			case "callback":
				AimlVoice.Program.sendInput($"OOB DefuserCallback {(tokens2.Length > 1 ? tokens2[1] : "nil")}");
				break;
			case "rx":
				var v = float.Parse(tokens2[1]);
				if (v > 0)
					this.rxTimer.Start();
				else
					this.rxTimer.Stop();
				break;
			case "ry":
				this.ry = float.Parse(tokens2[1]);
				break;
			case "a":
				switch (this.focusState) {
					case FocusStates.Room:
						this.focusState = this.roomX == -1 ? FocusStates.AlarmClock : FocusStates.Bomb;
						Message($"{this.focusState} selected");
						break;
					case FocusStates.AlarmClock:
						this.SetAlarmClock(!isAlarmClockOn);
						break;
					case FocusStates.Bomb:
						if ((int) this.currentFace % 2 != 0) {
							this.currentFace = (BombFaces) (((int) this.currentFace + 1) % 4);
							this.rxBetweenFaces = false;
							Message($"Aligned the bomb to {this.currentFace}");
						}
						var component = this.SelectedFace.SelectedComponent;
						if (component == null)
							Message("No module is highlighted.");
						else if (component is Module module1) {
							this.focusState = FocusStates.Module;
							Message($"{component.Processor.Name} [{module1.ID}] ({this.SelectedFace.X + 1}, {this.SelectedFace.Y + 1}) selected");
						} else
							Message($"Can't select {component.Processor.Name}.");
						break;
					case FocusStates.Module:
						if (this.SelectedFace.SelectedComponent is Module module2) {
							switch (tokens2.ElementAtOrDefault(1)) {
								case "hold":
									module2.Interact();
									break;
								case "release":
									module2.StopInteract();
									break;
								default:
									module2.Interact();
									module2.StopInteract();
									break;
							}
						}
						break;
				}
				break;
			case "b":
				switch (this.focusState) {
					case FocusStates.AlarmClock:
					case FocusStates.Bomb:
						if ((int) this.currentFace % 2 != 0) {
							this.currentFace = (BombFaces) (((int) this.currentFace + 1) % 4);
							this.rxBetweenFaces = false;
							Message($"Aligned the bomb to {this.currentFace}");
						}
						this.focusState = FocusStates.Room;
						if ((int) this.currentFace % 2 != 0) {
							this.currentFace = (BombFaces) (((int) this.currentFace + 1) % 4);
							this.rxBetweenFaces = false;
							Message($"Aligned the bomb to {this.currentFace}");
						}
						Message("Returned to room");
						break;
					case FocusStates.Module:
						this.focusState = FocusStates.Bomb;
						Message("Returned to bomb");
						break;
				}
				break;
			case "left":
				switch (this.focusState) {
					case FocusStates.Room:
						if (this.roomX == 0) this.roomX = -1;
						break;
					case FocusStates.Bomb:
						do {
							this.SelectedFace.X--;
						} while (this.SelectedFace.SelectedComponent is not Module);
						break;
					case FocusStates.Module:
						if (this.SelectedFace.SelectedComponent is Module module)
							module.MoveHighlight(DataTypes.Direction.Left);
						break;
				}
				break;
			case "right":
				switch (this.focusState) {
					case FocusStates.Room:
						if (this.roomX == -1) this.roomX = 0;
						break;
					case FocusStates.Bomb:
						do {
							this.SelectedFace.X++;
						} while (this.SelectedFace.SelectedComponent is not Module);
						break;
					case FocusStates.Module:
						if (this.SelectedFace.SelectedComponent is Module module)
							module.MoveHighlight(DataTypes.Direction.Right);
						break;
				}
				break;
			case "up":
				switch (this.focusState) {
					case FocusStates.Bomb:
						this.SelectedFace.Y--;
						this.FindNearestModule();
						break;
					case FocusStates.Module:
						if (this.SelectedFace.SelectedComponent is Module module)
							module.MoveHighlight(DataTypes.Direction.Up);
						break;
				}
				break;
			case "down":
				switch (this.focusState) {
					case FocusStates.Bomb:
						this.SelectedFace.Y++;
						this.FindNearestModule();
						break;
					case FocusStates.Module:
						if (this.SelectedFace.SelectedComponent is Module module)
							module.MoveHighlight(DataTypes.Direction.Down);
						break;
				}
				break;
			default:
				throw new InvalidOperationException($"Invalid control instruction: {tokens2[0]}");
		}
	}

	private void FindNearestModule() {
		if (this.SelectedFace.SelectedComponent is Module) return;
		for (var d = 1; d <= 2; d++) {
			for (var dir = 0; dir < 2; dir++) {
				var x = dir == 0 ? this.SelectedFace.X - d : this.SelectedFace.X + d;
				if (x >= 0 && x < this.SelectedFace.Slots.GetLength(1) && this.SelectedFace.Slots[this.SelectedFace.Y, x] is Module) {
					this.SelectedFace.X = x;
					return;
				}
			}
		}
	}

	public void SendInputs(string inputs) {
		foreach (var token in inputs.Split(new[] { ' ', '+', ',' }, StringSplitOptions.RemoveEmptyEntries))
			this.actionQueue.Enqueue(token);
		if (!this.queueTimer.Enabled) {
			this.queueTimer.Start();
			this.PopInputQueue();
		}
	}

	public string? IdentifyComponent(Point point1) => this.GetComponent(point1)?.Processor.GetType().Name;
	public string? IdentifyComponent(int face, int x, int y) => this.moduleFaces[face].Slots[y, x]?.Processor.GetType().Name;

	public T ReadComponent<T>(Point point1) where T : notnull {
		var component = this.GetComponent(point1) ?? throw new ArgumentException("Attempted to read an empty component slot.");
		return component is TimerComponent timerComponent
			? timerComponent.Details is T t ? t : throw new ArgumentException("Wrong type for specified component.")
			: component switch {
				Module<T> module => module.Details,
				NeedyModule<T> needyModule => needyModule.Details,
				_ => throw new ArgumentException("Wrong type for specified component")
			};
	}
	public string ReadModule(string type, Point point1) {
		var component = this.GetComponent(point1) ?? throw new ArgumentException("Attempt to read blank component");
		return component.Processor.GetType().Name.Equals(type, StringComparison.OrdinalIgnoreCase)
			? component.DetailsString
			: throw new ArgumentException("Wrong type for specified component.");
	}

	public ModuleLightState GetLightState(Point point1) => this.GetComponent(point1) is Module module && module is not NeedyModule ? module.LightState : ModuleLightState.Off;

	private BombComponent? GetComponent(Point point1) {
		switch (this.focusState) {
			case FocusStates.Bomb: {
				var face = this.currentFace switch { BombFaces.Face1 => this.moduleFaces[0], BombFaces.Face2 => this.moduleFaces[1], _ => throw new InvalidOperationException($"Can't identify modules from face {this.currentFace}.") };
				var slotX = point1.X switch { 558 or 572 => 0, 848 or 852 => 1, 1127 or 1134 => 2, _ => throw new ArgumentException($"Unknown x coordinate: {point1.X}") };
				var slotY = point1.Y switch { 291 or 292 => 0, 558 => 1, _ => throw new ArgumentException($"Unknown y coordinate: {point1.Y}") };
				return face.Slots[slotY, slotX];
			}
			case FocusStates.Module: {
				var face = this.SelectedFace;
				var slotDX = point1.X switch { <= 220 => -2, <= 535 => -1, <= 840 => 0, <= 1164 => 1, _ => 2 };
				var slotDY = point1.Y switch { <= 102 => -1, <= 393 => 0, _ => 1 };
				return face.Slots[face.Y + slotDY, face.X + slotDX];
			}
			default:
				throw new InvalidOperationException($"Can't identify modules from state {this.focusState}.");
		}
	}

	public string? IdentifyWidget(Point point1) => this.GetWidget(point1)?.Processor.GetType().Name;

	public T ReadWidget<T>(Point point1) where T : notnull {
		var widget = this.GetWidget(point1) ?? throw new ArgumentException("Attempt to read blank widget.");
		return widget is Widget<T> widget2 ? widget2.Details : throw new ArgumentException("Wrong type for specified widget.");
	}
	public string ReadWidget(string type, Point point1) {
		var widget = this.GetWidget(point1) ?? throw new ArgumentException("Attempt to read blank widget.");
		return widget.Processor.GetType().Name.Equals(type, StringComparison.OrdinalIgnoreCase)
			? widget.DetailsString
			: throw new ArgumentException("Wrong type for specified widget.");
	}

	private Widget? GetWidget(Point point1) {
		switch (this.focusState) {
			case FocusStates.Bomb: {
				var face = this.currentFace switch { BombFaces.Side1 => this.widgetFaces[0], BombFaces.Side2 => this.widgetFaces[1], _ => this.ry switch { < -0.5f => this.widgetFaces[2], >= 0.5f => this.widgetFaces[3], _ => throw new InvalidOperationException($"Can't identify widgets from face {this.currentFace}.") } };
				int slot;
				if (face.Slots.Length == 4) {
					var slotX = point1.X switch { < 900 => 0, _ => 1 };
					var slotY = point1.Y switch { 465 => 0, 772 => 1, _ => throw new ArgumentException($"Unknown y coordinate: {point1.Y}") };
					slot = slotY * 2 + slotX;
				} else {
					var slotX = point1.X switch { <= 588 => 0, <= 824 => 1, _ => 2 };
					var slotY = point1.Y switch { 430 => 0, 566 => 1, _ => throw new ArgumentException($"Unknown y coordinate: {point1.Y}") };
					slot = slotY * 3 + slotX;
				}
				return face.Slots[slot];
			}
			default:
				throw new InvalidOperationException($"Can't identify widgets from state {this.focusState}.");
		}
	}

	public void Solve() {
		if (this.SelectedFace.SelectedComponent is Module module)
			module.Solve();
	}

	public void Strike() {
		if (this.SelectedFace.SelectedComponent is Module module)
			module.StrikeFlash();
	}

	internal void SetAlarmClock(bool value) {
		Message($"Turned alarm clock {(value ? "on" : "off")}");
		isAlarmClockOn = value;
		AimlVoice.Program.sendInput($"OOB DefuserSocketMessage AlarmClock {value}");
	}

	private enum FocusStates {
		Room,
		Bomb,
		Module,
		AlarmClock
	}

	private enum BombFaces {
		Face1,
		Side1,
		Face2,
		Side2
	}

	private class ComponentFace {
		internal int X;
		internal int Y;
		internal readonly BombComponent?[,] Slots;

		public BombComponent? SelectedComponent => this.Slots[this.Y, this.X];

		public ComponentFace(BombComponent?[,] slots) {
			this.Slots = slots ?? throw new ArgumentNullException(nameof(slots));
			for (var y = 0; y < slots.GetLength(0); y++) {
				for (var x = 0; x < slots.GetLength(1); x++) {
					if (slots[y, x] is Module) {
						this.X = x;
						this.Y = y;
						return;
					}
				}
			}
		}
	}

	private class WidgetFace {
		internal readonly Widget?[] Slots;

		public WidgetFace(Widget?[] slots) => this.Slots = slots ?? throw new ArgumentNullException(nameof(slots));
	}

	private abstract class BombComponent {
		internal ComponentProcessor Processor { get; }
		internal abstract string DetailsString { get; }
		protected BombComponent(ComponentProcessor processor) => this.Processor = processor ?? throw new ArgumentNullException(nameof(processor));
	}

	private abstract class Module : BombComponent {
		private readonly Timer ResetLightTimer = new(2000) { AutoReset = false };

		private static int NextID;

		internal int ID { get; }
		public ModuleLightState LightState { get; private set; }

		public int X;
		public int Y;
		protected bool[,] SelectableGrid { get; }

		public Module(ComponentProcessor processor, int selectableWidth, int selectableHeight) : base(processor) {
			NextID++;
			this.ID = NextID;
			this.ResetLightTimer.Elapsed += this.ResetLightTimer_Elapsed;
			this.SelectableGrid = new bool[selectableHeight, selectableWidth];
			for (var y = 0; y < selectableHeight; y++)
				for (var x = 0; x < selectableWidth; x++)
					this.SelectableGrid[y, x] = true;
		}

		private void ResetLightTimer_Elapsed(object? sender, ElapsedEventArgs e) => this.LightState = ModuleLightState.Off;

		public void InitialiseHighlight() {
			for (var y = 0; y < this.SelectableGrid.GetLength(0); y++) {
				for (var x = 0; x < this.SelectableGrid.GetLength(1); x++) {
					if (this.SelectableGrid[y, x]) {
						this.X = x;
						this.Y = y;
						return;
					}
				}
			}
		}

		public void MoveHighlight(DataTypes.Direction direction) {
			for (var side = 0; side < 3; side++) {
				for (var forward = 1; forward < 3; forward++) {
					var (x1, y1, x2, y2) = direction switch {
						DataTypes.Direction.Up => (this.X - side, this.Y - forward, this.X + side, this.Y - forward),
						DataTypes.Direction.Right => (this.X + forward, this.Y - side, this.X + forward, this.Y + side),
						DataTypes.Direction.Down => (this.X - side, this.Y + forward, this.X + side, this.Y + forward),
						_ => (this.X - forward, this.Y - side, this.X - forward, this.Y + side)
					};
					if (x1 >= 0 && x1 < this.SelectableGrid.GetLength(1) && y1 >= 0 && y1 < this.SelectableGrid.GetLength(0)
						&& this.SelectableGrid[y1, x1]) {
						this.X = x1;
						this.Y = y1;
						return;
					}
					if (x2 >= 0 && x2 < this.SelectableGrid.GetLength(1) && y2 >= 0 && y2 < this.SelectableGrid.GetLength(0)
						&& this.SelectableGrid[y2, x2]) {
						this.X = x2;
						this.Y = y2;
						return;
					}
				}
			}
			throw new ArgumentException("Highlight movement went out of bounds");
		}

		public void Solve() {
			if (this.LightState == ModuleLightState.Solved) return;
			Message($"{this.Processor.Name} solved.");
			this.LightState = ModuleLightState.Solved;
			this.ResetLightTimer.Stop();
		}

		public void StrikeFlash() {
			Message($"{this.Processor.Name} strike.");
			if (this.LightState != ModuleLightState.Solved) {
				this.LightState = ModuleLightState.Strike;
				this.ResetLightTimer.Stop();
				this.ResetLightTimer.Start();
			}
		}

		public virtual void Interact() => Message($"Selected ({this.X}, {this.Y}) in {this.Processor.Name}");
		public virtual void StopInteract() { }
	}

	private abstract class Module<TDetails> : Module where TDetails : notnull {
		internal virtual TDetails Details { get; }
		internal override string DetailsString => this.Details.ToString() ?? "";

		protected Module(ComponentProcessor<TDetails> processor, int selectableWidth, int selectableHeight) : this(processor, default!, selectableWidth, selectableHeight) { }
		public Module(ComponentProcessor<TDetails> processor, TDetails details, int selectableWidth, int selectableHeight) : base(processor, selectableWidth, selectableHeight) => this.Details = details;
	}

	private abstract class NeedyModule : Module {
		private int faceNum;
		private int x;
		private int y;
		private TimeSpan baseTime;
		private readonly Stopwatch stopwatch = new();
		private readonly Timer timer = new() { AutoReset = false };

		public virtual TimeSpan StartingTime => TimeSpan.FromSeconds(45);
		public virtual bool AutoReset => true;

		public bool IsActive { get; private set; }
		public TimeSpan RemainingTime => this.stopwatch is not null ? this.baseTime - this.stopwatch.Elapsed : TimeSpan.Zero;
		public int? DisplayedTime => this.IsActive ? (int?) this.RemainingTime.TotalSeconds : null;

		protected NeedyModule(ComponentProcessor processor, int selectableWidth, int selectableHeight) : base(processor, selectableWidth, selectableHeight)
			=> this.timer.Elapsed += this.ReactivateTimer_Elapsed;

		public void Initialise(int faceNum, int x, int y) {
			this.faceNum = faceNum;
			this.x = x;
			this.y = y;
			AimlVoice.Program.sendInput($"OOB DefuserSocketMessage NeedyStateChanged {this.faceNum} {this.x} {this.y} AwaitingActivation");
			this.timer.Interval = 10000;
			this.timer.Start();
		}

		public void Activate() {
			this.baseTime = this.StartingTime;
			this.timer.Interval = this.baseTime.TotalMilliseconds;
			this.stopwatch.Restart();
			this.IsActive = true;
			Message($"{this.Processor.Name} activated with {this.baseTime} left.");
			this.OnActivate();
			AimlVoice.Program.sendInput($"OOB DefuserSocketMessage NeedyStateChanged {this.faceNum} {this.x} {this.y} Running");
		}

		protected abstract void OnActivate();

		public void Deactivate() {
			if (!this.IsActive) return;
			this.IsActive = false;
			this.stopwatch.Stop();
			Message($"{this.Processor.Name} deactivated with {this.RemainingTime} left.");
			if (this.AutoReset) {
				timer.Interval = 30000;
				AimlVoice.Program.sendInput($"OOB DefuserSocketMessage NeedyStateChanged {this.faceNum} {this.x} {this.y} Cooldown");
			} else {
				this.timer.Stop();
				AimlVoice.Program.sendInput($"OOB DefuserSocketMessage NeedyStateChanged {this.faceNum} {this.x} {this.y} Terminated");
			}
		}

		public virtual void OnTimerExpired() => this.StrikeFlash();

		private void ReactivateTimer_Elapsed(object? sender, ElapsedEventArgs e) {
			if (this.IsActive) {
				this.OnTimerExpired();
				this.Deactivate();
			} else
				this.Activate();
		}

		public void AddTime(TimeSpan time, TimeSpan max) {
			this.baseTime += time;
			if (this.RemainingTime > max) {
				this.baseTime = max;
				this.stopwatch.Restart();
			}
		}
	}

	private abstract class NeedyModule<TDetails> : NeedyModule where TDetails : notnull {
		internal virtual TDetails Details { get; }
		internal override string DetailsString => this.Details.ToString() ?? "";

		protected NeedyModule(ComponentProcessor<TDetails> processor, int selectableWidth, int selectableHeight) : this(processor, default!, selectableWidth, selectableHeight) { }
		protected NeedyModule(ComponentProcessor<TDetails> processor, TDetails details, int selectableWidth, int selectableHeight) : base(processor, selectableWidth, selectableHeight) => this.Details = details;
	}

	private class TimerComponent : BombComponent {
		public TimeSpan Elapsed => this.stopwatch.Elapsed;

		internal static TimerComponent Instance { get; } = new();

		private readonly Stopwatch stopwatch = Stopwatch.StartNew();

		internal Components.Timer.ReadData Details {
			get {
				var elapsed = this.stopwatch.ElapsedTicks;
				var seconds = elapsed / Stopwatch.Frequency;
				return new(Components.Timer.GameMode.Zen, (int) seconds, seconds < 60 ? (int) (elapsed / (Stopwatch.Frequency / 100) % 100) : 0, 0);
			}
		}
		internal override string DetailsString => this.Details.ToString();

		private TimerComponent() : base(new Components.Timer()) { }
	}

	private abstract class Widget {
		internal WidgetProcessor Processor { get; }
		internal abstract string DetailsString { get; }

		protected Widget(WidgetProcessor processor) => this.Processor = processor ?? throw new ArgumentNullException(nameof(processor));

		internal static Widget<T> Create<T>(WidgetProcessor<T> processor, T details) where T : notnull => new(processor, details);
	}

	private class Widget<T> : Widget where T : notnull {
		internal T Details { get; }
		internal override string DetailsString => this.Details.ToString() ?? "";

		public Widget(WidgetProcessor<T> processor, T details) : base(processor) => this.Details = details;
	}
}
