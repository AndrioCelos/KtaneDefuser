using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Timers;
using KtaneDefuserConnector.DataTypes;
using Microsoft.Extensions.Logging;
using WireFlags = KtaneDefuserConnector.Components.ComplicatedWires.WireFlags;

namespace KtaneDefuserConnector;
internal partial class Simulation {
	private static partial class Modules {
		#region Vanilla modules
		public class Wires(Simulation simulation, int shouldCut, params Components.Wires.Colour[] wires) : Module<Components.Wires.ReadData>(simulation, DefuserConnector.GetComponentReader<Components.Wires>(), 1, wires.Length) {
			internal override Components.Wires.ReadData Details => new(wires);

			private readonly bool[] isCut = new bool[wires.Length];

			public override void Interact() {
				LogCutWire(Y + 1);
				if (isCut[Y])
					return;
				isCut[Y] = true;
				if (Y == shouldCut)
					Solve();
				else
					StrikeFlash();
			}
		}

		public class ComplicatedWires(Simulation simulation, WireFlags[] wires) : Module<Components.ComplicatedWires.ReadData>(simulation, DefuserConnector.GetComponentReader<Components.ComplicatedWires>(), wires.Length, 1) {
			internal override Components.ComplicatedWires.ReadData Details => new(X, wires);

			private static readonly bool[] shouldCut = new bool[16];
			private readonly bool[] isCut = new bool[wires.Length];

			static ComplicatedWires() {
				shouldCut[(int) WireFlags.None] = true;
				shouldCut[(int) WireFlags.Blue] = true;
				shouldCut[(int) (WireFlags.Blue | WireFlags.Star)] = true;
				shouldCut[(int) (WireFlags.Blue | WireFlags.Light)] = true;
				shouldCut[(int) (WireFlags.Blue | WireFlags.Star | WireFlags.Light)] = true;
			}

			public override void Interact() {
				LogCutWire(X + 1);
				if (isCut[X])
					return;
				isCut[X] = true;
				if (!shouldCut[(int) wires[X]])
					StrikeFlash();
				else if (!wires.Where((t, i) => shouldCut[(int) t] && !isCut[i]).Any())
					Solve();
			}
		}

		public partial class Button : Module<Components.Button.ReadData> {
			private readonly Components.Button.Colour colour;
			private readonly string label;
			private Components.Button.Colour? indicatorColour;
			private int correctDigit;
			private readonly Timer pressTimer = new(500) { AutoReset = false };

			internal override Components.Button.ReadData Details => new(colour, label, indicatorColour);

			public Button(Simulation simulation, Components.Button.Colour colour, string label) : base(simulation, DefuserConnector.GetComponentReader<Components.Button>(), 1, 1) {
				this.colour = colour;
				this.label = label;
				pressTimer.Elapsed += PressTimer_Elapsed;
			}

			public override void Interact() => pressTimer.Start();
			public override void StopInteract() {
				bool correct;
				pressTimer.Stop();
				if (indicatorColour is not null) {
					var elapsed = TimerComponent.Instance.Elapsed;
					var time = elapsed.Ticks;
					LogButtonReleased(elapsed.TotalSeconds);
					correct = time >= Stopwatch.Frequency * 60
						? time / (Stopwatch.Frequency * 600) % 10 == correctDigit
							|| time / (Stopwatch.Frequency * 60) % 10 == correctDigit
							|| time / (Stopwatch.Frequency * 10) % 10 == correctDigit
							|| time / Stopwatch.Frequency % 10 == correctDigit
						: time / (Stopwatch.Frequency * 10) % 10 == correctDigit
							|| time / Stopwatch.Frequency % 10 == correctDigit
							|| time / (Stopwatch.Frequency / 10) % 10 == correctDigit
							|| time / (Stopwatch.Frequency / 100) % 10 == correctDigit;
					indicatorColour = null;
				} else {
					LogButtonTapped();
					correct = false;
				}
				if (correct) Solve();
				else StrikeFlash();
			}

