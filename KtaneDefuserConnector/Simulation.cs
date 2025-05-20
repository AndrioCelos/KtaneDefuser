using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Timers;
using KtaneDefuserConnector.Components;
using KtaneDefuserConnector.DataTypes;
using KtaneDefuserConnector.Widgets;
using KtaneDefuserConnectorApi;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Timer = System.Timers.Timer;

namespace KtaneDefuserConnector;
/// <summary>Provides a simulation of a Keep Talking and Nobody Explodes game, for testing without the real game.</summary>
internal partial class Simulation {
	private readonly ILoggerFactory _loggerFactory;
	private readonly ILogger _logger;
	private int _roomX;
	private readonly Timer _rxTimer = new(187.5);
	private bool _rxBetweenFaces;
	private float _ry;
	private FocusStates _focusState;
	private int _selectedFaceNum;
	private BombFaces _currentFace;
	private readonly Queue<IInputAction> _actionQueue = new();
	private readonly Timer _queueTimer = new(167);
	private readonly ComponentFace[] _moduleFaces = new ComponentFace[2];
	private readonly WidgetFace[] _widgetFaces = new WidgetFace[4];
	private bool _isAlarmClockOn;

	private Random Random { get; } = new();
	internal static Image<Rgba32> DummyScreenshot { get; } = new(1, 1);

	private ComponentFace SelectedFace => _moduleFaces[_selectedFaceNum];

	internal event EventHandler<string>? Postback;

	public Simulation(ILoggerFactory loggerFactory) {
		_loggerFactory = loggerFactory;
		_logger = loggerFactory.CreateLogger<Simulation>();
		_queueTimer.Elapsed += QueueTimer_Elapsed;
		_rxTimer.Elapsed += RXTimer_Elapsed;

		_moduleFaces[0] = new(new BombComponent?[,] {
			{
				TimerComponent.Instance,
				new Modules.Wires(this, 0, Wires.Colour.Black, Wires.Colour.White, Wires.Colour.Blue),
				new Modules.Keypad(this, [Keypad.Symbol.QuestionMark, Keypad.Symbol.HollowStar, Keypad.Symbol.LeftC, Keypad.Symbol.Balloon], [3, 2, 1, 0])
			},
			{
				new Modules.Password(this),
				new Modules.Button(this, Components.Button.Colour.Red, "ABORT"),
				new Modules.ComplicatedWires(this, [ComplicatedWires.WireFlags.None, ComplicatedWires.WireFlags.Red, ComplicatedWires.WireFlags.Blue, ComplicatedWires.WireFlags.Light, ComplicatedWires.WireFlags.Star])
			}
		});
		_moduleFaces[1] = new(new BombComponent?[,] {
			{
				new Modules.Maze(this, new(0, 0), new(6, 6), new(0, 1), new(5, 2)),
				new Modules.Memory(this),
				new Modules.MorseCode(this)
			},
			{
				new Modules.SimonSays(this),
				new Modules.WhosOnFirst(this),
				new Modules.WireSequence(this)
			}
		});
		_widgetFaces[0] = new([Widget.Create(new SerialNumber(), "AB3DE6"), null, null, null]);
		_widgetFaces[1] = new([Widget.Create(new Indicator(), new(false, "BOB")), Widget.Create(new Indicator(), new(true, "FRQ")), null, null
		]);
		_widgetFaces[2] = new([Widget.Create(new BatteryHolder(), 2), null, null, null, null, null]);
		_widgetFaces[3] = new([Widget.Create(new PortPlate(), new(PortPlate.PortType.Parallel | PortPlate.PortType.Serial)), Widget.Create(new PortPlate(), new(0)), null, null, null, null]);
		LogInitialised();

		for (var i = 0; i < _moduleFaces.Length; i++) {
			for (var y = 0; y < _moduleFaces[i].Slots.GetLength(0); y++) {
				for (var x = 0; x < _moduleFaces[i].Slots.GetLength(1); x++) {
					if (_moduleFaces[i].Slots[y, x] is not { } component) continue;
					component.PostbackSent += (_, m) => Postback?.Invoke(this, m);
					if (component is not Module module) continue;
					var slot = new Slot(0, i, x, y);
					module.Strike += (_, _)
						=> Postback?.Invoke(this, $"OOB Strike {slot.Bomb} {slot.Face} {slot.X} {slot.Y}");
					module.InitialiseHighlight();
					if (module is NeedyModule needyModule)
						needyModule.Initialise(i, x, y);
				}
			}
		}
	}

