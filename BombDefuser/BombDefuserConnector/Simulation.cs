using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Timers;

namespace BombDefuserConnector;
internal class Simulation {
	private int roomX;
	private Stopwatch? rxStopwatch;
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

		this.moduleFaces[0] = new(new BombComponent?[,] {
			{ TimerComponent.Instance, new Module.Button("blue", "HOLD"), null },
			{ null, null, null }
		});
		this.moduleFaces[1] = new(new BombComponent?[,] {
			{ null, null, null },
			{ null, null, null }
		});
		this.widgetFaces[0] = new(new Widget?[] { new("SerialNumber", "AB3DE6"), null, null, null });
		this.widgetFaces[1] = new(new Widget?[] { null, null, null, null });
		this.widgetFaces[2] = new(new Widget?[] { null, null, null, null, null, null });
		this.widgetFaces[3] = new(new Widget?[] { null, null, null, null, null, null });
		Message("Simulation initialised.");
	}

	private static void Message(string s) {
		Console.ForegroundColor = ConsoleColor.Cyan;
		Console.WriteLine($"Simulation: {s}");
		Console.ResetColor();
	}

	private void QueueTimer_Elapsed(object? sender, ElapsedEventArgs e) {
		var tokens2 = this.actionQueue.Dequeue().Split(':');
		if (this.actionQueue.Count == 0) this.queueTimer.Stop();
		switch (tokens2[0].ToLowerInvariant()) {
			case "callback":
				AimlVoice.Program.sendInput($"OOB DefuserCallback {(tokens2.Length > 1 ? tokens2[1] : "nil")}");
				break;
			case "rx":
				var v = float.Parse(tokens2[1]);
				if (v > 0) {
					this.rxStopwatch ??= Stopwatch.StartNew();
				} else if (v == 0 && this.rxStopwatch is not null) {
					var facesMoved = Math.Min(1, (int) Math.Round(this.rxStopwatch.ElapsedMilliseconds / 375.0));
					this.currentFace = (BombFaces) (((int) this.currentFace + facesMoved) % 4);
					var faceNum = ((int) this.currentFace + 1) / 2 % 2;
					if (facesMoved > 1 || faceNum != this.selectedFaceNum) {
						this.selectedFaceNum = faceNum;
						if (this.focusState == FocusStates.Module) {
							this.focusState = FocusStates.Bomb;
							Message("Module deselected");
						}
					}
					Message($"Turned the bomb to {this.currentFace}");
					this.rxStopwatch = null;
				}
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
							Message($"{component.Type} [{module1.ID}] ({this.SelectedFace.X + 1}, {this.SelectedFace.Y + 1}) selected");
						} else
							Message($"Can't select {component.Type}.");
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
						if ((int)this.currentFace % 2 != 0) {
							this.currentFace = (BombFaces) (((int) this.currentFace + 1) % 4);
							Message($"Aligned the bomb to {this.currentFace}");
						}
						this.focusState = FocusStates.Room;
						if ((int)this.currentFace % 2 != 0) {
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
						if (this.SelectedFace.X > 0) this.SelectedFace.X--;
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
						if (this.SelectedFace.X < this.SelectedFace.Slots.GetUpperBound(1)) this.SelectedFace.X++;
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
						if (this.SelectedFace.Y > 0) {
							this.SelectedFace.Y--;
							this.FindNearestModule();
						}
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
						if (this.SelectedFace.Y < this.SelectedFace.Slots.GetUpperBound(0)) {
							this.SelectedFace.Y++;
							this.FindNearestModule();
						}
						break;
					case FocusStates.Module:
						if (this.SelectedFace.SelectedComponent is Module module)
							module.Y++;
						break;
				}
				break;
		}
	}

	private void FindNearestModule() {
		if (this.SelectedFace.SelectedComponent is Module) return;
		for (var d = 1; d <= 2; d++) {
			for (var dir = 0; dir < 2; dir++) {
				var x = dir == 0 ? this.SelectedFace.X - d : this.SelectedFace.X + d;
				if (x >= 0 && x < this.SelectedFace.Slots.GetLength(1) && this.SelectedFace.Slots[this.SelectedFace.Y, x] is Module) {
					this.SelectedFace.X -= d;
					return;
				}
			}
		}
	}

	public void SendInputs(string inputs) {
		foreach (var token in inputs.Split(new[] { ' ', '+', ',' }, StringSplitOptions.RemoveEmptyEntries))
			this.actionQueue.Enqueue(token);
		this.queueTimer.Start();
	}

	public string IdentifyModule(int x1, int y1) => this.GetComponent(x1, y1)?.Type ?? "nil";

	public string ReadModule(string type, int x1, int y1) {
		var component = this.GetComponent(x1, y1);
		if (component is null)
			throw new ArgumentException("Attempt to read blank component");
		if (!component.Type.Equals(type, StringComparison.InvariantCultureIgnoreCase))
			throw new ArgumentException("Wrong type for specified component.");
		return component.Details;
	}

	public string GetLightState(int x1, int y1) => (this.GetComponent(x1, y1) is Module module ? module.LightState : ModuleLightState.Off).ToString();

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

	public string IdentifyWidget(int x1, int y1) => this.GetWidget(x1, y1)?.Type ?? "nil";

	public string ReadWidget(string type, int x1, int y1) {
		var widget = this.GetWidget(x1, y1);
		if (widget is null)
			throw new ArgumentException("Attempt to read blank widget.");
		if (!widget.Type.Equals(type, StringComparison.InvariantCultureIgnoreCase))
			throw new ArgumentException("Wrong type for specified widget.");
		return widget.Details;
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
		internal string Type { get; }
		internal abstract string Details { get; }

		protected BombComponent(string type) => this.Type = type ?? throw new ArgumentNullException(nameof(type));
	}

	private class Module : BombComponent {
		private readonly Timer ResetLightTimer = new(2000) { AutoReset = false };

		private static int NextID;

		internal int ID { get; }
		internal override string Details { get; }
		public ModuleLightState LightState { get; private set; }

		public int X;
		public int Y;

		protected Module(string type) : this(type, "") { }
		public Module(string type, string details) : base(type) {
			++NextID;
			this.ID = NextID;
			this.Details = details ?? throw new ArgumentNullException(nameof(details));
			this.ResetLightTimer.Elapsed += this.ResetLightTimer_Elapsed;
		}

		private void ResetLightTimer_Elapsed(object? sender, ElapsedEventArgs e) => this.LightState = ModuleLightState.Off;

		public void Solve() {
			Message($"{this.Type} solved.");
			this.LightState = ModuleLightState.Solved;
			this.ResetLightTimer.Stop();
		}

		public void StrikeFlash() {
			Message($"{this.Type} strike.");
			if (this.LightState != ModuleLightState.Solved) {
				this.LightState = ModuleLightState.Strike;
				this.ResetLightTimer.Stop();
				this.ResetLightTimer.Start();
			}
		}

		public virtual void Interact() {
			Message($"Selected ({this.X}, {this.Y}) in {this.Type}");
		}
		public virtual void StopInteract() { }

		public class ComplicatedWires : Module {
			internal static readonly ComplicatedWires Test1 = new(new WireFlags[] { WireFlags.None, WireFlags.None, WireFlags.Blue });
			internal static readonly ComplicatedWires Test2 = new(new WireFlags[] { WireFlags.Blue, WireFlags.Red, WireFlags.Blue | WireFlags.Light });
			internal static readonly ComplicatedWires Test3 = new(new WireFlags[] { WireFlags.Red, WireFlags.Blue | WireFlags.Star, WireFlags.Blue | WireFlags.Light });

			internal override string Details => $"{this.X + 1} XS {string.Join("XS ", from w in wires select w == 0 ? "nil " : $"{(w.HasFlag(WireFlags.Red) ? "red " : "")}{(w.HasFlag(WireFlags.Blue) ? "blue " : "")}{(w.HasFlag(WireFlags.Star) ? "star " : "")}{(w.HasFlag(WireFlags.Light) ? "light " : "")}")}";

			[Flags]
			public enum WireFlags {
				None,
				Red = 1,
				Blue = 2,
				Star = 4,
				Light = 8
			}

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

			public ComplicatedWires(WireFlags[] wires) : base("ComplicatedWires") {
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

		public class Button : Module {
			private readonly string colour;
			private readonly string label;
			private string? indicatorColour;
			private int correctDigit;
			private readonly Timer pressTimer = new(500) { AutoReset = false };

			internal override string Details => $"{colour} {label} {indicatorColour ?? "nil"}";

			public Button(string colour, string label) : base("Button") {
				this.colour = colour ?? throw new ArgumentNullException(nameof(colour));
				this.label = label ?? throw new ArgumentNullException(nameof(label));
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
				this.indicatorColour = "blue";
				this.correctDigit = 4;
				Message($"Button held - indicator is {this.indicatorColour}");
			}
		}
	}

	private class TimerComponent : BombComponent {
		public TimeSpan Elapsed => this.stopwatch.Elapsed;

		internal static TimerComponent Instance { get; } = new();

		private readonly Stopwatch stopwatch = Stopwatch.StartNew();
		
		internal override string Details {
			get {
				var elapsed = this.stopwatch.ElapsedTicks;
				var seconds = elapsed / Stopwatch.Frequency;
				return $"Zen {seconds} {(seconds < 60 ? elapsed / (Stopwatch.Frequency / 100) % 100 : 0)} 0";
			}
		}

		private TimerComponent() : base("Timer") { }
	}

	private class Widget {
		internal string Type { get; }
		internal string Details { get; }

		public Widget(string type, string details) {
			this.Type = type ?? throw new ArgumentNullException(nameof(type));
			this.Details = details ?? throw new ArgumentNullException(nameof(details));
		}
	}
}
