using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using WireFlags = BombDefuserConnector.Components.ComplicatedWires.WireFlags;

namespace BombDefuserConnector;
internal class Simulation {
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

	private ComponentFace SelectedFace => this.moduleFaces[this.selectedFaceNum];

	public Simulation() {
		this.queueTimer.Elapsed += this.QueueTimer_Elapsed;
		this.rxTimer.Elapsed += this.RXTimer_Elapsed;

		this.moduleFaces[0] = new(new BombComponent?[,] {
			{ TimerComponent.Instance, new Modules.ComplicatedWires(new[] { WireFlags.Red, WireFlags.None, WireFlags.None, WireFlags.Blue }), new Modules.ComplicatedWires(new[] { WireFlags.Red, WireFlags.Blue, WireFlags.Blue | WireFlags.Light }) },
			{ null, null, new Modules.ComplicatedWires(new[] { WireFlags.Red, WireFlags.Blue, WireFlags.Red }) }
		});
		this.moduleFaces[1] = new(new BombComponent?[,] {
			{ null, null, null },
			{ null, null, null }
		});
		this.widgetFaces[0] = new(new Widget?[] { Widget.Create(new Widgets.SerialNumber(), "AB3DE6"), null, null, null });
		this.widgetFaces[1] = new(new Widget?[] { Widget.Create(new Widgets.Indicator(), new(false, "BOB")), Widget.Create(new Widgets.Indicator(), new(true, "FRQ")), null, null });
		this.widgetFaces[2] = new(new Widget?[] { Widget.Create(new Widgets.BatteryHolder(), 2), null, null, null, null, null });
		this.widgetFaces[3] = new(new Widget?[] { Widget.Create(new Widgets.PortPlate(), new Widgets.PortPlate.Ports(Widgets.PortPlate.PortType.Parallel | Widgets.PortPlate.PortType.Serial)), Widget.Create(new Widgets.PortPlate(), new Widgets.PortPlate.Ports(0)), null, null, null, null });
		Message("Simulation initialised.");

		foreach (var face in this.moduleFaces) {
			foreach (var module in face.Slots) {
				if (module is NeedyModule needyModule)
					needyModule.Activate();
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
		var tokens2 = this.actionQueue.Dequeue().Split(':');
		if (this.actionQueue.Count == 0) this.queueTimer.Stop();
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
						SetAlarmClock(!isAlarmClockOn);
						break;
					case FocusStates.Bomb:
						if ((int) this.currentFace % 2 != 0) {
							this.currentFace = (BombFaces) (((int) this.currentFace + 1) % 4);
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
							Message($"Aligned the bomb to {this.currentFace}");
						}
						this.focusState = FocusStates.Room;
						if ((int) this.currentFace % 2 != 0) {
							this.currentFace = (BombFaces) (((int) this.currentFace + 1) % 4);
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
							module.X--;
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
							module.X++;
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
							module.Y--;
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
							module.Y++;
						break;
				}
				break;
		}
	}

	internal void SimulateScreenshot(string token) {
		Task.Run(async () => {
			await Task.Delay(50);
			AimlVoice.Program.sendInput($"OOB ScreenshotReady {token} {Guid.NewGuid():N}");
		});
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

	public string? IdentifyComponent(int x1, int y1) => this.GetComponent(x1, y1)?.Processor.GetType().Name;
	public string? IdentifyComponent(int face, int x, int y) => this.moduleFaces[face - 1].Slots[y - 1, x - 1]?.Processor.GetType().Name;

	public T ReadComponent<T>(ComponentProcessor<T> processor, int x1, int y1) where T : notnull {
		var component = this.GetComponent(x1, y1);
		if (component is null)
			throw new ArgumentException("Attempt to read blank component");
		if (processor is Components.Timer) {
			if (component is not TimerComponent timerComponent || timerComponent.Details is not T t)
				throw new ArgumentException("Wrong type for specified component.");
			return t;
		}
		if (component is not Module<T> module)
			throw new ArgumentException("Attempt to read something that isn't a module");
		if (component.Processor.GetType() != processor.GetType())
			throw new ArgumentException("Wrong type for specified component.");
		return module.Details;
	}
	public string ReadModule(string type, int x1, int y1) {
		var component = this.GetComponent(x1, y1);
		if (component is null)
			throw new ArgumentException("Attempt to read blank component");
		if (!component.Processor.GetType().Name.Equals(type, StringComparison.InvariantCultureIgnoreCase))
			throw new ArgumentException("Wrong type for specified component.");
		return component.DetailsString;
	}

	public ModuleLightState GetLightState(int x1, int y1) => this.GetComponent(x1, y1) is Module module ? module.LightState : ModuleLightState.Off;

	private BombComponent? GetComponent(int x1, int y1) {
		switch (this.focusState) {
			case FocusStates.Bomb: {
				var face = this.currentFace switch { BombFaces.Face1 => this.moduleFaces[0], BombFaces.Face2 => this.moduleFaces[1], _ => throw new InvalidOperationException($"Can't identify modules from face {this.currentFace}.") };
				var slotX = x1 switch { 558 or 572 => 0, 848 or 852 => 1, 1127 or 1134 => 2, _ => throw new ArgumentException($"Unknown x coordinate: {x1}") };
				var slotY = y1 switch { 291 or 292 => 0, 558 => 1, _ => throw new ArgumentException($"Unknown y coordinate: {y1}") };
				return face.Slots[slotY, slotX];
			}
			case FocusStates.Module: {
				var face = this.SelectedFace;
				var slotDX = x1 switch { <= 220 => -2, <= 535 => -1, <= 840 => 0, <= 1164 => 1, _ => 2 };
				var slotDY = y1 switch { <= 102 => -1, <= 393 => 0, _ => 1 };
				return face.Slots[face.Y + slotDY, face.X + slotDX];
			}
			default:
				throw new InvalidOperationException($"Can't identify modules from state {this.focusState}.");
		}
	}

	public string? IdentifyWidget(int x1, int y1) => this.GetWidget(x1, y1)?.Processor.GetType().Name;

	public T ReadWidget<T>(WidgetProcessor<T> processor, int x1, int y1) where T : notnull {
		var widget = this.GetWidget(x1, y1);
		if (widget is null)
			throw new ArgumentException("Attempt to read blank widget.");
		if (widget is not Widget<T> widget2)
			throw new ArgumentException("Wrong type for specified widget.");
		return widget2.Details;
	}
	public string ReadWidget(string type, int x1, int y1) {
		var widget = this.GetWidget(x1, y1);
		if (widget is null)
			throw new ArgumentException("Attempt to read blank widget.");
		if (!widget.Processor.GetType().Name.Equals(type, StringComparison.InvariantCultureIgnoreCase))
			throw new ArgumentException("Wrong type for specified widget.");
		return widget.DetailsString;
	}

	private Widget? GetWidget(int x1, int y1) {
		switch (this.focusState) {
			case FocusStates.Bomb: {
				var face = this.currentFace switch { BombFaces.Side1 => this.widgetFaces[0], BombFaces.Side2 => this.widgetFaces[1], _ => this.ry switch { < -0.5f => this.widgetFaces[2], >= 0.5f => this.widgetFaces[3], _ => throw new InvalidOperationException($"Can't identify widgets from face {this.currentFace}.") } };
				int slot;
				if (face.Slots.Length == 4) {
					var slotX = x1 switch { < 900 => 0, _ => 1 };
					var slotY = y1 switch { 465 => 0, 772 => 1, _ => throw new ArgumentException($"Unknown y coordinate: {y1}") };
					slot = slotY * 2 + slotX;
				} else {
					var slotX = x1 switch { <= 588 => 0, <= 824 => 1, _ => 2 };
					var slotY = y1 switch { 430 => 0, 566 => 1, _ => throw new ArgumentException($"Unknown y coordinate: {y1}") };
					slot = slotY * 3 + slotX;
				}
				return face.Slots[slot];
			}
			default:
				throw new InvalidOperationException($"Can't identify widgets from state {this.focusState}.");
		}
	}

	public void Solve() {
		if (this.SelectedFace.SelectedComponent is Module module) {
			module.Solve();
		}
	}

	public void Strike() {
		if (this.SelectedFace.SelectedComponent is Module module) {
			module.StrikeFlash();
		}
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

		public Module(ComponentProcessor processor) : base(processor) {
			++NextID;
			this.ID = NextID;
			this.ResetLightTimer.Elapsed += this.ResetLightTimer_Elapsed;
		}

		private void ResetLightTimer_Elapsed(object? sender, ElapsedEventArgs e) => this.LightState = ModuleLightState.Off;

		public void Solve() {
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

		public virtual void Interact() {
			Message($"Selected ({this.X}, {this.Y}) in {this.Processor.Name}");
		}
		public virtual void StopInteract() { }
	}

	private abstract class Module<TDetails> : Module where TDetails : notnull {
		internal virtual TDetails Details { get; }
		internal override string DetailsString => this.Details.ToString() ?? "";

		protected Module(ComponentProcessor<TDetails> processor) : this(processor, default!) { }
		public Module(ComponentProcessor<TDetails> processor, TDetails details) : base(processor) => this.Details = details;
	}

	private abstract class NeedyModule : Module {
		private readonly Stopwatch stopwatch = new();
		private readonly TimeSpan startingTime;
		private TimeSpan initialTime;

		public bool IsActive { get; private set; }
		public TimeSpan RemainingTime => this.stopwatch is not null ? this.initialTime - this.stopwatch.Elapsed : TimeSpan.Zero;

		protected NeedyModule(ComponentProcessor processor, TimeSpan startingTime) : base(processor) => this.startingTime = startingTime;

		public void Activate() {
			this.initialTime = this.startingTime;
			this.stopwatch.Restart();
			this.IsActive = true;
		}

		public void Deactivate() {
			Message($"{this.Processor.Name} deactivated with {this.RemainingTime.TotalSeconds}s left.");
			this.stopwatch.Stop();
			this.IsActive = false;
		}

		public void AddTime(TimeSpan time, TimeSpan max) {
			this.initialTime += time;
			if (this.RemainingTime > max) {
				this.initialTime = max;
				this.stopwatch.Restart();
			}
		}
	}

	private abstract class NeedyModule<TProcessor, TDetails> : NeedyModule where TProcessor : ComponentProcessor<TDetails> where TDetails : notnull {
		internal virtual TDetails Details { get; }
		internal override string DetailsString => this.Details.ToString() ?? "";

		protected NeedyModule(TimeSpan startingTime) : this(default!, startingTime) { }
		protected NeedyModule(TDetails details, TimeSpan startingTime) : base(BombDefuserAimlService.GetComponentProcessor<TProcessor>(), startingTime) => this.Details = details;
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

	private static class Modules {
		public class Wires : Module<Components.Wires.ReadData> {
			internal override Components.Wires.ReadData Details => new(this.wires);

			private readonly Components.Wires.Colour[] wires;
			private bool[] isCut;
			private int shouldCut;

			public Wires(int shouldCut, params Components.Wires.Colour[] wires) : base(BombDefuserAimlService.GetComponentProcessor<Components.Wires>()) {
				this.shouldCut = shouldCut;
				this.wires = wires;
				this.isCut = new bool[wires.Length];
			}

			public override void Interact() {
				Message($"Cut wire {this.Y + 1}");
				if (this.isCut[this.Y])
					return;
				this.isCut[this.Y] = true;
				if (this.Y == this.shouldCut)
					this.Solve();
				else
					this.StrikeFlash();
			}
		}

		public class ComplicatedWires : Module<Components.ComplicatedWires.ReadData> {
			internal static readonly ComplicatedWires Test1 = new(new WireFlags[] { WireFlags.None, WireFlags.None, WireFlags.Blue });
			internal static readonly ComplicatedWires Test2 = new(new WireFlags[] { WireFlags.Blue, WireFlags.Red, WireFlags.Blue | WireFlags.Light });
			internal static readonly ComplicatedWires Test3 = new(new WireFlags[] { WireFlags.Red, WireFlags.Blue | WireFlags.Star, WireFlags.Blue | WireFlags.Light });

			internal override Components.ComplicatedWires.ReadData Details => new(this.X, this.wires);

			public static bool[] ShouldCut = new bool[16];
			private readonly WireFlags[] wires;
			private bool[] isCut;

			static ComplicatedWires() {
				ShouldCut[(int) WireFlags.None] = true;
				ShouldCut[(int) WireFlags.Blue] = true;
				ShouldCut[(int) (WireFlags.Blue | WireFlags.Star)] = true;
				ShouldCut[(int) (WireFlags.Blue | WireFlags.Light)] = true;
				ShouldCut[(int) (WireFlags.Blue | WireFlags.Star | WireFlags.Light)] = true;
			}

			public ComplicatedWires(WireFlags[] wires) : base(BombDefuserAimlService.GetComponentProcessor<Components.ComplicatedWires>()) {
				this.wires = wires;
				this.isCut = new bool[wires.Length];
			}

			public override void Interact() {
				Message($"Cut wire {this.X + 1}");
				if (this.isCut[this.X])
					return;
				this.isCut[this.X] = true;
				if (!ShouldCut[(int) this.wires[this.X]])
					this.StrikeFlash();
				else {
					for (int i = 0; i < this.wires.Length; i++) {
						if (ShouldCut[(int) this.wires[i]] && !this.isCut[i]) return;
					}
					this.Solve();
				}
			}
		}

		public class Button : Module<Components.Button.ReadData> {
			private readonly Components.Button.Colour colour;
			private readonly Components.Button.Label label;
			private Components.Button.Colour? indicatorColour;
			private int correctDigit;
			private readonly Timer pressTimer = new(500) { AutoReset = false };

			internal override Components.Button.ReadData Details => new(this.colour, this.label, this.indicatorColour);

			public Button(Components.Button.Colour colour, Components.Button.Label label) : base(BombDefuserAimlService.GetComponentProcessor<Components.Button>()) {
				this.colour = colour;
				this.label = label;
				this.pressTimer.Elapsed += this.PressTimer_Elapsed;
			}

			public override void Interact() {
				this.pressTimer.Start();
			}
			public override void StopInteract() {
				bool correct;
				this.pressTimer.Stop();
				if (indicatorColour is not null) {
					var elapsed = TimerComponent.Instance.Elapsed;
					var time = elapsed.Ticks;
					Message($"Button released at {elapsed.TotalSeconds}");
					if (time >= Stopwatch.Frequency * 60) {
						correct =
							(time / (Stopwatch.Frequency * 600) % 10 == correctDigit)
							|| (time / (Stopwatch.Frequency * 60) % 10 == correctDigit)
							|| (time / (Stopwatch.Frequency * 10) % 10 == correctDigit)
							|| (time / Stopwatch.Frequency % 10 == correctDigit);
					} else {
						correct =
							(time / (Stopwatch.Frequency * 10) % 10 == correctDigit)
							|| (time / Stopwatch.Frequency % 10 == correctDigit)
							|| (time / (Stopwatch.Frequency / 10) % 10 == correctDigit)
							|| (time / (Stopwatch.Frequency / 100) % 10 == correctDigit);
					}
					indicatorColour = null;
				} else {
					Message("Button tapped");
					correct = false;
				}
				if (correct) this.Solve();
				else this.StrikeFlash();
			}

			private void PressTimer_Elapsed(object? sender, ElapsedEventArgs e) {
				this.indicatorColour = Components.Button.Colour.Blue;
				this.correctDigit = 4;
				Message($"Button held - indicator is {this.indicatorColour}");
			}
		}

		public class Keypad : Module<Components.Keypad.ReadData> {
			internal override Components.Keypad.ReadData Details => new(this.symbols);

			private readonly Components.Keypad.Symbol[] symbols;
			private readonly int[] correctOrder;
			private readonly bool[] isPressed;

			public Keypad(Components.Keypad.Symbol[] symbols, int[] correctOrder) : base(BombDefuserAimlService.GetComponentProcessor<Components.Keypad>()) {
				this.symbols = symbols;
				this.correctOrder = correctOrder;
				this.isPressed = new bool[symbols.Length];
			}

			public override void Interact() {
				if (this.X is < 0 or >= 2 || this.Y is < 0 or >= 2) throw new InvalidOperationException("Invalid highlight position");
				var index = this.Y * 2 + this.X;
				Message($"Pressed button {index}");
				if (this.isPressed[index])
					return;
				foreach (var i in this.correctOrder) {
					if (i == index) break;
					if (!this.isPressed[i]) {
						this.StrikeFlash();
						return;
					}
				}
				this.isPressed[index] = true;
				if (!this.isPressed.Contains(false))
					this.Solve();
			}
		}
		/*
		public class CapacitorDischarge : NeedyModule<int> {
			private Stopwatch pressStopwatch = new();

			internal override int Details => (int) this.RemainingTime.TotalSeconds;

			public CapacitorDischarge() : base(null, TimeSpan.FromSeconds(45)) { }

			public override void Interact() {
				Message($"{this.Processor.Name} pressed with {this.RemainingTime.TotalSeconds} s left.");
				pressStopwatch.Restart();
			}

			public override void StopInteract() {
				this.AddTime(pressStopwatch.Elapsed * 5, TimeSpan.FromSeconds(45));
				Message($"{this.Processor.Name} released with {this.RemainingTime.TotalSeconds} s left.");
			}
		}
		*/
	}
}