	private void RXTimer_Elapsed(object? sender, ElapsedEventArgs e) {
		if (_rxBetweenFaces)
			_rxBetweenFaces = false;
		else {
			_rxBetweenFaces = true;
			_currentFace = (BombFaces) (((int) _currentFace + 1) % 4);
			var faceNum = ((int) _currentFace + 1) / 2 % 2;
			if (faceNum != _selectedFaceNum) {
				_selectedFaceNum = faceNum;
				if (_focusState == FocusStates.Module) {
					_focusState = FocusStates.Bomb;
					LogModuleDeselected();
				}
			}
			LogBombTurned(_currentFace);
		}
	}

	private void QueueTimer_Elapsed(object? sender, ElapsedEventArgs e) => PopInputQueue();

	private void PopInputQueue() {
		if (!_actionQueue.TryDequeue(out var action)) {
			_queueTimer.Stop();
			return;
		}
		switch (action) {
			case CallbackAction callbackAction:
				Postback?.Invoke(this, $"OOB DefuserCallback {callbackAction.Token:N}");
				break;
			case AxisAction axisAction:
				switch (axisAction.Axis) {
					case Axis.RightStickX:
						if (axisAction.Value > 0)
							_rxTimer.Start();
						else
							_rxTimer.Stop();
						break;
					case Axis.RightStickY:
						_ry = axisAction.Value;
						break;
				}
				break;
			case ButtonAction buttonAction:
				switch (buttonAction.Button) {
					case KtaneDefuserConnectorApi.Button.A:
						switch (_focusState) {
							case FocusStates.Room:
								if (buttonAction.Action != ButtonActionType.Press) break;
								_focusState = _roomX == -1 ? FocusStates.AlarmClock : FocusStates.Bomb;
								LogFocusStateChanged(_focusState);
								break;
							case FocusStates.AlarmClock:
								if (buttonAction.Action != ButtonActionType.Press) break;
								SetAlarmClock(!_isAlarmClockOn);
								break;
							case FocusStates.Bomb:
								if (buttonAction.Action != ButtonActionType.Press) break;
								if ((int) _currentFace % 2 != 0) {
									_currentFace = (BombFaces) (((int) _currentFace + 1) % 4);
									_rxBetweenFaces = false;
									LogBombAligned(_currentFace);
								}
								var component = SelectedFace.SelectedComponent;
								switch (component) {
									case null:
										LogNoModuleHighlighted();
										break;
									case Module module1:
										_focusState = FocusStates.Module;
										LogModuleSelected(component.Reader.Name, module1.ID, SelectedFace.X + 1, SelectedFace.Y + 1);
										break;
									default:
										LogUnselectableComponent(component.Reader.Name);
										break;
								}
								break;
							case FocusStates.Module:
								if (SelectedFace.SelectedComponent is Module module2) {
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
					case KtaneDefuserConnectorApi.Button.B:
						switch (_focusState) {
							case FocusStates.AlarmClock:
							case FocusStates.Bomb:
								if ((int) _currentFace % 2 != 0) {
									_currentFace = (BombFaces) (((int) _currentFace + 1) % 4);
									_rxBetweenFaces = false;
									LogBombAligned(_currentFace);
								}
								_focusState = FocusStates.Room;
								if ((int) _currentFace % 2 != 0) {
									_currentFace = (BombFaces) (((int) _currentFace + 1) % 4);
									_rxBetweenFaces = false;
									LogBombAligned(_currentFace);
								}
								LogFocusStateChanged(_focusState);
								break;
							case FocusStates.Module:
								_focusState = FocusStates.Bomb;
								LogFocusStateChanged(_focusState);
								break;
						}
						break;
					case KtaneDefuserConnectorApi.Button.Left:
						switch (_focusState) {
							case FocusStates.Room:
								if (_roomX == 0) _roomX = -1;
								break;
							case FocusStates.Bomb:
								do {
									SelectedFace.X--;
								} while (SelectedFace.SelectedComponent is not Module);
								break;
							case FocusStates.Module:
								if (SelectedFace.SelectedComponent is Module module)
									module.MoveHighlight(Direction.Left);
								break;
						}
						break;
					case KtaneDefuserConnectorApi.Button.Right:
						switch (_focusState) {
							case FocusStates.Room:
								if (_roomX == -1) _roomX = 0;
								break;
							case FocusStates.Bomb:
								do {
									SelectedFace.X++;
								} while (SelectedFace.SelectedComponent is not Module);
								break;
							case FocusStates.Module:
								if (SelectedFace.SelectedComponent is Module module)
									module.MoveHighlight(Direction.Right);
								break;
						}
						break;
					case KtaneDefuserConnectorApi.Button.Up:
						switch (_focusState) {
							case FocusStates.Bomb:
								SelectedFace.Y--;
								FindNearestModule();
								break;
							case FocusStates.Module:
								if (SelectedFace.SelectedComponent is Module module)
									module.MoveHighlight(Direction.Up);
								break;
						}
						break;
					case KtaneDefuserConnectorApi.Button.Down:
						switch (_focusState) {
							case FocusStates.Bomb:
								SelectedFace.Y++;
								FindNearestModule();
								break;
							case FocusStates.Module:
								if (SelectedFace.SelectedComponent is Module module)
									module.MoveHighlight(Direction.Down);
								break;
						}
						break;
				}
				break;
		}
	}

	/// <summary>Handles moving the selection to the correct module after changing rows.</summary>
	private void FindNearestModule() {
		if (SelectedFace.SelectedComponent is Module) return;
		for (var d = 1; d <= 2; d++) {
			for (var dir = 0; dir < 2; dir++) {
				var x = dir == 0 ? SelectedFace.X - d : SelectedFace.X + d;
				if (x < 0 || x >= SelectedFace.Slots.GetLength(1) || SelectedFace.Slots[SelectedFace.Y, x] is not Module) continue;
				SelectedFace.X = x;
				return;
			}
		}
	}

	/// <summary>Simulates the specified controller input actions.</summary>
	public void SendInputs(IEnumerable<IInputAction> actions) {
		foreach (var action in actions)
			_actionQueue.Enqueue(action);
		if (_queueTimer.Enabled) return;
		_queueTimer.Start();
		PopInputQueue();
	}

	/// <summary>Cancels any queued input actions.</summary>
	public void CancelInputs() {
		_actionQueue.Clear();
		_queueTimer.Stop();
	}

	/// <summary>Returns the <see cref="ComponentReader"/> instance that handles the component at the specified point.</summary>
	public ComponentReader? GetComponentReader(Quadrilateral quadrilateral) => GetComponent(quadrilateral)?.Reader;
	/// <summary>Returns the <see cref="ComponentReader"/> instance that handles the component in the specified slot.</summary>
	public ComponentReader? GetComponentReader(Slot slot) => _moduleFaces[slot.Face].Slots[slot.Y, slot.X]?.Reader;

	/// <summary>Reads component data of the specified type from the component at the specified point.</summary>
	public T ReadComponent<T>(Quadrilateral quadrilateral) where T : ComponentReadData {
		var component = GetComponent(quadrilateral) ?? throw new ArgumentException("Attempted to read an empty component slot.");
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
		var component = GetComponent(quadrilateral) ?? throw new ArgumentException("Attempt to read blank component");
		return component.Reader.GetType().Name.Equals(type, StringComparison.OrdinalIgnoreCase)
			? component.DetailsString
			: throw new ArgumentException("Wrong type for specified component.");
	}

	/// <summary>Returns the light state of the module at the specified point.</summary>
	public ModuleStatus GetLightState(Quadrilateral quadrilateral) => GetComponent(quadrilateral) is Module module and not NeedyModule ? module.LightState : ModuleStatus.Off;

	private BombComponent? GetComponent(Quadrilateral quadrilateral) {
		switch (_focusState) {
			case FocusStates.Bomb: {
				var face = _currentFace switch { BombFaces.Face1 => _moduleFaces[0], BombFaces.Face2 => _moduleFaces[1], _ => throw new InvalidOperationException($"Can't identify modules from face {_currentFace}.") };
				var slotX = quadrilateral.TopLeft.X switch { 558 or 572 => 0, 848 or 852 => 1, 1127 or 1134 => 2, _ => throw new ArgumentException($"Unknown x coordinate: {quadrilateral.TopLeft.X}") };
				var slotY = quadrilateral.TopLeft.Y switch { 291 or 292 => 0, 558 => 1, _ => throw new ArgumentException($"Unknown y coordinate: {quadrilateral.TopLeft.Y}") };
				return face.Slots[slotY, slotX];
			}
			case FocusStates.Module: {
				var face = SelectedFace;
				var slotDx = quadrilateral.TopLeft.X switch { <= 220 => -2, <= 535 => -1, <= 840 => 0, <= 1164 => 1, _ => 2 };
				var slotDy = quadrilateral.TopLeft.Y switch { <= 102 => -1, <= 393 => 0, _ => 1 };
				return face.Slots[face.Y + slotDy, face.X + slotDx];
			}
			default:
				throw new InvalidOperationException($"Can't identify modules from state {_focusState}.");
		}
	}

	/// <summary>Returns the <see cref="WidgetReader"/> instance that handles the widget at the specified point.</summary>
	public WidgetReader? GetWidgetReader(Quadrilateral quadrilateral) => GetWidget(quadrilateral)?.Reader;

	/// <summary>Reads widget data of the specified type from the widget at the specified point.</summary>
	public T ReadWidget<T>(Quadrilateral quadrilateral) where T : notnull {
		var widget = GetWidget(quadrilateral) ?? throw new ArgumentException("Attempt to read blank widget.");
		return widget is Widget<T> widget2 ? widget2.Details : throw new ArgumentException("Wrong type for specified widget.");
	}
	[Obsolete("This method is being replaced with the generic overload.")]
	public string ReadWidget(string type, Quadrilateral quadrilateral) {
		var widget = GetWidget(quadrilateral) ?? throw new ArgumentException("Attempt to read blank widget.");
		return widget.Reader.GetType().Name.Equals(type, StringComparison.OrdinalIgnoreCase)
			? widget.DetailsString
			: throw new ArgumentException("Wrong type for specified widget.");
	}

	private Widget? GetWidget(Quadrilateral quadrilateral) {
		switch (_focusState) {
			case FocusStates.Bomb: {
				var face = _currentFace switch { BombFaces.Side1 => _widgetFaces[0], BombFaces.Side2 => _widgetFaces[1], _ => _ry switch { < -0.5f => _widgetFaces[2], >= 0.5f => _widgetFaces[3], _ => throw new InvalidOperationException($"Can't identify widgets from face {_currentFace}.") } };
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
				throw new InvalidOperationException($"Can't identify widgets from state {_focusState}.");
		}
	}

	/// <summary>Disarms the selected module.</summary>
	public void Solve() {
		if (SelectedFace.SelectedComponent is Module module)
			module.Solve();
	}

	/// <summary>Triggers a strike on the selected module.</summary>
	public void Strike() {
		if (SelectedFace.SelectedComponent is Module module)
			module.StrikeFlash();
	}

	/// <summary>Triggers the alarm clock.</summary>
	internal void SetAlarmClock(bool value) {
		LogAlarmClockStateChanged(value ? "on" : "off");
		_isAlarmClockOn = value;
		Postback?.Invoke(this, $"OOB AlarmClock {value}");
	}

	#region Log templates

	[LoggerMessage(LogLevel.Information, "Simulation initialised.")]
	private partial void LogInitialised();

	[LoggerMessage(LogLevel.Information, "Module deselected.")]
	private partial void LogModuleDeselected();

	[LoggerMessage(LogLevel.Information, "Turned the bomb to {Face}.")]
	private partial void LogBombTurned(BombFaces face);

	[LoggerMessage(LogLevel.Information, "{FocusState} selected.")]
	private partial void LogFocusStateChanged(FocusStates focusState);

	[LoggerMessage(LogLevel.Information, "Aligned the bomb to {Face}.")]
	private partial void LogBombAligned(BombFaces face);

	[LoggerMessage(LogLevel.Warning, "A pressed while no module is highlighted.")]
	private partial void LogNoModuleHighlighted();

	[LoggerMessage(LogLevel.Information, "{Name} [{ID}] ({X}, {Y}) selected.")]
	private partial void LogModuleSelected(string name,	int id, int x, int y);

	[LoggerMessage(LogLevel.Warning, "Can't select {Name}.")]
	private partial void LogUnselectableComponent(string name);

	[LoggerMessage(LogLevel.Information, "Turned the alarm clock {NewState}.")]
	private partial void LogAlarmClockStateChanged(string newState);

	#endregion

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

		public BombComponent? SelectedComponent => Slots[Y, X];

		public ComponentFace(BombComponent?[,] slots) {
			Slots = slots ?? throw new ArgumentNullException(nameof(slots));
			for (var y = 0; y < slots.GetLength(0); y++) {
				for (var x = 0; x < slots.GetLength(1); x++) {
					if (slots[y, x] is not Module) continue;
					X = x;
					Y = y;
					return;
				}
			}
		}
	}

	private class WidgetFace(Widget?[] slots) {
		internal readonly Widget?[] Slots = slots ?? throw new ArgumentNullException(nameof(slots));
	}

	private abstract class BombComponent(ComponentReader reader) {
		internal ComponentReader Reader { get; } = reader ?? throw new ArgumentNullException(nameof(reader));
		internal abstract string DetailsString { get; }
		public event EventHandler<string>? PostbackSent;

		protected void Postback(string message) => PostbackSent?.Invoke(this, message);
	}

	private abstract partial class Module : BombComponent {
		protected readonly Simulation Simulation;
		protected readonly ILogger Logger;
		private readonly Timer _resetLightTimer = new(2000) { AutoReset = false };

		private static int _nextId;

		// ReSharper disable once InconsistentNaming
		internal int ID { get; }
		public ModuleStatus LightState { get; private set; }

		protected Point Selection;
		protected bool[,] SelectableGrid { get; }

		public event EventHandler? Strike;

		protected Module(Simulation simulation, ComponentReader reader, int selectableWidth, int selectableHeight) : base(reader) {
			Simulation = simulation;
			Logger = simulation._loggerFactory.CreateLogger(GetType());
			_nextId++;
			ID = _nextId;
			_resetLightTimer.Elapsed += ResetLightTimer_Elapsed;
			SelectableGrid = new bool[selectableHeight, selectableWidth];
			for (var y = 0; y < selectableHeight; y++)
				for (var x = 0; x < selectableWidth; x++)
					SelectableGrid[y, x] = true;
		}

		private void ResetLightTimer_Elapsed(object? sender, ElapsedEventArgs e) => LightState = ModuleStatus.Off;

		public void InitialiseHighlight() {
			for (var y = 0; y < SelectableGrid.GetLength(0); y++) {
				for (var x = 0; x < SelectableGrid.GetLength(1); x++) {
					if (!SelectableGrid[y, x]) continue;
					Selection = new(x, y);
					return;
				}
			}
		}

		public void MoveHighlight(Direction direction) {
			for (var side = 0; side < 3; side++) {
				for (var forward = 1; forward < 3; forward++) {
					var (x1, y1, x2, y2) = direction switch {
						Direction.Up => (Selection.X - side, Selection.Y - forward, Selection.X + side, Selection.Y - forward),
						Direction.Right => (Selection.X + forward, Selection.Y - side, Selection.X + forward, Selection.Y + side),
						Direction.Down => (Selection.X - side, Selection.Y + forward, Selection.X + side, Selection.Y + forward),
						_ => (Selection.X - forward, Selection.Y - side, Selection.X - forward, Selection.Y + side)
					};
					if (x1 >= 0 && x1 < SelectableGrid.GetLength(1) && y1 >= 0 && y1 < SelectableGrid.GetLength(0)
						&& SelectableGrid[y1, x1]) {
						Selection = new(x1, y1);
						return;
					}
					if (x2 >= 0 && x2 < SelectableGrid.GetLength(1) && y2 >= 0 && y2 < SelectableGrid.GetLength(0)
						&& SelectableGrid[y2, x2]) {
						Selection = new(x2, y2);
						return;
					}
				}
			}
			throw new ArgumentException("Highlight movement went out of bounds");
		}

		public void Solve() {
			if (LightState == ModuleStatus.Solved) return;
			LogModuleSolved(Reader.Name);
			LightState = ModuleStatus.Solved;
			_resetLightTimer.Stop();
		}

		public void StrikeFlash() {
			LogModuleStrike(Reader.Name);
			if (LightState == ModuleStatus.Solved) return;
			LightState = ModuleStatus.Strike;
			_resetLightTimer.Stop();
			_resetLightTimer.Start();
			Strike?.Invoke(this, EventArgs.Empty);
		}

		public virtual void Interact() => LogModuleInteraction(Selection.X, Selection.Y, Reader.Name);
		public virtual void StopInteract() { }

		#region Log templates

		[LoggerMessage(LogLevel.Information, "{Answer} was submitted.")]
		protected partial void LogSubmit(object? answer);

		[LoggerMessage(LogLevel.Information, "Cut wire {Wire}.")]
		protected partial void LogCutWire(int wire);

		[LoggerMessage(LogLevel.Information, "{ModuleName} solved.")]
		private partial void LogModuleSolved(string moduleName);

		[LoggerMessage(LogLevel.Information, "{ModuleName} strike.")]
		private partial void LogModuleStrike(string moduleName);

		[LoggerMessage(LogLevel.Information, "Selected ({X}, {Y}) in {ModuleName}")]
		private partial void LogModuleInteraction(int x, int y, string moduleName);
			
		[LoggerMessage(LogLevel.Information, "{Button} was pressed.")]
		protected partial void LogButton(object? button);

		#endregion
	}

	private abstract class Module<TDetails>(Simulation simulation, ComponentReader<TDetails> reader, TDetails details, int selectableWidth, int selectableHeight) : Module(simulation, reader, selectableWidth, selectableHeight) where TDetails : ComponentReadData {
		internal virtual TDetails Details { get; } = details;
		internal override string DetailsString => Details.ToString() ?? "";

		protected Module(Simulation simulation, ComponentReader<TDetails> reader, int selectableWidth, int selectableHeight) : this(simulation, reader, null!, selectableWidth, selectableHeight) { }
	}

	private abstract partial class NeedyModule : Module {
		private int _faceNum;
		private int _x;
		private int _y;
		private TimeSpan _baseTime;
		private readonly Stopwatch _stopwatch = new();

		protected virtual TimeSpan StartingTime => TimeSpan.FromSeconds(45);
		protected virtual bool AutoReset => true;

		protected bool IsActive { get; private set; }
		protected TimeSpan RemainingTime => _baseTime - _stopwatch.Elapsed;
		protected int? DisplayedTime => IsActive ? (int?) RemainingTime.TotalSeconds : null;
		protected Timer Timer { get; } = new() { AutoReset = false };

		protected NeedyModule(Simulation simulation, ComponentReader reader, int selectableWidth, int selectableHeight) : base(simulation, reader, selectableWidth, selectableHeight)
			=> Timer.Elapsed += ReactivateTimer_Elapsed;

		public void Initialise(int faceNum, int x, int y) {
			_faceNum = faceNum;
			_x = x;
			_y = y;
			Postback($"OOB NeedyStateChange 0 {_faceNum} {_x} {_y} {nameof(NeedyState.AwaitingActivation)}");
			Timer.Interval = 10000;
			Timer.Start();
		}

		private void Activate() {
			_baseTime = StartingTime;
			Timer.Interval = _baseTime.TotalMilliseconds;
			_stopwatch.Restart();
			IsActive = true;
			LogNeedyModuleActivated(Reader.Name, _baseTime);
			OnActivate();
			Postback($"OOB NeedyStateChange 0 {_faceNum} {_x} {_y} {nameof(NeedyState.Running)}");
		}

		protected abstract void OnActivate();

		protected void Deactivate() {
			if (!IsActive) return;
			IsActive = false;
			_stopwatch.Stop();
			LogNeedyModuleDeactivated(Reader.Name, RemainingTime);
			if (AutoReset) {
				Timer.Interval = 30_000;
				Postback($"OOB NeedyStateChange 0 {_faceNum} {_x} {_y} {nameof(NeedyState.Cooldown)}");
			} else {
				Timer.Stop();
				Postback($"OOB NeedyStateChange 0 {_faceNum} {_x} {_y} {nameof(NeedyState.Terminated)}");
			}
		}

		protected virtual void OnTimerExpired() => StrikeFlash();

		private void ReactivateTimer_Elapsed(object? sender, ElapsedEventArgs e) {
			if (IsActive) {
				OnTimerExpired();
				Deactivate();
			} else
				Activate();
		}

		protected void AddTime(TimeSpan time, TimeSpan max) {
			_baseTime += time;
			if (RemainingTime <= max) return;
			_baseTime = max;
			_stopwatch.Restart();
		}

		#region Log templates

		[LoggerMessage(LogLevel.Information, "{ModuleName} activated with {Time} left.")]
		private partial void LogNeedyModuleActivated(string moduleName, TimeSpan time);

		[LoggerMessage(LogLevel.Information, "{ModuleName} deactivated with {Time} left.")]
		private partial void LogNeedyModuleDeactivated(string moduleName, TimeSpan time);

		#endregion
	}

	private abstract class NeedyModule<TDetails>(Simulation simulation, ComponentReader<TDetails> reader, TDetails details, int selectableWidth, int selectableHeight) : NeedyModule(simulation, reader, selectableWidth, selectableHeight) where TDetails : ComponentReadData {
		internal virtual TDetails Details { get; } = details;
		internal override string DetailsString => Details.ToString() ?? "";

		protected NeedyModule(Simulation simulation, ComponentReader<TDetails> reader, int selectableWidth, int selectableHeight) : this(simulation, reader, null!, selectableWidth, selectableHeight) { }
	}

	private class TimerComponent : BombComponent {
		public TimeSpan Elapsed => _stopwatch.Elapsed;

		internal static TimerComponent Instance { get; } = new();

		private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

		internal Components.Timer.ReadData Details {
			get {
				var elapsed = _stopwatch.ElapsedTicks;
				var seconds = elapsed / Stopwatch.Frequency;
				return new(GameMode.Zen, (int) seconds, seconds < 60 ? (int) (elapsed / (Stopwatch.Frequency / 100) % 100) : 0, 0);
			}
		}
		internal override string DetailsString => Details.ToString();

		private TimerComponent() : base(new Components.Timer()) { }
	}

	private abstract class Widget(WidgetReader reader) {
		internal WidgetReader Reader { get; } = reader ?? throw new ArgumentNullException(nameof(reader));
		internal abstract string DetailsString { get; }

		internal static Widget<T> Create<T>(WidgetReader<T> reader, T details) where T : notnull => new(reader, details);
	}

	private class Widget<T>(WidgetReader<T> reader, T details) : Widget(reader) where T : notnull {
		internal T Details { get; } = details;
		internal override string DetailsString => Details.ToString() ?? "";
	}
}