			private void PressTimer_Elapsed(object? sender, ElapsedEventArgs e) {
				indicatorColour = Components.Button.Colour.Blue;
				correctDigit = 4;
				LogButtonHeld(indicatorColour);
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
			internal override Components.Keypad.ReadData Details => new(symbols);

			private readonly Components.Keypad.Symbol[] symbols = symbols;
			private readonly int[] correctOrder = correctOrder;
			private readonly bool[] isPressed = new bool[symbols.Length];

			public override void Interact() {
				if (X is < 0 or >= 2 || Y is < 0 or >= 2) throw new InvalidOperationException("Invalid highlight position");
				var index = Y * 2 + X;
				LogKeyPressed(index);
				if (isPressed[index])
					return;
				foreach (var i in correctOrder) {
					if (i == index) break;
					if (!isPressed[i]) {
						StrikeFlash();
						return;
					}
				}
				isPressed[index] = true;
				if (!isPressed.Contains(false))
					Solve();
			}

			#region Log templates

			[LoggerMessage(LogLevel.Information, "Pressed key {Index}.")]
			private partial void LogKeyPressed(int index);

			#endregion
		}

		public partial class Maze : Module<Components.Maze.ReadData> {
			internal override Components.Maze.ReadData Details => new(position, goal, circle1, circle2);

			private GridCell position;
			private readonly GridCell goal;
			private readonly GridCell circle1;
			private readonly GridCell circle2;

			public Maze(Simulation simulation, GridCell start, GridCell goal, GridCell circle1, GridCell circle2) : base(simulation, DefuserConnector.GetComponentReader<Components.Maze>(), 3, 3) {
				position = start;
				this.goal = goal;
				this.circle1 = circle1;
				this.circle2 = circle2;
				SelectableGrid[0, 0] = false;
				SelectableGrid[0, 2] = false;
				SelectableGrid[1, 1] = false;
				SelectableGrid[2, 0] = false;
				SelectableGrid[2, 2] = false;
			}

