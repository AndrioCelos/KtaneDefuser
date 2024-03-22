using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Timers;
using BombDefuserConnector.DataTypes;
using BombDefuserConnectorApi;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace BombDefuserConnector;
/// <summary>Provides a simulation of a Keep Talking and Nobody Explodes game, for testing without the real game.</summary>
internal partial class Simulation {
	private int roomX;
	private readonly Timer rxTimer = new(187.5);
	private bool rxBetweenFaces;
	private float ry;
	private FocusStates focusState;
	private int selectedFaceNum;
	private BombFaces currentFace;
	private readonly Queue<IInputAction> actionQueue = new();
	private readonly Timer queueTimer = new(1000 / 6);
	private readonly ComponentFace[] moduleFaces = new ComponentFace[2];
	private readonly WidgetFace[] widgetFaces = new WidgetFace[4];
	private bool isAlarmClockOn;

	internal static Image<Rgba32> DummyScreenshot { get; } = new(1, 1);

	private ComponentFace SelectedFace => this.moduleFaces[this.selectedFaceNum];

	internal event EventHandler<string>? Postback;

	public Simulation() {
		this.queueTimer.Elapsed += this.QueueTimer_Elapsed;
		this.rxTimer.Elapsed += this.RXTimer_Elapsed;

		this.moduleFaces[0] = new(new BombComponent?[,] {
			{ TimerComponent.Instance, new Modules.Semaphore(), null },
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
					if (this.moduleFaces[i].Slots[y, x] is BombComponent component)
						component.PostbackSent += (s, m) => this.Postback?.Invoke(this, m);
					if (this.moduleFaces[i].Slots[y, x] is Module module) {
						var slot = new Slot(0, i, x, y);
						module.Strike += (_, _) => this.Postback?.Invoke(this, $"OOB Strike {slot.Bomb} {slot.Face} {slot.X} {slot.Y}");
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

	/// <summary>Writes the specified message to the console with a colour and prefix.</summary>
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
		switch (action) {
			case CallbackAction callbackAction:
				this.Postback?.Invoke(this, $"OOB DefuserCallback {callbackAction.Token:N}");
				break;
			case AxisAction axisAction:
				switch (axisAction.Axis) {
					case Axis.RightStickX:
						if (axisAction.Value > 0)
							this.rxTimer.Start();
						else
							this.rxTimer.Stop();
						break;
					case Axis.RightStickY:
						this.ry = axisAction.Value;
						break;
				}
				break;
			case ButtonAction buttonAction:
				switch (buttonAction.Button) {
					case Button.A:
						switch (this.focusState) {
							case FocusStates.Room:
								if (buttonAction.Action != ButtonActionType.Press) break;
								this.focusState = this.roomX == -1 ? FocusStates.AlarmClock : FocusStates.Bomb;
								Message($"{this.focusState} selected");
								break;
							case FocusStates.AlarmClock:
								if (buttonAction.Action != ButtonActionType.Press) break;
								this.SetAlarmClock(!isAlarmClockOn);
								break;
							case FocusStates.Bomb:
								if (buttonAction.Action != ButtonActionType.Press) break;
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
									Message($"{component.Reader.Name} [{module1.ID}] ({this.SelectedFace.X + 1}, {this.SelectedFace.Y + 1}) selected");
								} else
									Message($"Can't select {component.Reader.Name}.");
								break;
							case FocusStates.Module:
								if (this.SelectedFace.SelectedComponent is Module module2) {
									switch (buttonAction.Action) {
										case ButtonActionType.Hold:
											module2.Interact();
											break;
										case ButtonActionType.Release:
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
					case Button.B:
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
					case Button.Left:
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
					case Button.Right:
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
					case Button.Up:
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
					case Button.Down:
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
				}
				break;
		}
	}

	/// <summary>Handles moving the selection to the correct module after changing rows.</summary>
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

	/// <summary>Simulates the specified controller input actions.</summary>
	public void SendInputs(IEnumerable<IInputAction> actions) {
		foreach (var action in actions)
			this.actionQueue.Enqueue(action);
		if (!this.queueTimer.Enabled) {
			this.queueTimer.Start();
			this.PopInputQueue();
		}
	}

	/// <summary>Cancels any queued input actions.</summary>
	public void CancelInputs() {
		this.actionQueue.Clear();
		this.queueTimer.Stop();
	}

	/// <summary>Returns the <see cref="ComponentReader"/> instance that handles the component at the specified point.</summary>
	public ComponentReader? GetComponentReader(Quadrilateral quadrilateral) => this.GetComponent(quadrilateral)?.Reader;
	/// <summary>Returns the <see cref="ComponentReader"/> instance that handles the component in the specified slot.</summary>
	public ComponentReader? GetComponentReader(Slot slot) => this.moduleFaces[slot.Face].Slots[slot.Y, slot.X]?.Reader;

	/// <summary>Reads component data of the specified type from the component at the specified point.</summary>
	public T ReadComponent<T>(Quadrilateral quadrilateral) where T : notnull {
		var component = this.GetComponent(quadrilateral) ?? throw new ArgumentException("Attempted to read an empty component slot.");
		return component is TimerComponent timerComponent
			? timerComponent.Details is T t ? t : throw new ArgumentException("Wrong type for specified component.")
			: component switch {
				Module<T> module => module.Details,
				NeedyModule<T> needyModule => needyModule.Details,
				_ => throw new ArgumentException("Wrong type for specified component")
			};
	}
	[Obsolete("This method is being replaced with the generic overload.")]
	public string ReadModule(string type, Quadrilateral quadrilateral) {
		var component = this.GetComponent(quadrilateral) ?? throw new ArgumentException("Attempt to read blank component");
		return component.Reader.GetType().Name.Equals(type, StringComparison.OrdinalIgnoreCase)
			? component.DetailsString
			: throw new ArgumentException("Wrong type for specified component.");
	}

	/// <summary>Returns the light state of the module at the specified point.</summary>
	public ModuleLightState GetLightState(Quadrilateral quadrilateral) => this.GetComponent(quadrilateral) is Module module && module is not NeedyModule ? module.LightState : ModuleLightState.Off;

	private BombComponent? GetComponent(Quadrilateral quadrilateral) {
		switch (this.focusState) {
			case FocusStates.Bomb: {
				var face = this.currentFace switch { BombFaces.Face1 => this.moduleFaces[0], BombFaces.Face2 => this.moduleFaces[1], _ => throw new InvalidOperationException($"Can't identify modules from face {this.currentFace}.") };
				var slotX = quadrilateral.TopLeft.X switch { 558 or 572 => 0, 848 or 852 => 1, 1127 or 1134 => 2, _ => throw new ArgumentException($"Unknown x coordinate: {quadrilateral.TopLeft.X}") };
				var slotY = quadrilateral.TopLeft.Y switch { 291 or 292 => 0, 558 => 1, _ => throw new ArgumentException($"Unknown y coordinate: {quadrilateral.TopLeft.Y}") };
				return face.Slots[slotY, slotX];
			}
			case FocusStates.Module: {
				var face = this.SelectedFace;
				var slotDX = quadrilateral.TopLeft.X switch { <= 220 => -2, <= 535 => -1, <= 840 => 0, <= 1164 => 1, _ => 2 };
				var slotDY = quadrilateral.TopLeft.Y switch { <= 102 => -1, <= 393 => 0, _ => 1 };
				return face.Slots[face.Y + slotDY, face.X + slotDX];
			}
			default:
				throw new InvalidOperationException($"Can't identify modules from state {this.focusState}.");
		}
	}

	/// <summary>Returns the <see cref="WidgetReader"/> instance that handles the widget at the specified point.</summary>
	public WidgetReader? GetWidgetReader(Quadrilateral quadrilateral) => this.GetWidget(quadrilateral)?.Reader;

	/// <summary>Reads widget data of the specified type from the widget at the specified point.</summary>
	public T ReadWidget<T>(Quadrilateral quadrilateral) where T : notnull {
		var widget = this.GetWidget(quadrilateral) ?? throw new ArgumentException("Attempt to read blank widget.");
		return widget is Widget<T> widget2 ? widget2.Details : throw new ArgumentException("Wrong type for specified widget.");
	}
	[Obsolete("This method is being replaced with the generic overload.")]
	public string ReadWidget(string type, Quadrilateral quadrilateral) {
		var widget = this.GetWidget(quadrilateral) ?? throw new ArgumentException("Attempt to read blank widget.");
		return widget.Reader.GetType().Name.Equals(type, StringComparison.OrdinalIgnoreCase)
			? widget.DetailsString
			: throw new ArgumentException("Wrong type for specified widget.");
	}

	private Widget? GetWidget(Quadrilateral quadrilateral) {
		switch (this.focusState) {
			case FocusStates.Bomb: {
				var face = this.currentFace switch { BombFaces.Side1 => this.widgetFaces[0], BombFaces.Side2 => this.widgetFaces[1], _ => this.ry switch { < -0.5f => this.widgetFaces[2], >= 0.5f => this.widgetFaces[3], _ => throw new InvalidOperationException($"Can't identify widgets from face {this.currentFace}.") } };
				int slot;
				if (face.Slots.Length == 4) {
					var slotX = quadrilateral.TopLeft.X switch { < 900 => 0, _ => 1 };
					var slotY = quadrilateral.TopLeft.Y switch { 465 => 0, 772 => 1, _ => throw new ArgumentException($"Unknown y coordinate: {quadrilateral.TopLeft.Y}") };
					slot = slotY * 2 + slotX;
				} else {
					var slotX = quadrilateral.TopLeft.X switch { <= 588 => 0, <= 824 => 1, _ => 2 };
					var slotY = quadrilateral.TopLeft.Y switch { 430 => 0, 566 => 1, _ => throw new ArgumentException($"Unknown y coordinate: {quadrilateral.TopLeft.Y}") };
					slot = slotY * 3 + slotX;
				}
				return face.Slots[slot];
			}
			default:
				throw new InvalidOperationException($"Can't identify widgets from state {this.focusState}.");
		}
	}

	/// <summary>Disarms the selected module.</summary>
	public void Solve() {
		if (this.SelectedFace.SelectedComponent is Module module)
			module.Solve();
	}

	/// <summary>Triggers a strike on the selected module.</summary>
	public void Strike() {
		if (this.SelectedFace.SelectedComponent is Module module)
			module.StrikeFlash();
	}

	/// <summary>Triggers the alarm clock.</summary>
	internal void SetAlarmClock(bool value) {
		Message($"Turned alarm clock {(value ? "on" : "off")}");
		isAlarmClockOn = value;
		this.Postback?.Invoke(this, $"OOB AlarmClock {value}");
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

	private class WidgetFace(Simulation.Widget?[] slots) {
		internal readonly Widget?[] Slots = slots ?? throw new ArgumentNullException(nameof(slots));
	}

	private abstract class BombComponent(ComponentReader reader) {
		internal ComponentReader Reader { get; } = reader ?? throw new ArgumentNullException(nameof(reader));
		internal abstract string DetailsString { get; }
		public event EventHandler<string>? PostbackSent;

		protected void Postback(string message) => this.PostbackSent?.Invoke(this, message);
	}

	private abstract class Module : BombComponent {
		private readonly Timer ResetLightTimer = new(2000) { AutoReset = false };

		private static int NextID;

		internal int ID { get; }
		public ModuleLightState LightState { get; private set; }

		public int X;
		public int Y;
		protected bool[,] SelectableGrid { get; }

		public event EventHandler? Strike;

		public Module(ComponentReader reader, int selectableWidth, int selectableHeight) : base(reader) {
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
			Message($"{this.Reader.Name} solved.");
			this.LightState = ModuleLightState.Solved;
			this.ResetLightTimer.Stop();
		}

		public void StrikeFlash() {
			Message($"{this.Reader.Name} strike.");
			if (this.LightState != ModuleLightState.Solved) {
				this.LightState = ModuleLightState.Strike;
				this.ResetLightTimer.Stop();
				this.ResetLightTimer.Start();
				this.Strike?.Invoke(this, EventArgs.Empty);
			}
		}

		public virtual void Interact() => Message($"Selected ({this.X}, {this.Y}) in {this.Reader.Name}");
		public virtual void StopInteract() { }
	}

	private abstract class Module<TDetails>(ComponentReader<TDetails> reader, TDetails details, int selectableWidth, int selectableHeight) : Module(reader, selectableWidth, selectableHeight) where TDetails : notnull {
		internal virtual TDetails Details { get; } = details;
		internal override string DetailsString => this.Details.ToString() ?? "";

		protected Module(ComponentReader<TDetails> reader, int selectableWidth, int selectableHeight) : this(reader, default!, selectableWidth, selectableHeight) { }
	}

	private abstract class NeedyModule : Module {
		private int faceNum;
		private int x;
		private int y;
		private TimeSpan baseTime;
		private readonly Stopwatch stopwatch = new();

		public virtual TimeSpan StartingTime => TimeSpan.FromSeconds(45);
		public virtual bool AutoReset => true;

		public bool IsActive { get; private set; }
		public TimeSpan RemainingTime => this.stopwatch is not null ? this.baseTime - this.stopwatch.Elapsed : TimeSpan.Zero;
		public int? DisplayedTime => this.IsActive ? (int?) this.RemainingTime.TotalSeconds : null;
		protected Timer Timer { get; } = new() { AutoReset = false };

		protected NeedyModule(ComponentReader reader, int selectableWidth, int selectableHeight) : base(reader, selectableWidth, selectableHeight)
			=> this.Timer.Elapsed += this.ReactivateTimer_Elapsed;

		public void Initialise(int faceNum, int x, int y) {
			this.faceNum = faceNum;
			this.x = x;
			this.y = y;
			this.Postback($"OOB NeedyStateChange {this.faceNum} {this.x} {this.y} AwaitingActivation");
			this.Timer.Interval = 10000;
			this.Timer.Start();
		}

		public void Activate() {
			this.baseTime = this.StartingTime;
			this.Timer.Interval = this.baseTime.TotalMilliseconds;
			this.stopwatch.Restart();
			this.IsActive = true;
			Message($"{this.Reader.Name} activated with {this.baseTime} left.");
			this.OnActivate();
			this.Postback($"OOB NeedyStateChange {this.faceNum} {this.x} {this.y} Running");
		}

		protected abstract void OnActivate();

		public void Deactivate() {
			if (!this.IsActive) return;
			this.IsActive = false;
			this.stopwatch.Stop();
			Message($"{this.Reader.Name} deactivated with {this.RemainingTime} left.");
			if (this.AutoReset) {
				this.Timer.Interval = 30000;
				this.Postback($"OOB NeedyStateChange {this.faceNum} {this.x} {this.y} Cooldown");
			} else {
				this.Timer.Stop();
				this.Postback($"OOB NeedyStateChange {this.faceNum} {this.x} {this.y} Terminated");
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

	private abstract class NeedyModule<TDetails>(ComponentReader<TDetails> reader, TDetails details, int selectableWidth, int selectableHeight) : NeedyModule(reader, selectableWidth, selectableHeight) where TDetails : notnull {
		internal virtual TDetails Details { get; } = details;
		internal override string DetailsString => this.Details.ToString() ?? "";

		protected NeedyModule(ComponentReader<TDetails> reader, int selectableWidth, int selectableHeight) : this(reader, default!, selectableWidth, selectableHeight) { }
	}

	private class TimerComponent : BombComponent {
		public TimeSpan Elapsed => this.stopwatch.Elapsed;

		internal static TimerComponent Instance { get; } = new();

		private readonly Stopwatch stopwatch = Stopwatch.StartNew();

		internal Components.Timer.ReadData Details {
			get {
				var elapsed = this.stopwatch.ElapsedTicks;
				var seconds = elapsed / Stopwatch.Frequency;
				return new(GameMode.Zen, (int) seconds, seconds < 60 ? (int) (elapsed / (Stopwatch.Frequency / 100) % 100) : 0, 0);
			}
		}
		internal override string DetailsString => this.Details.ToString();

		private TimerComponent() : base(new Components.Timer()) { }
	}

	private abstract class Widget(WidgetReader reader) {
		internal WidgetReader Reader { get; } = reader ?? throw new ArgumentNullException(nameof(reader));
		internal abstract string DetailsString { get; }

		internal static Widget<T> Create<T>(WidgetReader<T> reader, T details) where T : notnull => new(reader, details);
	}

	private class Widget<T>(WidgetReader<T> reader, T details) : Widget(reader) where T : notnull {
		internal T Details { get; } = details;
		internal override string DetailsString => this.Details.ToString() ?? "";
	}
}
