using BombDefuserConnector.DataTypes;
using System;
using System.Diagnostics;
using System.Linq;
using System.Timers;
using static BombDefuserConnector.Components.ComplicatedWires;

namespace BombDefuserConnector;
internal partial class Simulation {
	private static class Modules {
		public class Wires : Module<Components.Wires.ReadData> {
			internal override Components.Wires.ReadData Details => new(this.wires);

			private readonly Components.Wires.Colour[] wires;
			private bool[] isCut;
			private int shouldCut;

			public Wires(int shouldCut, params Components.Wires.Colour[] wires) : base(BombDefuserAimlService.GetComponentProcessor<Components.Wires>(), 1, wires.Length) {
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

			public ComplicatedWires(WireFlags[] wires) : base(BombDefuserAimlService.GetComponentProcessor<Components.ComplicatedWires>(), wires.Length, 1) {
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

			public Button(Components.Button.Colour colour, Components.Button.Label label) : base(BombDefuserAimlService.GetComponentProcessor<Components.Button>(), 1, 1) {
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

			public Keypad(Components.Keypad.Symbol[] symbols, int[] correctOrder) : base(BombDefuserAimlService.GetComponentProcessor<Components.Keypad>(), 2, 2) {
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

		public class Maze : Module<Components.Maze.ReadData> {
			internal override Components.Maze.ReadData Details => new(this.position, this.goal, this.circle1, this.circle2);

			private GridCell position;
			private readonly GridCell goal;
			private readonly GridCell circle1;
			private readonly GridCell circle2;

			public Maze(GridCell start, GridCell goal, GridCell circle1, GridCell circle2) : base(BombDefuserAimlService.GetComponentProcessor<Components.Maze>(), 3, 3) {
				this.position = start;
				this.goal = goal;
				this.circle1 = circle1;
				this.circle2 = circle2;
				this.SelectableGrid[0, 0] = false;
				this.SelectableGrid[0, 2] = false;
				this.SelectableGrid[1, 1] = false;
				this.SelectableGrid[2, 0] = false;
				this.SelectableGrid[2, 2] = false;
			}

			public override void Interact() {
				var direction = this.Y switch {
					0 => Direction.Up,
					2 => Direction.Down,
					_ => this.X == 0 ? Direction.Left : Direction.Right
				};
				var newPosition = position;
				switch (direction) {
					case Direction.Up: newPosition.Y--; break;
					case Direction.Right: newPosition.X++; break;
					case Direction.Down: newPosition.Y++; break;
					case Direction.Left: newPosition.X--; break;
				}
				if (newPosition.X is < 0 or >= 6  || newPosition.Y is < 0 or >= 6) {
					Message($"{direction} pressed; hit the boundary.");
					this.StrikeFlash();
				} else {
					this.position = newPosition;
					Message($"{direction} pressed; moved to {this.position}.");
					if (this.position == this.goal)
						this.Solve();
				}
			}
		}

		public class Memory : Module<Components.Memory.ReadData> {
			internal override Components.Memory.ReadData Details => !this.isAnimating ? new(this.stagesCleared, this.display, this.keyDigits) : throw new InvalidOperationException("Tried to read module while animating");

			private int display;
			private readonly int[] keyDigits = new int[4];
			private int stagesCleared;
			private bool isAnimating;
			private readonly Timer animationTimer = new(2900) { AutoReset = false };

			public Memory() : base(BombDefuserAimlService.GetComponentProcessor<Components.Memory>(), 4, 1) {
				this.animationTimer.Elapsed += this.AnimationTimer_Elapsed;
				this.SetKeysAndDisplay();
			}

			private void AnimationTimer_Elapsed(object? sender, ElapsedEventArgs e) {
				this.SetKeysAndDisplay();
				this.isAnimating = false;
			}

			private void SetKeysAndDisplay() {
				for (var i = 0; i < 4; i++)
					this.keyDigits[i] = (i + stagesCleared) % 4 + 1;
				this.display = this.keyDigits[0];
			}

			private void StartAnimation() {
				this.isAnimating = true;
				this.animationTimer.Start();
			}

			public override void Interact() {
				if (this.isAnimating) throw new InvalidOperationException("Memory button pressed during animation");
				Message($"Pressed button {this.X + 1}");
				if (this.LightState == ModuleLightState.Solved) return;
				if (this.X != this.stagesCleared % 4) {
					this.StrikeFlash();
					this.stagesCleared = 0;
					this.StartAnimation();
				} else {
					this.stagesCleared++;
					if (this.stagesCleared >= 5)
						this.Solve();
					else
						this.StartAnimation();
				}
			}
		}

		public class NeedyCapacitor : NeedyModule<Components.NeedyCapacitor.ReadData> {
			private Stopwatch pressStopwatch = new();

			internal override Components.NeedyCapacitor.ReadData Details => new(this.IsActive ? (int) this.RemainingTime.TotalSeconds : null);

			public NeedyCapacitor() : base(BombDefuserAimlService.GetComponentProcessor<Components.NeedyCapacitor>(), 1, 1) { }

			protected override void OnActivate() { }

			public override void Interact() {
				Message($"{this.Processor.Name} pressed with {this.RemainingTime} left.");
				pressStopwatch.Restart();
			}

			public override void StopInteract() {
				this.AddTime(pressStopwatch.Elapsed * 6, TimeSpan.FromSeconds(45));
				Message($"{this.Processor.Name} released with {this.RemainingTime} left.");
			}
		}

		public class NeedyKnob : NeedyModule<Components.NeedyKnob.ReadData> {
			private static readonly bool[] inactiveLights = new bool[12];
			private bool[] lights = inactiveLights;
			private int position;
			private int correctPosition;
			private int nextStateIndex;

			private static readonly (bool[] lights, int correctPosition)[] states = new[] {
				(new[] { false, false, true, false, true, true, true, true, true, true, false, true }, 0),
				(new[] { false, true, true, false, false, true, true, true, true, true, false, true }, 2)
			};

			internal override Components.NeedyKnob.ReadData Details => new(this.DisplayedTime, this.lights);

			public NeedyKnob() : base(BombDefuserAimlService.GetComponentProcessor<Components.NeedyKnob>(), 1, 1) { }

			protected override void OnActivate() {
				var state = states[nextStateIndex];
				nextStateIndex = (nextStateIndex + 1) % states.Length;
				this.lights = state.lights;
				correctPosition = state.correctPosition;
			}

			public override void Interact() {
				this.position = (this.position + 1) % 4;
				Message($"Moved the knob to position {this.position}");
			}

			public override void OnTimerExpired() {
				if (this.position != this.correctPosition)
					this.StrikeFlash();
				this.lights = inactiveLights;
			}
		}

		public class NeedyVentGas : NeedyModule<Components.NeedyVentGas.ReadData> {
			private static readonly string[] messages = new[] { "VENT GAS?", "DETONATE?" };
			private int messageIndex = 1;

			internal override Components.NeedyVentGas.ReadData Details => new(this.DisplayedTime, this.DisplayedTime is not null ? messages[this.messageIndex] : null);

			public NeedyVentGas() : base(BombDefuserAimlService.GetComponentProcessor<Components.NeedyVentGas>(), 2, 1) { }

			protected override void OnActivate() {
				this.messageIndex ^= 1;
				Message($"Display: {messages[this.messageIndex]}");
			}

			public override void Interact() {
				Message($"{(this.X == 0 ? 'Y' : 'N')} was pressed.");
				if (this.X == 0) {
					if (this.messageIndex != 0) this.StrikeFlash();
					this.Deactivate();
				} else {
					if (this.messageIndex != 0) this.Deactivate();
				}
			}

			public override void OnTimerExpired() {
				this.StrikeFlash();
			}
		}
	}
}
