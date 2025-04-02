﻿using BombDefuserConnector.DataTypes;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Timers;
using Microsoft.Extensions.Logging;
using WireFlags = BombDefuserConnector.Components.ComplicatedWires.WireFlags;

namespace BombDefuserConnector;
internal partial class Simulation {
	private static partial class Modules {
#region Vanilla modules
		public class Wires(Simulation simulation, int shouldCut, params Components.Wires.Colour[] wires) : Module<Components.Wires.ReadData>(simulation, DefuserConnector.GetComponentReader<Components.Wires>(), 1, wires.Length) {
			internal override Components.Wires.ReadData Details => new(this.wires);

			private readonly Components.Wires.Colour[] wires = wires;
			private readonly bool[] isCut = new bool[wires.Length];
			private readonly int shouldCut = shouldCut;

			public override void Interact() {
				LogCutWire(this.Y + 1);
				if (this.isCut[this.Y])
					return;
				this.isCut[this.Y] = true;
				if (this.Y == this.shouldCut)
					this.Solve();
				else
					this.StrikeFlash();
			}
		}

		public class ComplicatedWires(Simulation simulation, WireFlags[] wires) : Module<Components.ComplicatedWires.ReadData>(simulation, DefuserConnector.GetComponentReader<Components.ComplicatedWires>(), wires.Length, 1) {
			internal override Components.ComplicatedWires.ReadData Details => new(this.X, this.wires);

			public static bool[] ShouldCut = new bool[16];
			private readonly WireFlags[] wires = wires;
			private readonly bool[] isCut = new bool[wires.Length];

			static ComplicatedWires() {
				ShouldCut[(int) WireFlags.None] = true;
				ShouldCut[(int) WireFlags.Blue] = true;
				ShouldCut[(int) (WireFlags.Blue | WireFlags.Star)] = true;
				ShouldCut[(int) (WireFlags.Blue | WireFlags.Light)] = true;
				ShouldCut[(int) (WireFlags.Blue | WireFlags.Star | WireFlags.Light)] = true;
			}