			public override void Interact() {
				var direction = Y switch {
					0 => Direction.Up,
					2 => Direction.Down,
					_ => X == 0 ? Direction.Left : Direction.Right
				};
				var newPosition = position;
				switch (direction) {
					case Direction.Up: newPosition.Y--; break;
					case Direction.Right: newPosition.X++; break;
					case Direction.Down: newPosition.Y++; break;
					case Direction.Left: newPosition.X--; break;
				}
				if (newPosition.X is < 0 or >= 6  || newPosition.Y is < 0 or >= 6) {
					LogMovedIntoBoundary(direction);
					StrikeFlash();
				} else {
					position = newPosition;
					LogMoved(direction, newPosition);
					if (position == goal)
						Solve();
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
			internal override Components.Memory.ReadData Details => !isAnimating ? new(stagesCleared, display, keyDigits) : throw new InvalidOperationException("Tried to read module while animating");

			private int display;
			private readonly int[] keyDigits = new int[4];
			private int stagesCleared;
			private bool isAnimating;
			private readonly Timer animationTimer = new(2900) { AutoReset = false };

			public Memory(Simulation simulation) : base(simulation, DefuserConnector.GetComponentReader<Components.Memory>(), 4, 1) {
				animationTimer.Elapsed += AnimationTimer_Elapsed;
				SetKeysAndDisplay();
			}

			private void AnimationTimer_Elapsed(object? sender, ElapsedEventArgs e) {
				SetKeysAndDisplay();
				isAnimating = false;
			}

			private void SetKeysAndDisplay() {
				for (var i = 0; i < 4; i++)
					keyDigits[i] = (i + stagesCleared) % 4 + 1;
				display = keyDigits[0];
			}

			private void StartAnimation() {
				isAnimating = true;
				animationTimer.Start();
			}

			public override void Interact() {
				if (isAnimating) throw new InvalidOperationException("Memory button pressed during animation");
				LogKeyPressed(X + 1);
				if (LightState == ModuleLightState.Solved) return;
				if (X != stagesCleared % 4) {
					StrikeFlash();
					stagesCleared = 0;
					StartAnimation();
				} else {
					stagesCleared++;
					if (stagesCleared >= 5)
						Solve();
					else
						StartAnimation();
				}
			}

			#region Log templates

			[LoggerMessage(LogLevel.Information, "Pressed key {Index}.")]
			private partial void LogKeyPressed(int index);

			#endregion
		}

		public class MorseCode : Module<Components.MorseCode.ReadData> {
			internal override Components.MorseCode.ReadData Details => new(lightOn);

			private bool lightOn;
			private const long Pattern = 0b10101000101010111000111011100011101110111000101010111; // bombs
			private const int PatternLength = 53;
			private int index;
			private readonly Timer animationTimer = new(250);
			private int selectedFrequency;
			private static readonly string[] allFrequencies = ["505", "515", "522", "532", "535", "542", "545", "552", "555", "565", "572", "575", "582", "592", "595", "600"];

			public MorseCode(Simulation simulation) : base(simulation, DefuserConnector.GetComponentReader<Components.MorseCode>(), 2, 2) {
				SelectableGrid[1, 1] = false;
				animationTimer.Elapsed += AnimationTimer_Elapsed;
				animationTimer.Start();
			}

			private void AnimationTimer_Elapsed(object? sender, ElapsedEventArgs e) {
				index++;
				if (index >= PatternLength) index = -10;
				lightOn = index >= 0 && (Pattern & 1L << index) != 0;
			}

			public override void Interact() {
				if (Y == 1) {
					LogSubmit($"3.{allFrequencies[selectedFrequency]}");
					if (selectedFrequency == 9) {
						Solve();
						animationTimer.Stop();
						lightOn = false;
					} else
						StrikeFlash();
				} else if (X == 0) {
					if (selectedFrequency == 0) throw new InvalidOperationException("Pointer went out of bounds.");
					selectedFrequency--;
				} else {
					if (selectedFrequency == 15) throw new InvalidOperationException("Pointer went out of bounds.");
					selectedFrequency++;
				}
			}
		}

		public partial class NeedyCapacitor(Simulation simulation) : NeedyModule<Components.NeedyCapacitor.ReadData>(simulation, DefuserConnector.GetComponentReader<Components.NeedyCapacitor>(), 1, 1) {
			private readonly Stopwatch pressStopwatch = new();

			protected override bool AutoReset => false;
			internal override Components.NeedyCapacitor.ReadData Details => new(IsActive ? (int) RemainingTime.TotalSeconds : null);

			protected override void OnActivate() { }

			public override void Interact() {
				LogPressed(RemainingTime);
				Timer.Stop();
				pressStopwatch.Restart();
			}

			public override void StopInteract() {
				AddTime(pressStopwatch.Elapsed * 6, TimeSpan.FromSeconds(45));
				Timer.Start();
				LogReleased(RemainingTime);
			}

			#region Log templates

			[LoggerMessage(LogLevel.Information, "Pressed with {RemainingTime} left.")]
			private partial void LogPressed(TimeSpan remainingTime);

			[LoggerMessage(LogLevel.Information, "Released with {RemainingTime} left.")]
			private partial void LogReleased(TimeSpan remainingTime);

			#endregion
		}

		public partial class NeedyKnob(Simulation simulation) : NeedyModule<Components.NeedyKnob.ReadData>(simulation, DefuserConnector.GetComponentReader<Components.NeedyKnob>(), 1, 1) {
			private static readonly bool[] inactiveLights = new bool[12];
			private bool[] lights = inactiveLights;
			private int position;
			private int correctPosition;
			private int nextStateIndex;

			private static readonly (bool[] lights, int correctPosition)[] states = [
				([false, false, true, false, true, true, true, true, true, true, false, true], 0),
				([false, true, true, false, false, true, true, true, true, true, false, true], 2)
			];

			internal override Components.NeedyKnob.ReadData Details => new(DisplayedTime, lights);

			protected override void OnActivate() {
				var state = states[nextStateIndex];
				nextStateIndex = (nextStateIndex + 1) % states.Length;
				lights = state.lights;
				correctPosition = state.correctPosition;
			}

			public override void Interact() {
				position = (position + 1) % 4;
				LogMoved(position);
			}

			protected override void OnTimerExpired() {
				if (position != correctPosition)
					StrikeFlash();
				lights = inactiveLights;
			}

			#region Log templates

			[LoggerMessage(LogLevel.Information, "Moved to position {Position}.")]
			private partial void LogMoved(int position);

			#endregion
		}

		public partial class NeedyVentGas(Simulation simulation) : NeedyModule<Components.NeedyVentGas.ReadData>(simulation, DefuserConnector.GetComponentReader<Components.NeedyVentGas>(), 2, 1) {
			private static readonly string[] messages = ["VENT GAS?", "DETONATE?"];
			private int messageIndex = 1;

			internal override Components.NeedyVentGas.ReadData Details => new(DisplayedTime, DisplayedTime is not null ? messages[messageIndex] : null);

			protected override void OnActivate() {
				messageIndex ^= 1;
				LogDisplay(messages[messageIndex]);
			}

			public override void Interact() {
				LogKeyPressed(X == 0 ? 'Y' : 'N');
				if (X == 0) {
					if (messageIndex != 0) StrikeFlash();
					Deactivate();
				} else {
					if (messageIndex != 0) Deactivate();
				}
			}

			protected override void OnTimerExpired() => StrikeFlash();

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

			internal override Components.Password.ReadData Details => new([.. columnPositions.Select((y, x) => columns[x, y])]);

			public Password(Simulation simulation) : base(simulation, DefuserConnector.GetComponentReader<Components.Password>(), 5, 3) {
				SelectableGrid[2, 0] = false;
				SelectableGrid[2, 1] = false;
				SelectableGrid[2, 3] = false;
				SelectableGrid[2, 4] = false;
			}

			public override void Interact() {
				if (Y == 2) {
					LogSubmit(new string(Details.Display));
					if (columnPositions[0] == 0 && columnPositions[1] == 1 && columnPositions[2] == 3 && columnPositions[3] == 3 && columnPositions[4] == 5)
						Solve();
					else
						StrikeFlash();
				} else {
					ref var columnPosition = ref columnPositions[X];
					if (Y == 0) {
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
			internal override Components.SimonSays.ReadData Details => new(litColour);

			private readonly Timer timer = new(500);
			private SimonColour? litColour;
			private int stagesCleared;
			private int inputProgress;
			private int tick;

			public SimonSays(Simulation simulation) : base(simulation, DefuserConnector.GetComponentReader<Components.SimonSays>(), 3, 3) {
				SelectableGrid[0, 0] = false;
				SelectableGrid[0, 2] = false;
				SelectableGrid[1, 1] = false;
				SelectableGrid[2, 0] = false;
				SelectableGrid[2, 2] = false;
				timer.Elapsed += Timer_Elapsed;
				timer.Start();
			}

			private void Timer_Elapsed(object? sender, ElapsedEventArgs e) {
				if (tick >= 0) {
					inputProgress = 0;
					litColour = tick % 3 == 2 ? null : (SimonColour) (tick / 3);
				} else if (tick is -18 or -4)
					litColour = null;
				tick++;
				if (tick >= (stagesCleared + 1) * 3)
					tick = -20;
			}

			public override void Interact() {
				var pressedColour = Y switch {
					0 => SimonColour.Blue,
					2 => SimonColour.Green,
					_ => X == 0 ? SimonColour.Red : SimonColour.Yellow
				};
				litColour = pressedColour;
				LogButtonPressed(pressedColour);
				if (LightState == ModuleLightState.Solved) return;
				if (pressedColour != (SimonColour) inputProgress) {
					StrikeFlash();
					inputProgress = 0;
				} else if (inputProgress >= stagesCleared) {
					stagesCleared++;
					inputProgress = 0;
					tick = -6;
					if (stagesCleared >= 4) {
						Solve();
						timer.Stop();
					}
				} else {
					inputProgress++;
					tick = -20;
				}
			}

			#region Log templates

			[LoggerMessage(LogLevel.Information, "{Colour} was pressed.")]
			private partial void LogButtonPressed(SimonColour colour);

			#endregion
		}

		public partial class WhosOnFirst : Module<Components.WhosOnFirst.ReadData> {
			internal override Components.WhosOnFirst.ReadData Details => !isAnimating ? new(stagesCleared, display, keys) : throw new InvalidOperationException("Tried to read module while animating");

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
				animationTimer.Elapsed += AnimationTimer_Elapsed;
				SetKeysAndDisplay();
			}

			private void AnimationTimer_Elapsed(object? sender, ElapsedEventArgs e) {
				SetKeysAndDisplay();
				isAnimating = false;
			}

			[MemberNotNull(nameof(display))]
			private void SetKeysAndDisplay() {
				for (var i = 0; i < 6; i++)
					keys[i] = keyStrings[i + stagesCleared * 8];
				display = displayStrings[stagesCleared];
			}

			private void StartAnimation() {
				isAnimating = true;
				animationTimer.Start();
			}

			public override void Interact() {
				if (isAnimating) throw new InvalidOperationException("Memory button pressed during animation");
				LogButtonPressed(X + Y * 2 + 1);
				if (LightState == ModuleLightState.Solved) return;
				if (X + Y * 2 != stagesCleared) {
					StrikeFlash();
					StartAnimation();
				} else {
					stagesCleared++;
					if (stagesCleared >= 3)
						Solve();
					else
						StartAnimation();
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
					readFailSimulation--;
					if (readFailSimulation < 0) readFailSimulation = 3;
					return !isAnimating
						? new(stagesCleared, 1 + currentPage * 3, wires.Skip(stagesCleared * 3).Take(3).Select(w => w?.Colour).ToArray(), Y switch { 0 => -1, 4 => 1, _ => 0 },
							readFailSimulation == 0 && Y is >= 1 and <= 3 && wires[Y - 1 + currentPage * 3] is { } wire ? new(Y - 1, wire.To) : null)
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
				animationTimer.Elapsed += AnimationTimer_Elapsed;
				for (var i = 0; i < 12; i++) {
					if (i % 4 != 0) {
						var colour = (Components.WireSequence.WireColour) (i / 2 % 3);
						wires[i] = new(colour, (char) ('A' + i % 3));
						shouldCut[i] = colour == Components.WireSequence.WireColour.Blue;
					}
				}
				UpdateSelectable();
			}

			private void UpdateSelectable() {
				for (var y = 0; y < 3; y++)
					SelectableGrid[y + 1, 0] = currentPage < 4 && wires[y + currentPage * 3] is not null;
			}

			private void AnimationTimer_Elapsed(object? sender, ElapsedEventArgs e) {
				UpdateSelectable();
				isAnimating = false;
			}

			private void StartAnimation() {
				isAnimating = true;
				animationTimer.Start();
			}

			public override void Interact() {
				if (LightState == ModuleLightState.Solved) return;
				if (isAnimating) throw new InvalidOperationException("Wire Sequence interacted during animation");
				switch (Y) {
					case 0:
						if (currentPage == 0) throw new InvalidOperationException("Tried to move up from the first page");
						currentPage--;
						LogPageChanged(currentPage + 1);
						StartAnimation();
						break;
					case 4:
						if (currentPage == stagesCleared) {
							if (Enumerable.Range(currentPage * 3, 3).Any(i => shouldCut[i] && !isCut[i]))
								StrikeFlash();
							else {
								currentPage++;
								stagesCleared++;
								if (currentPage == 4)
									Solve();
								LogPageChanged(currentPage + 1);
								StartAnimation();
							}
						} else {
							currentPage++;
							LogPageChanged(currentPage + 1);
							StartAnimation();
						}
						break;
					default:
						if (isCut[currentPage * 3 + Y - 1]) return;
						LogCutWire(currentPage * 3 + Y);
						isCut[currentPage * 3 + Y - 1] = true;
						if (!shouldCut[currentPage * 3 + Y - 1])
							StrikeFlash();
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
			internal override Components.ColourFlash.ReadData Details => index < 0 ? new(null, Components.ColourFlash.Colour.None) : sequence[index];

			private int index;
			private readonly Components.ColourFlash.ReadData[] sequence;
			private readonly Timer animationTimer = new(750);

			public ColourFlash(Simulation simulation) : base(simulation, DefuserConnector.GetComponentReader<Components.ColourFlash>(), 2, 1) {
				sequence = [ new("RED", Components.ColourFlash.Colour.Yellow), new("YELLOW", Components.ColourFlash.Colour.Green), new("GREEN", Components.ColourFlash.Colour.Blue),
					new("BLUE", Components.ColourFlash.Colour.Magenta), new("MAGENTA", Components.ColourFlash.Colour.White), new("WHITE", Components.ColourFlash.Colour.Blue),
					new("RED", Components.ColourFlash.Colour.Red), new("BLUE", Components.ColourFlash.Colour.Blue) ];
				animationTimer.Elapsed += AnimationTimer_Elapsed;
				animationTimer.Start();
			}

			private void AnimationTimer_Elapsed(object? sender, ElapsedEventArgs e) {
				index++;
				if (index >= sequence.Length) index = -4;
			}

			public override void Interact() {
				switch (X) {
					case 0:
						LogYes(index);
						if (index == 0) {
							Solve();
							animationTimer.Stop();
							index = -1;
						} else
							StrikeFlash();
						break;
					case 1:
						LogNo(index);
						StrikeFlash();
						break;
				}
			}

			#region Log templates

			[LoggerMessage(LogLevel.Information, "Yes was pressed at {Index}.")]
			private partial void LogNo(int index);

			[LoggerMessage(LogLevel.Information, "Yes was pressed at {Index}.")]
			private partial void LogYes(int index);

			#endregion
		}

		public partial class PianoKeys(Simulation simulation) : Module<Components.PianoKeys.ReadData>(simulation, DefuserConnector.GetComponentReader<Components.PianoKeys>(), 12, 1) {
			internal override Components.PianoKeys.ReadData Details => new([Components.PianoKeys.Symbol.CutCommonTime, Components.PianoKeys.Symbol.Natural, Components.PianoKeys.Symbol.Fermata]);

			private static readonly string[] labels = ["C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"];
			private readonly int[] correctSequence = [4, 6, 6, 6, 6, 4, 4, 4];
			private int index;

			public override void Interact() {
				LogKeyPressed(labels[X]);
				if (index >= correctSequence.Length) return;
				if (X == correctSequence[index]) {
					index++;
					if (index >= correctSequence.Length)
						Solve();
				} else {
					StrikeFlash();
					index = 0;
				}
			}

			#region Log templates

			[LoggerMessage(LogLevel.Information, "{Note} was played.")]
			private partial void LogKeyPressed(string note);

			#endregion
		}

		public class Semaphore(Simulation simulation) : Module<Components.Semaphore.ReadData>(simulation,
			DefuserConnector.GetComponentReader<Components.Semaphore>(), 3, 1) {
			internal override Components.Semaphore.ReadData Details => signals[index];

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
			private const int CorrectIndex = 5;

			public override void Interact() {
				switch (X) {
					case 0:
						if (index > 0) index--;
						break;
					case 2:
						if (index < signals.Length - 1) index++;
						break;
					case 1:
						LogSubmit(Details);
						if (index == CorrectIndex)
							Solve();
						else
							StrikeFlash();
						break;
				}
			}
		}

		public partial class NeedyMath(Simulation simulation) : NeedyModule<Components.NeedyMath.ReadData>(simulation, DefuserConnector.GetComponentReader<Components.NeedyMath>(), 4, 3) {
			internal override Components.NeedyMath.ReadData Details => new(IsActive ? (int) RemainingTime.TotalSeconds : null, display, new(X, Y));

			private string display = "";
			private int answer;
			private int input;
			private bool minus;

			protected override void OnActivate() {
				var random = new Random();
				int a = random.Next(100), b = random.Next(100);
				var subtract = random.Next(2) != 0;
				answer = subtract ? a - b : a + b;
				input = 0;
				minus = false;
				display = $"{a}{(subtract ? '-' : '+')}{b}";
			}

			public override void Interact() {
				if (X == 3) {
					switch (Y) {
						case 0:
							LogButton('0');
							input *= 10;
							return;
						case 1:
							LogButton('-');
							minus = !minus;
							return;
						default:
							if (!IsActive) return;
							if (minus) input = -input;
							LogSubmit(input);
							if (input != answer) StrikeFlash();
							Deactivate();
							display = "";
							return;
					}
				}

				var n = X + 1 + Y * 3;
				LogButton((char) ('0' + n));
				input = input * 10 + n;
			}

			#region Log templates

			[LoggerMessage(LogLevel.Information, "{Button} was pressed.")]
			private partial void LogButton(char button);

			#endregion
		}

		public partial class EmojiMath : Module<Components.EmojiMath.ReadData> {
			internal override Components.EmojiMath.ReadData Details => new(display, new(X, Y));

			private readonly string display;
			private readonly int answer;
			private int input;
			private bool minus;

			public EmojiMath(Simulation simulation) : base(simulation, DefuserConnector.GetComponentReader<Components.EmojiMath>(), 4, 3) {
				var random = new Random();
				int a = random.Next(100), b = random.Next(100);
				var subtract = random.Next(2) != 0;
				answer = subtract ? a - b : a + b;
				input = 0;
				minus = false;
				display = string.Join(null, from c in $"{a}{(subtract ? '-' : '+')}{b}" select c switch { '0' => ":)", '1' => "=(", '2' => "(:", '3' => ")=", '4' => ":(", '5' => "):", '6' => "=)", '7' => "(=", '8' => ":|", '9' => "|:", _ => c.ToString() });
			}

			public override void Interact() {
				if (X == 3) {
					switch (Y) {
						case 0:
							LogButton('0');
							input *= 10;
							return;
						case 1:
							LogButton('-');
							minus = !minus;
							return;
						default:
							if (minus) input = -input;
							LogSubmit(input);
							if (input != answer) StrikeFlash();
							Solve();
							return;
					}
				}

				var n = X + 1 + Y * 3;
				LogButton((char) ('0' + n));
				input = input * 10 + n;
			}

			#region Log templates

			[LoggerMessage(LogLevel.Information, "{Button} was pressed.")]
			private partial void LogButton(char button);

			#endregion
		}
		#endregion
	}
}