			public override void Interact() {
				LogCutWire(this.X + 1);
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

		public partial class Button : Module<Components.Button.ReadData> {
			private readonly Components.Button.Colour colour;
			private readonly string label;
			private Components.Button.Colour? indicatorColour;
			private int correctDigit;
			private readonly Timer pressTimer = new(500) { AutoReset = false };

			internal override Components.Button.ReadData Details => new(this.colour, this.label, this.indicatorColour);

			public Button(Simulation simulation, Components.Button.Colour colour, string label) : base(simulation, DefuserConnector.GetComponentReader<Components.Button>(), 1, 1) {
				this.colour = colour;
				this.label = label;
				this.pressTimer.Elapsed += this.PressTimer_Elapsed;
			}

			public override void Interact() => this.pressTimer.Start();
			public override void StopInteract() {
				bool correct;
				this.pressTimer.Stop();
				if (this.indicatorColour is not null) {
					var elapsed = TimerComponent.Instance.Elapsed;
					var time = elapsed.Ticks;
					LogButtonReleased(elapsed.TotalSeconds);
					correct = time >= Stopwatch.Frequency * 60
						? (time / (Stopwatch.Frequency * 600) % 10 == this.correctDigit)
							|| (time / (Stopwatch.Frequency * 60) % 10 == this.correctDigit)
							|| (time / (Stopwatch.Frequency * 10) % 10 == this.correctDigit)
							|| (time / Stopwatch.Frequency % 10 == this.correctDigit)
						: (time / (Stopwatch.Frequency * 10) % 10 == this.correctDigit)
							|| (time / Stopwatch.Frequency % 10 == this.correctDigit)
							|| (time / (Stopwatch.Frequency / 10) % 10 == this.correctDigit)
							|| (time / (Stopwatch.Frequency / 100) % 10 == this.correctDigit);
					this.indicatorColour = null;
				} else {
					LogButtonTapped();
					correct = false;
				}
				if (correct) this.Solve();
				else this.StrikeFlash();
			}

			private void PressTimer_Elapsed(object? sender, ElapsedEventArgs e) {
				this.indicatorColour = Components.Button.Colour.Blue;
				this.correctDigit = 4;
				LogButtonHeld(this.indicatorColour);
			}

			#region Log templates

			[LoggerMessage(LogLevel.Information, "Button released at {Time}.")]
			private partial void LogButtonReleased(double time);

			[LoggerMessage(LogLevel.Information, "Button tapped.")]
			private partial void LogButtonTapped();

			[LoggerMessage(LogLevel.Information, "Button held – indicator is {IndicatorColour}.")]
			private partial void LogButtonHeld(Components.Button.Colour? indicatorColour);

			#endregion
		}

		public partial class Keypad(Simulation simulation, Components.Keypad.Symbol[] symbols, int[] correctOrder) : Module<Components.Keypad.ReadData>(simulation, DefuserConnector.GetComponentReader<Components.Keypad>(), 2, 2) {
			internal override Components.Keypad.ReadData Details => new(this.symbols);

			private readonly Components.Keypad.Symbol[] symbols = symbols;
			private readonly int[] correctOrder = correctOrder;
			private readonly bool[] isPressed = new bool[symbols.Length];

			public override void Interact() {
				if (this.X is < 0 or >= 2 || this.Y is < 0 or >= 2) throw new InvalidOperationException("Invalid highlight position");
				var index = this.Y * 2 + this.X;
				LogKeyPressed(index);
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

			#region Log templates

			[LoggerMessage(LogLevel.Information, "Pressed key {Index}.")]
			private partial void LogKeyPressed(int index);

			#endregion
		}

		public partial class Maze : Module<Components.Maze.ReadData> {
			internal override Components.Maze.ReadData Details => new(this.position, this.goal, this.circle1, this.circle2);

			private GridCell position;
			private readonly GridCell goal;
			private readonly GridCell circle1;
			private readonly GridCell circle2;

			public Maze(Simulation simulation, GridCell start, GridCell goal, GridCell circle1, GridCell circle2) : base(simulation, DefuserConnector.GetComponentReader<Components.Maze>(), 3, 3) {
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
				var newPosition = this.position;
				switch (direction) {
					case Direction.Up: newPosition.Y--; break;
					case Direction.Right: newPosition.X++; break;
					case Direction.Down: newPosition.Y++; break;
					case Direction.Left: newPosition.X--; break;
				}
				if (newPosition.X is < 0 or >= 6  || newPosition.Y is < 0 or >= 6) {
					LogMovedIntoBoundary(direction);
					this.StrikeFlash();
				} else {
					this.position = newPosition;
					LogMoved(direction, newPosition);
					if (this.position == this.goal)
						this.Solve();
				}
			}

			#region Log templates

			[LoggerMessage(LogLevel.Information, "{Direction} pressed; hit the boundary.")]
			private partial void LogMovedIntoBoundary(Direction direction);

			[LoggerMessage(LogLevel.Information, "{Direction} pressed; moved to {Position}.")]
			private partial void LogMoved(Direction direction, GridCell position);

			#endregion
		}

		public partial class Memory : Module<Components.Memory.ReadData> {
			internal override Components.Memory.ReadData Details => !this.isAnimating ? new(this.stagesCleared, this.display, this.keyDigits) : throw new InvalidOperationException("Tried to read module while animating");

			private int display;
			private readonly int[] keyDigits = new int[4];
			private int stagesCleared;
			private bool isAnimating;
			private readonly Timer animationTimer = new(2900) { AutoReset = false };

			public Memory(Simulation simulation) : base(simulation, DefuserConnector.GetComponentReader<Components.Memory>(), 4, 1) {
				this.animationTimer.Elapsed += this.AnimationTimer_Elapsed;
				this.SetKeysAndDisplay();
			}

			private void AnimationTimer_Elapsed(object? sender, ElapsedEventArgs e) {
				this.SetKeysAndDisplay();
				this.isAnimating = false;
			}

			private void SetKeysAndDisplay() {
				for (var i = 0; i < 4; i++)
					this.keyDigits[i] = (i + this.stagesCleared) % 4 + 1;
				this.display = this.keyDigits[0];
			}

			private void StartAnimation() {
				this.isAnimating = true;
				this.animationTimer.Start();
			}

			public override void Interact() {
				if (this.isAnimating) throw new InvalidOperationException("Memory button pressed during animation");
				LogKeyPressed(this.X + 1);
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

			#region Log templates

			[LoggerMessage(LogLevel.Information, "Pressed key {Index}.")]
			private partial void LogKeyPressed(int index);

			#endregion
		}

		public partial class MorseCode : Module<Components.MorseCode.ReadData> {
			internal override Components.MorseCode.ReadData Details => new(this.lightOn);

			private bool lightOn;
			private readonly long pattern = 0b10101000101010111000111011100011101110111000101010111;  // bombs
			private readonly int patternLength = 53;
			private int index;
			private readonly Timer animationTimer = new(250);
			private int selectedFrequency;
			private static readonly string[] allFrequencies = ["505", "515", "522", "532", "535", "542", "545", "552", "555", "565", "572", "575", "582", "592", "595", "600"];

			public MorseCode(Simulation simulation) : base(simulation, DefuserConnector.GetComponentReader<Components.MorseCode>(), 2, 2) {
				this.SelectableGrid[1, 1] = false;
				this.animationTimer.Elapsed += this.AnimationTimer_Elapsed;
				this.animationTimer.Start();
			}

			private void AnimationTimer_Elapsed(object? sender, ElapsedEventArgs e) {
				this.index++;
				if (this.index >= this.patternLength) this.index = -10;
				this.lightOn = this.index >= 0 && (this.pattern & 1L << this.index) != 0;
			}

			public override void Interact() {
				if (this.Y == 1) {
					LogSubmit($"3.{allFrequencies[this.selectedFrequency]}");
					if (this.selectedFrequency == 9) {
						this.Solve();
						this.animationTimer.Stop();
						this.lightOn = false;
					} else
						this.StrikeFlash();
				} else if (this.X == 0) {
					if (this.selectedFrequency == 0) throw new InvalidOperationException("Pointer went out of bounds.");
					this.selectedFrequency--;
				} else {
					if (this.selectedFrequency == 15) throw new InvalidOperationException("Pointer went out of bounds.");
					this.selectedFrequency++;
				}
			}
		}

		public partial class NeedyCapacitor : NeedyModule<Components.NeedyCapacitor.ReadData> {
			private readonly Stopwatch pressStopwatch = new();

			public override bool AutoReset => false;
			internal override Components.NeedyCapacitor.ReadData Details => new(this.IsActive ? (int) this.RemainingTime.TotalSeconds : null);

			public NeedyCapacitor(Simulation simulation) : base(simulation, DefuserConnector.GetComponentReader<Components.NeedyCapacitor>(), 1, 1) { }

			protected override void OnActivate() { }

			public override void Interact() {
				LogPressed(this.RemainingTime);
				this.Timer.Stop();
				this.pressStopwatch.Restart();
			}

			public override void StopInteract() {
				this.AddTime(this.pressStopwatch.Elapsed * 6, TimeSpan.FromSeconds(45));
				this.Timer.Start();
				LogReleased(this.RemainingTime);
			}

			#region Log templates

			[LoggerMessage(LogLevel.Information, "Pressed with {RemainingTime} left.")]
			private partial void LogPressed(TimeSpan remainingTime);

			[LoggerMessage(LogLevel.Information, "Released with {RemainingTime} left.")]
			private partial void LogReleased(TimeSpan remainingTime);

			#endregion
		}

		public partial class NeedyKnob : NeedyModule<Components.NeedyKnob.ReadData> {
			private static readonly bool[] inactiveLights = new bool[12];
			private bool[] lights = inactiveLights;
			private int position;
			private int correctPosition;
			private int nextStateIndex;

			private static readonly (bool[] lights, int correctPosition)[] states = [
				(new[] { false, false, true, false, true, true, true, true, true, true, false, true }, 0),
				(new[] { false, true, true, false, false, true, true, true, true, true, false, true }, 2)
			];

			internal override Components.NeedyKnob.ReadData Details => new(this.DisplayedTime, this.lights);

			public NeedyKnob(Simulation simulation) : base(simulation, DefuserConnector.GetComponentReader<Components.NeedyKnob>(), 1, 1) { }

			protected override void OnActivate() {
				var state = states[this.nextStateIndex];
				this.nextStateIndex = (this.nextStateIndex + 1) % states.Length;
				this.lights = state.lights;
				this.correctPosition = state.correctPosition;
			}

			public override void Interact() {
				this.position = (this.position + 1) % 4;
				LogMoved(position);
			}

			public override void OnTimerExpired() {
				if (this.position != this.correctPosition)
					this.StrikeFlash();
				this.lights = inactiveLights;
			}

			#region Log templates

			[LoggerMessage(LogLevel.Information, "Moved to position {Position}.")]
			private partial void LogMoved(int position);

			#endregion
		}

		public partial class NeedyVentGas : NeedyModule<Components.NeedyVentGas.ReadData> {
			private static readonly string[] messages = ["VENT GAS?", "DETONATE?"];
			private int messageIndex = 1;

			internal override Components.NeedyVentGas.ReadData Details => new(this.DisplayedTime, this.DisplayedTime is not null ? messages[this.messageIndex] : null);

			public NeedyVentGas(Simulation simulation) : base(simulation, DefuserConnector.GetComponentReader<Components.NeedyVentGas>(), 2, 1) { }

			protected override void OnActivate() {
				this.messageIndex ^= 1;
				LogDisplay(messages[this.messageIndex]);
			}

			public override void Interact() {
				LogKeyPressed(this.X == 0 ? 'Y' : 'N');
				if (this.X == 0) {
					if (this.messageIndex != 0) this.StrikeFlash();
					this.Deactivate();
				} else {
					if (this.messageIndex != 0) this.Deactivate();
				}
			}

			public override void OnTimerExpired() => this.StrikeFlash();

			#region Log templates

			[LoggerMessage(LogLevel.Information, "Display: {Display}")]
			private partial void LogDisplay(string display);

			[LoggerMessage(LogLevel.Information, "{Button} was pressed.")]
			private partial void LogKeyPressed(char button);

			#endregion
		}

		public class Password : Module<Components.Password.ReadData> {
			private readonly char[,] columns = new[,] {
				{ 'A', 'B', 'C', 'D', 'E', 'F' },
				{ 'G', 'B', 'H', 'I', 'J', 'K' },
				{ 'L', 'M', 'N', 'O', 'P', 'Q' },
				{ 'R', 'S', 'T', 'U', 'V', 'W' },
				{ 'W', 'X', 'Y', 'Z', 'A', 'T' }
			};
			private readonly int[] columnPositions = new int[5];

			internal override Components.Password.ReadData Details => new(this.columnPositions.Select((y, x) => this.columns[x, y]).ToArray());

			public Password(Simulation simulation) : base(simulation, DefuserConnector.GetComponentReader<Components.Password>(), 5, 3) {
				this.SelectableGrid[2, 0] = false;
				this.SelectableGrid[2, 1] = false;
				this.SelectableGrid[2, 3] = false;
				this.SelectableGrid[2, 4] = false;
			}

			public override void Interact() {
				if (this.Y == 2) {
					LogSubmit(new string(this.Details.Display));
					if (this.columnPositions[0] == 0 && this.columnPositions[1] == 1 && this.columnPositions[2] == 3 && this.columnPositions[3] == 3 && this.columnPositions[4] == 5)
						this.Solve();
					else
						this.StrikeFlash();
				} else {
					ref var columnPosition = ref this.columnPositions[this.X];
					if (this.Y == 0) {
						columnPosition--;
						if (columnPosition < 0) columnPosition = 5;
					} else {
						columnPosition++;
						if (columnPosition >= 6) columnPosition = 0;
					}
				}
			}
		}

		public partial class SimonSays : Module<Components.SimonSays.ReadData> {
			internal override Components.SimonSays.ReadData Details => new(this.litColour);

			private readonly Timer timer = new(500);
			private SimonColour? litColour;
			private int stagesCleared;
			private int inputProgress;
			private int tick;

			public SimonSays(Simulation simulation) : base(simulation, DefuserConnector.GetComponentReader<Components.SimonSays>(), 3, 3) {
				this.SelectableGrid[0, 0] = false;
				this.SelectableGrid[0, 2] = false;
				this.SelectableGrid[1, 1] = false;
				this.SelectableGrid[2, 0] = false;
				this.SelectableGrid[2, 2] = false;
				this.timer.Elapsed += this.Timer_Elapsed;
				this.timer.Start();
			}

			private void Timer_Elapsed(object? sender, ElapsedEventArgs e) {
				if (this.tick >= 0) {
					this.inputProgress = 0;
					this.litColour = this.tick % 3 == 2 ? null : (SimonColour) (this.tick / 3);
				} else if (this.tick is -18 or -4)
					this.litColour = null;
				this.tick++;
				if (this.tick >= (this.stagesCleared + 1) * 3)
					this.tick = -20;
			}

			public override void Interact() {
				var pressedColour = this.Y switch {
					0 => SimonColour.Blue,
					2 => SimonColour.Green,
					_ => this.X == 0 ? SimonColour.Red : SimonColour.Yellow
				};
				this.litColour = pressedColour;
				LogButtonPressed(pressedColour);
				if (this.LightState == ModuleLightState.Solved) return;
				if (pressedColour != (SimonColour) this.inputProgress) {
					this.StrikeFlash();
					this.inputProgress = 0;
				} else if (this.inputProgress >= this.stagesCleared) {
					this.stagesCleared++;
					this.inputProgress = 0;
					this.tick = -6;
					if (this.stagesCleared >= 4) {
						this.Solve();
						this.timer.Stop();
					}
				} else {
					this.inputProgress++;
					this.tick = -20;
				}
			}

			#region Log templates

			[LoggerMessage(LogLevel.Information, "{Colour} was pressed.")]
			private partial void LogButtonPressed(SimonColour colour);

			#endregion
		}

		public partial class WhosOnFirst : Module<Components.WhosOnFirst.ReadData> {
			internal override Components.WhosOnFirst.ReadData Details => !this.isAnimating ? new(this.stagesCleared, this.display, this.keys) : throw new InvalidOperationException("Tried to read module while animating");

			private string display;
			private readonly string[] keys = new string[6];
			private int stagesCleared;
			private bool isAnimating;
			private readonly Timer animationTimer = new(2900) { AutoReset = false };

			private static readonly string[] displayStrings = [ "", "YES", "FIRST", "DISPLAY", "OKAY", "SAYS", "NOTHING", "BLANK", "NO", "LED", "LEAD", "READ", "RED", "REED", "LEED",
				"HOLD ON", "YOU", "YOU ARE", "YOUR", "YOU'RE", "UR", "THERE", "THEY'RE", "THEIR", "THEY ARE", "SEE", "C", "CEE" ];
			private static readonly string[] keyStrings = [ "READY", "FIRST", "NO", "BLANK", "NOTHING", "YES", "WHAT", "UHHH", "LEFT", "RIGHT", "MIDDLE", "OKAY", "WAIT", "PRESS",
				"YOU", "YOU ARE", "YOUR", "YOU’RE", "UR", "U", "UH HUH", "UH UH", "WHAT?", "DONE", "NEXT", "HOLD", "SURE", "LIKE" ];

			public WhosOnFirst(Simulation simulation) : base(simulation, DefuserConnector.GetComponentReader<Components.WhosOnFirst>(), 2, 3) {
				this.animationTimer.Elapsed += this.AnimationTimer_Elapsed;
				this.SetKeysAndDisplay();
			}

			private void AnimationTimer_Elapsed(object? sender, ElapsedEventArgs e) {
				this.SetKeysAndDisplay();
				this.isAnimating = false;
			}

			[MemberNotNull(nameof(display))]
			private void SetKeysAndDisplay() {
				for (var i = 0; i < 6; i++)
					this.keys[i] = keyStrings[i + this.stagesCleared * 8];
				this.display = displayStrings[this.stagesCleared];
			}

			private void StartAnimation() {
				this.isAnimating = true;
				this.animationTimer.Start();
			}

			public override void Interact() {
				if (this.isAnimating) throw new InvalidOperationException("Memory button pressed during animation");
				LogButtonPressed(this.X + this.Y * 2 + 1);
				if (this.LightState == ModuleLightState.Solved) return;
				if (this.X + this.Y * 2 != this.stagesCleared) {
					this.StrikeFlash();
					this.StartAnimation();
				} else {
					this.stagesCleared++;
					if (this.stagesCleared >= 3)
						this.Solve();
					else
						this.StartAnimation();
				}
			}

			#region Log templates

			[LoggerMessage(LogLevel.Information, "Pressed button {Index}.")]
			private partial void LogButtonPressed(int index);

			#endregion
		}

		public partial class WireSequence : Module<Components.WireSequence.ReadData> {
			private record struct WireData(Components.WireSequence.WireColour Colour, char To);

			internal override Components.WireSequence.ReadData Details {
				get {
					this.readFailSimulation--;
					if (this.readFailSimulation < 0) this.readFailSimulation = 3;
					return !this.isAnimating
						? new(this.stagesCleared, 1 + this.currentPage * 3, this.wires.Skip(this.stagesCleared * 3).Take(3).Select(w => w?.Colour).ToArray(), this.Y switch { 0 => -1, 4 => 1, _ => 0 },
							this.readFailSimulation == 0 && this.Y is >= 1 and <= 3 && this.wires[this.Y - 1 + this.currentPage * 3] is WireData wire ? new(this.Y - 1, wire.To) : null)
						: throw new InvalidOperationException("Tried to read module while animating");
				}
			}

			private readonly WireData?[] wires = new WireData?[12];
			private readonly bool[] shouldCut = new bool[12];
			private readonly bool[] isCut = new bool[12];
			private int currentPage;
			private int stagesCleared;
			private bool isAnimating;
			private int readFailSimulation;
			private readonly Timer animationTimer = new(1200) { AutoReset = false };

			public WireSequence(Simulation simulation) : base(simulation, DefuserConnector.GetComponentReader<Components.WireSequence>(), 1, 5) {
				this.animationTimer.Elapsed += this.AnimationTimer_Elapsed;
				for (var i = 0; i < 12; i++) {
					if (i % 4 != 0) {
						var colour = (Components.WireSequence.WireColour) (i / 2 % 3);
						this.wires[i] = new(colour, (char) ('A' + i % 3));
						this.shouldCut[i] = colour == Components.WireSequence.WireColour.Blue;
					}
				}
				this.UpdateSelectable();
			}

			private void UpdateSelectable() {
				for (var y = 0; y < 3; y++)
					this.SelectableGrid[y + 1, 0] = this.currentPage < 4 && this.wires[y + this.currentPage * 3] is not null;
			}

			private void AnimationTimer_Elapsed(object? sender, ElapsedEventArgs e) {
				this.UpdateSelectable();
				this.isAnimating = false;
			}

			private void StartAnimation() {
				this.isAnimating = true;
				this.animationTimer.Start();
			}

			public override void Interact() {
				if (this.LightState == ModuleLightState.Solved) return;
				if (this.isAnimating) throw new InvalidOperationException("Wire Sequence interacted during animation");
				switch (this.Y) {
					case 0:
						if (this.currentPage == 0) throw new InvalidOperationException("Tried to move up from the first page");
						this.currentPage--;
						LogPageChanged(this.currentPage + 1);
						this.StartAnimation();
						break;
					case 4:
						if (this.currentPage == this.stagesCleared) {
							if (Enumerable.Range(this.currentPage * 3, 3).Any(i => this.shouldCut[i] && !this.isCut[i]))
								this.StrikeFlash();
							else {
								this.currentPage++;
								this.stagesCleared++;
								if (this.currentPage == 4)
									this.Solve();
								LogPageChanged(this.currentPage + 1);
								this.StartAnimation();
							}
						} else {
							this.currentPage++;
							LogPageChanged(this.currentPage + 1);
							this.StartAnimation();
						}
						break;
					default:
						if (this.isCut[this.currentPage * 3 + this.Y - 1]) return;
						LogCutWire(this.currentPage * 3 + this.Y);
						this.isCut[this.currentPage * 3 + this.Y - 1] = true;
						if (!this.shouldCut[this.currentPage * 3 + this.Y - 1])
							this.StrikeFlash();
						break;

				}
			}

			#region Log templates

			[LoggerMessage(LogLevel.Information, "Moving to page {Index}.")]
			private partial void LogPageChanged(int index);

			#endregion
		}

		#endregion

		#region Mod modules
		public partial class ColourFlash : Module<Components.ColourFlash.ReadData> {
			internal override Components.ColourFlash.ReadData Details => this.index < 0 ? new(null, Components.ColourFlash.Colour.None) : this.sequence[this.index];

			private int index;
			private readonly Components.ColourFlash.ReadData[] sequence;
			private readonly Timer animationTimer = new(750);

			public ColourFlash(Simulation simulation) : base(simulation, DefuserConnector.GetComponentReader<Components.ColourFlash>(), 2, 1) {
				this.sequence = [ new("RED", Components.ColourFlash.Colour.Yellow), new("YELLOW", Components.ColourFlash.Colour.Green), new("GREEN", Components.ColourFlash.Colour.Blue),
					new("BLUE", Components.ColourFlash.Colour.Magenta), new("MAGENTA", Components.ColourFlash.Colour.White), new("WHITE", Components.ColourFlash.Colour.Blue),
					new("RED", Components.ColourFlash.Colour.Red), new("BLUE", Components.ColourFlash.Colour.Blue) ];
				this.animationTimer.Elapsed += this.AnimationTimer_Elapsed;
				this.animationTimer.Start();
			}

			private void AnimationTimer_Elapsed(object? sender, ElapsedEventArgs e) {
				this.index++;
				if (this.index >= this.sequence.Length) this.index = -4;
			}

			public override void Interact() {
				if (this.X == 0) {
					LogYes(this.index);
					if (this.index == 0) {
						this.Solve();
						this.animationTimer.Stop();
						this.index = -1;
					} else
						this.StrikeFlash();
				} else if (this.X == 1) {
					LogNo(this.index);
					this.StrikeFlash();
				}
			}

			#region Log templates

			[LoggerMessage(LogLevel.Information, "Yes was pressed at {Index}.")]
			private partial void LogNo(int index);

			[LoggerMessage(LogLevel.Information, "Yes was pressed at {Index}.")]
			private partial void LogYes(int index);

			#endregion
		}

		public partial class PianoKeys : Module<Components.PianoKeys.ReadData> {
			internal override Components.PianoKeys.ReadData Details => new([Components.PianoKeys.Symbol.CutCommonTime, Components.PianoKeys.Symbol.Natural, Components.PianoKeys.Symbol.Fermata]);

			private static readonly string[] labels = ["C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"];
			private readonly int[] correctSequence = [4, 6, 6, 6, 6, 4, 4, 4];
			private int index;

			public PianoKeys(Simulation simulation) : base(simulation, DefuserConnector.GetComponentReader<Components.PianoKeys>(), 12, 1) { }

			public override void Interact() {
				LogKeyPressed(labels[this.X]);
				if (this.index >= this.correctSequence.Length) return;
				if (this.X == this.correctSequence[this.index]) {
					this.index++;
					if (this.index >= this.correctSequence.Length)
						this.Solve();
				} else {
					this.StrikeFlash();
					this.index = 0;
				}
			}

			#region Log templates

			[LoggerMessage(LogLevel.Information, "{Note} was played.")]
			private partial void LogKeyPressed(string note);

			#endregion
		}

		public class Semaphore : Module<Components.Semaphore.ReadData> {
			internal override Components.Semaphore.ReadData Details => this.signals[this.index];

			private readonly Components.Semaphore.ReadData[] signals = [
				new(Components.Semaphore.Direction.Up, Components.Semaphore.Direction.Right),
				new(Components.Semaphore.Direction.DownLeft, Components.Semaphore.Direction.Down),
				new(Components.Semaphore.Direction.Left, Components.Semaphore.Direction.Down),
				new(Components.Semaphore.Direction.Up, Components.Semaphore.Direction.UpRight),
				new(Components.Semaphore.Direction.UpLeft, Components.Semaphore.Direction.Down),
				new(Components.Semaphore.Direction.Up, Components.Semaphore.Direction.Down),
				new(Components.Semaphore.Direction.Up, Components.Semaphore.Direction.Right),
				new(Components.Semaphore.Direction.Up, Components.Semaphore.Direction.Down),
				new(Components.Semaphore.Direction.Down, Components.Semaphore.Direction.UpRight)
			];
			private int index;
			private readonly int correctIndex = 5;

			public Semaphore(Simulation simulation) : base(simulation, DefuserConnector.GetComponentReader<Components.Semaphore>(), 3, 1) { }

			public override void Interact() {
				switch (this.X) {
					case 0:
						if (this.index > 0) this.index--;
						break;
					case 2:
						if (this.index < this.signals.Length - 1) this.index++;
						break;
					case 1:
						LogSubmit(this.Details);
						if (this.index == this.correctIndex)
							this.Solve();
						else
							this.StrikeFlash();
						break;
				}
			}
		}
#endregion
	}
}
