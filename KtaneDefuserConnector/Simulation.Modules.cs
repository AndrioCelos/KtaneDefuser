using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Timers;
using KtaneDefuserConnector.DataTypes;
using Microsoft.Extensions.Logging;
using WireFlags = KtaneDefuserConnector.Components.ComplicatedWires.WireFlags;
// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable UnusedType.Local

namespace KtaneDefuserConnector;
internal partial class Simulation {
	private static partial class Modules {
		#region Vanilla modules
		public class Wires(Simulation simulation, int shouldCut, params Components.Wires.Colour[] wires) : Module<Components.Wires.ReadData>(simulation, DefuserConnector.GetComponentReader<Components.Wires>(), 1, wires.Length) {
			internal override Components.Wires.ReadData Details => new(Selection, wires);

			private readonly bool[] _isCut = new bool[wires.Length];

			public override void Interact() {
				LogCutWire(Selection.Y + 1);
				if (_isCut[Selection.Y])
					return;
				_isCut[Selection.Y] = true;
				if (Selection.Y == shouldCut)
					Solve();
				else
					StrikeFlash();
			}
		}

		public class ComplicatedWires(Simulation simulation, WireFlags[] wires) : Module<Components.ComplicatedWires.ReadData>(simulation, DefuserConnector.GetComponentReader<Components.ComplicatedWires>(), wires.Length, 1) {
			internal override Components.ComplicatedWires.ReadData Details => new(Selection, wires);

			private static readonly bool[] ShouldCut = new bool[16];
			private readonly bool[] _isCut = new bool[wires.Length];

			static ComplicatedWires() {
				ShouldCut[(int) WireFlags.None] = true;
				ShouldCut[(int) WireFlags.Blue] = true;
				ShouldCut[(int) (WireFlags.Blue | WireFlags.Star)] = true;
				ShouldCut[(int) (WireFlags.Blue | WireFlags.Light)] = true;
				ShouldCut[(int) (WireFlags.Blue | WireFlags.Star | WireFlags.Light)] = true;
			}

			public override void Interact() {
				LogCutWire(Selection.X + 1);
				if (_isCut[Selection.X])
					return;
				_isCut[Selection.X] = true;
				if (!ShouldCut[(int) wires[Selection.X]])
					StrikeFlash();
				else if (!wires.Where((t, i) => ShouldCut[(int) t] && !_isCut[i]).Any())
					Solve();
			}
		}

		public partial class Button : Module<Components.Button.ReadData> {
			private readonly Components.Button.Colour _colour;
			private readonly string _label;
			private Components.Button.Colour? _indicatorColour;
			private int _correctDigit;
			private readonly Timer _pressTimer = new(500) { AutoReset = false };

			internal override Components.Button.ReadData Details => new(_colour, _label, _indicatorColour);

			public Button(Simulation simulation, Components.Button.Colour colour, string label) : base(simulation, DefuserConnector.GetComponentReader<Components.Button>(), 1, 1) {
				_colour = colour;
				_label = label;
				_pressTimer.Elapsed += PressTimer_Elapsed;
			}

			public override void Interact() => _pressTimer.Start();
			public override void StopInteract() {
				bool correct;
				_pressTimer.Stop();
				if (_indicatorColour is not null) {
					var elapsed = TimerComponent.Instance.Elapsed;
					var time = elapsed.Ticks;
					LogButtonReleased(elapsed.TotalSeconds);
					correct = time >= Stopwatch.Frequency * 60
						? time / (Stopwatch.Frequency * 600) % 10 == _correctDigit
							|| time / (Stopwatch.Frequency * 60) % 10 == _correctDigit
							|| time / (Stopwatch.Frequency * 10) % 10 == _correctDigit
							|| time / Stopwatch.Frequency % 10 == _correctDigit
						: time / (Stopwatch.Frequency * 10) % 10 == _correctDigit
							|| time / Stopwatch.Frequency % 10 == _correctDigit
							|| time / (Stopwatch.Frequency / 10) % 10 == _correctDigit
							|| time / (Stopwatch.Frequency / 100) % 10 == _correctDigit;
					_indicatorColour = null;
				} else {
					LogButtonTapped();
					correct = false;
				}
				if (correct) Solve();
				else StrikeFlash();
			}

			private void PressTimer_Elapsed(object? sender, ElapsedEventArgs e) {
				_indicatorColour = Components.Button.Colour.Blue;
				_correctDigit = 4;
				LogButtonHeld(_indicatorColour);
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
			internal override Components.Keypad.ReadData Details => new(Selection, symbols);

			private readonly bool[] _isPressed = new bool[symbols.Length];

			public override void Interact() {
				var index = Selection.Y * 2 + Selection.X;
				LogKeyPressed(index);
				if (_isPressed[index])
					return;
				if (correctOrder.TakeWhile(i => i != index).Any(i => !_isPressed[i])) {
					StrikeFlash();
					return;
				}
				_isPressed[index] = true;
				if (!_isPressed.Contains(false))
					Solve();
			}

			#region Log templates

			[LoggerMessage(LogLevel.Information, "Pressed key {Index}.")]
			private partial void LogKeyPressed(int index);

			#endregion
		}

		public partial class Maze : Module<Components.Maze.ReadData> {
			internal override Components.Maze.ReadData Details => new(Selection, _position, _goal, _circle1, _circle2);

			private GridCell _position;
			private readonly GridCell _goal;
			private readonly GridCell _circle1;
			private readonly GridCell _circle2;

			public Maze(Simulation simulation, GridCell start, GridCell goal, GridCell circle1, GridCell circle2) : base(simulation, DefuserConnector.GetComponentReader<Components.Maze>(), 3, 3) {
				_position = start;
				_goal = goal;
				_circle1 = circle1;
				_circle2 = circle2;
				SelectableGrid[0, 0] = false;
				SelectableGrid[0, 2] = false;
				SelectableGrid[1, 1] = false;
				SelectableGrid[2, 0] = false;
				SelectableGrid[2, 2] = false;
			}

			public override void Interact() {
				var direction = Selection.Y switch {
					0 => Direction.Up,
					2 => Direction.Down,
					_ => Selection.X == 0 ? Direction.Left : Direction.Right
				};
				var newPosition = _position;
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
					_position = newPosition;
					LogMoved(direction, newPosition);
					if (_position == _goal)
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
			internal override Components.Memory.ReadData Details => !_isAnimating ? new(Selection, _stagesCleared, _display, _keyDigits) : throw new InvalidOperationException("Tried to read module while animating");

			private int _display;
			private readonly int[] _keyDigits = new int[4];
			private int _stagesCleared;
			private bool _isAnimating;
			private readonly Timer _animationTimer = new(2900) { AutoReset = false };

			public Memory(Simulation simulation) : base(simulation, DefuserConnector.GetComponentReader<Components.Memory>(), 4, 1) {
				_animationTimer.Elapsed += AnimationTimer_Elapsed;
				SetKeysAndDisplay();
			}

			private void AnimationTimer_Elapsed(object? sender, ElapsedEventArgs e) {
				SetKeysAndDisplay();
				_isAnimating = false;
			}

			private void SetKeysAndDisplay() {
				for (var i = 0; i < 4; i++)
					_keyDigits[i] = (i + _stagesCleared) % 4 + 1;
				_display = _keyDigits[0];
			}

			private void StartAnimation() {
				_isAnimating = true;
				_animationTimer.Start();
			}

			public override void Interact() {
				if (_isAnimating) throw new InvalidOperationException("Memory button pressed during animation");
				LogKeyPressed(Selection.X + 1);
				if (LightState == ModuleStatus.Solved) return;
				if (Selection.X != _stagesCleared % 4) {
					StrikeFlash();
					_stagesCleared = 0;
					StartAnimation();
				} else {
					_stagesCleared++;
					if (_stagesCleared >= 5)
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
			private const long Pattern = 0b10101000101010111000111011100011101110111000101010111; // bombs
			private const int PatternLength = 53;
			private static readonly string[] AllFrequencies = ["505", "515", "522", "532", "535", "542", "545", "552", "555", "565", "572", "575", "582", "592", "595", "600"];

			internal override Components.MorseCode.ReadData Details => new(Selection, _isLightOn);

			private bool _isLightOn;
			private int _index;
			private readonly Timer _animationTimer = new(250);
			private int _selectedFrequency;

			public MorseCode(Simulation simulation) : base(simulation, DefuserConnector.GetComponentReader<Components.MorseCode>(), 2, 2) {
				SelectableGrid[1, 1] = false;
				_animationTimer.Elapsed += AnimationTimer_Elapsed;
				_animationTimer.Start();
			}

			private void AnimationTimer_Elapsed(object? sender, ElapsedEventArgs e) {
				_index++;
				if (_index >= PatternLength) _index = -10;
				_isLightOn = _index >= 0 && (Pattern & 1L << _index) != 0;
			}

			public override void Interact() {
				if (Selection.Y == 1) {
					LogSubmit($"3.{AllFrequencies[_selectedFrequency]}");
					if (_selectedFrequency == 9) {
						Solve();
						_animationTimer.Stop();
						_isLightOn = false;
					} else
						StrikeFlash();
				} else if (Selection.X == 0) {
					if (_selectedFrequency == 0) throw new InvalidOperationException("Pointer went out of bounds.");
					_selectedFrequency--;
				} else {
					if (_selectedFrequency == 15) throw new InvalidOperationException("Pointer went out of bounds.");
					_selectedFrequency++;
				}
			}
		}

		public partial class NeedyCapacitor(Simulation simulation) : NeedyModule<Components.NeedyCapacitor.ReadData>(simulation, DefuserConnector.GetComponentReader<Components.NeedyCapacitor>(), 1, 1) {
			private readonly Stopwatch _pressStopwatch = new();

			protected override bool AutoReset => false;
			internal override Components.NeedyCapacitor.ReadData Details => new(IsActive ? (int) RemainingTime.TotalSeconds : null);

			protected override void OnActivate() { }

			public override void Interact() {
				LogPressed(RemainingTime);
				Timer.Stop();
				_pressStopwatch.Restart();
			}

			public override void StopInteract() {
				AddTime(_pressStopwatch.Elapsed * 6, TimeSpan.FromSeconds(45));
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
			private static readonly bool[] InactiveLights = new bool[12];
			private static readonly (bool[] lights, int correctPosition)[] States = [
				([false, false, true, false, true, true, true, true, true, true, false, true], 0),
				([false, true, true, false, false, true, true, true, true, true, false, true], 2)
			];

			private bool[] _lights = InactiveLights;
			private int _position;
			private int _correctPosition;
			private int _nextStateIndex;

			internal override Components.NeedyKnob.ReadData Details => new(DisplayedTime, _lights);

			protected override void OnActivate() {
				var state = States[_nextStateIndex];
				_nextStateIndex = (_nextStateIndex + 1) % States.Length;
				_lights = state.lights;
				_correctPosition = state.correctPosition;
			}

			public override void Interact() {
				_position = (_position + 1) % 4;
				LogMoved(_position);
			}

			protected override void OnTimerExpired() {
				if (_position != _correctPosition)
					StrikeFlash();
				_lights = InactiveLights;
			}

			#region Log templates

			[LoggerMessage(LogLevel.Information, "Moved to position {Position}.")]
			private partial void LogMoved(int position);

			#endregion
		}

		public partial class NeedyVentGas(Simulation simulation) : NeedyModule<Components.NeedyVentGas.ReadData>(simulation, DefuserConnector.GetComponentReader<Components.NeedyVentGas>(), 2, 1) {
			private static readonly string[] Messages = ["VENT GAS?", "DETONATE?"];
			
			private int _messageIndex = 1;

			internal override Components.NeedyVentGas.ReadData Details => new(Selection, DisplayedTime, DisplayedTime is not null ? Messages[_messageIndex] : null);

			protected override void OnActivate() {
				_messageIndex ^= 1;
				LogDisplay(Messages[_messageIndex]);
			}

			public override void Interact() {
				LogButton(Selection.X == 0 ? 'Y' : 'N');
				if (Selection.X == 0) {
					if (_messageIndex != 0) StrikeFlash();
					Deactivate();
				} else {
					if (_messageIndex != 0) Deactivate();
				}
			}

			protected override void OnTimerExpired() => StrikeFlash();

			#region Log templates

			[LoggerMessage(LogLevel.Information, "Display: {Display}")]
			private partial void LogDisplay(string display);

			#endregion
		}

		public class Password : Module<Components.Password.ReadData> {
			private readonly char[,] _columns = new[,] {
				{ 'A', 'B', 'C', 'D', 'E', 'F' },
				{ 'G', 'B', 'H', 'I', 'J', 'K' },
				{ 'L', 'M', 'N', 'O', 'P', 'Q' },
				{ 'R', 'S', 'T', 'U', 'V', 'W' },
				{ 'W', 'X', 'Y', 'Z', 'A', 'T' }
			};
			private readonly int[] _columnPositions = new int[5];

			internal override Components.Password.ReadData Details => new(Selection, [.. _columnPositions.Select((y, x) => _columns[x, y])]);

			public Password(Simulation simulation) : base(simulation, DefuserConnector.GetComponentReader<Components.Password>(), 5, 3) {
				SelectableGrid[2, 0] = false;
				SelectableGrid[2, 1] = false;
				SelectableGrid[2, 3] = false;
				SelectableGrid[2, 4] = false;
			}

			public override void Interact() {
				if (Selection.Y == 2) {
					LogSubmit(new string(Details.Display));
					if (_columnPositions[0] == 0 && _columnPositions[1] == 1 && _columnPositions[2] == 3 && _columnPositions[3] == 3 && _columnPositions[4] == 5)
						Solve();
					else
						StrikeFlash();
				} else {
					ref var columnPosition = ref _columnPositions[Selection.X];
					if (Selection.Y == 0) {
						columnPosition--;
						if (columnPosition < 0) columnPosition = 5;
					} else {
						columnPosition++;
						if (columnPosition >= 6) columnPosition = 0;
					}
				}
			}
		}

		public class SimonSays : Module<Components.SimonSays.ReadData> {
			internal override Components.SimonSays.ReadData Details => new(Selection, _litColour);

			private readonly Timer _timer = new(500);
			private SimonColour? _litColour;
			private int _stagesCleared;
			private int _inputProgress;
			private int _tick;

			public SimonSays(Simulation simulation) : base(simulation, DefuserConnector.GetComponentReader<Components.SimonSays>(), 3, 3) {
				SelectableGrid[0, 0] = false;
				SelectableGrid[0, 2] = false;
				SelectableGrid[1, 1] = false;
				SelectableGrid[2, 0] = false;
				SelectableGrid[2, 2] = false;
				_timer.Elapsed += Timer_Elapsed;
				_timer.Start();
			}

			private void Timer_Elapsed(object? sender, ElapsedEventArgs e) {
				switch (_tick) {
					case >= 0:
						_inputProgress = 0;
						_litColour = _tick % 3 == 2 ? null : (SimonColour) (_tick / 3);
						break;
					case -18 or -4:
						_litColour = null;
						break;
				}
				_tick++;
				if (_tick >= (_stagesCleared + 1) * 3)
					_tick = -20;
			}

			public override void Interact() {
				var pressedColour = Selection.Y switch {
					0 => SimonColour.Blue,
					2 => SimonColour.Green,
					_ => Selection.X == 0 ? SimonColour.Red : SimonColour.Yellow
				};
				_litColour = pressedColour;
				LogButton(pressedColour);
				if (LightState == ModuleStatus.Solved) return;
				if (pressedColour != (SimonColour) _inputProgress) {
					StrikeFlash();
					_inputProgress = 0;
				} else if (_inputProgress >= _stagesCleared) {
					_stagesCleared++;
					_inputProgress = 0;
					_tick = -6;
					if (_stagesCleared < 4) return;
					Solve();
					_timer.Stop();
				} else {
					_inputProgress++;
					_tick = -20;
				}
			}
		}

			private static readonly string[] DisplayStrings = [ "", "YES", "FIRST", "DISPLAY", "OKAY", "SAYS", "NOTHING", "BLANK", "NO", "LED", "LEAD", "READ", "RED", "REED", "LEED",
				"HOLD ON", "YOU", "YOU ARE", "YOUR", "YOU'RE", "UR", "THERE", "THEY'RE", "THEIR", "THEY ARE", "SEE", "C", "CEE" ];
			private static readonly string[] KeyStrings = [ "READY", "FIRST", "NO", "BLANK", "NOTHING", "YES", "WHAT", "UHHH", "LEFT", "RIGHT", "MIDDLE", "OKAY", "WAIT", "PRESS",
				"YOU", "YOU ARE", "YOUR", "YOU’RE", "UR", "U", "UH HUH", "UH UH", "WHAT?", "DONE", "NEXT", "HOLD", "SURE", "LIKE" ];

		public partial class WhosOnFirst : Module<Components.WhosOnFirst.ReadData> {
			internal override Components.WhosOnFirst.ReadData Details => !_isAnimating ? new(Selection, _stagesCleared, _display, _keys) : throw new InvalidOperationException("Tried to read module while animating");

			private string _display;
			private readonly string[] _keys = new string[6];
			private int _stagesCleared;
			private bool _isAnimating;
			private readonly Timer _animationTimer = new(2900) { AutoReset = false };

			public WhosOnFirst(Simulation simulation) : base(simulation, DefuserConnector.GetComponentReader<Components.WhosOnFirst>(), 2, 3) {
				_animationTimer.Elapsed += AnimationTimer_Elapsed;
				SetKeysAndDisplay();
			}

			private void AnimationTimer_Elapsed(object? sender, ElapsedEventArgs e) {
				SetKeysAndDisplay();
				_isAnimating = false;
			}

			[MemberNotNull(nameof(_display))]
			private void SetKeysAndDisplay() {
				for (var i = 0; i < 6; i++)
					_keys[i] = KeyStrings[i + _stagesCleared * 8];
				_display = DisplayStrings[_stagesCleared];
			}

			private void StartAnimation() {
				_isAnimating = true;
				_animationTimer.Start();
			}

			public override void Interact() {
				if (_isAnimating) throw new InvalidOperationException("Memory button pressed during animation");
				LogButtonPressed(Selection.X + Selection.Y * 2 + 1);
				if (LightState == ModuleStatus.Solved) return;
				if (Selection.X + Selection.Y * 2 != _stagesCleared) {
					StrikeFlash();
					StartAnimation();
				} else {
					_stagesCleared++;
					if (_stagesCleared >= 3)
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
					_readFailSimulation--;
					if (_readFailSimulation < 0) _readFailSimulation = 3;
					return !_isAnimating
						? new(Selection, _stagesCleared, 1 + _currentPage * 3, _wires.Skip(_stagesCleared * 3).Take(3).Select(w => w?.Colour).ToArray(),
							_readFailSimulation == 0 && Selection.Y is >= 1 and <= 3 && _wires[Selection.Y - 1 + _currentPage * 3] is { } wire ? new(Selection.Y - 1, wire.To) : null)
						: throw new InvalidOperationException("Tried to read module while animating");
				}
			}

			private readonly WireData?[] _wires = new WireData?[12];
			private readonly bool[] _shouldCut = new bool[12];
			private readonly bool[] _isCut = new bool[12];
			private int _currentPage;
			private int _stagesCleared;
			private bool _isAnimating;
			private int _readFailSimulation;
			private readonly Timer _animationTimer = new(1200) { AutoReset = false };

			public WireSequence(Simulation simulation) : base(simulation, DefuserConnector.GetComponentReader<Components.WireSequence>(), 1, 5) {
				_animationTimer.Elapsed += AnimationTimer_Elapsed;
				for (var i = 0; i < 12; i++) {
					if (i % 4 == 0) continue;
					var colour = (Components.WireSequence.WireColour) (i / 2 % 3);
					_wires[i] = new(colour, (char) ('A' + i % 3));
					_shouldCut[i] = colour == Components.WireSequence.WireColour.Blue;
				}
				UpdateSelectable();
			}

			private void UpdateSelectable() {
				for (var y = 0; y < 3; y++)
					SelectableGrid[y + 1, 0] = _currentPage < 4 && _wires[y + _currentPage * 3] is not null;
			}

			private void AnimationTimer_Elapsed(object? sender, ElapsedEventArgs e) {
				UpdateSelectable();
				_isAnimating = false;
			}

			private void StartAnimation() {
				_isAnimating = true;
				_animationTimer.Start();
			}

			public override void Interact() {
				if (LightState == ModuleStatus.Solved) return;
				if (_isAnimating) throw new InvalidOperationException("Wire Sequence interacted during animation");
				switch (Selection.Y) {
					case 0:
						if (_currentPage == 0) throw new InvalidOperationException("Tried to move up from the first page");
						_currentPage--;
						LogPageChanged(_currentPage + 1);
						StartAnimation();
						break;
					case 4:
						if (_currentPage == _stagesCleared) {
							if (Enumerable.Range(_currentPage * 3, 3).Any(i => _shouldCut[i] && !_isCut[i]))
								StrikeFlash();
							else {
								_currentPage++;
								_stagesCleared++;
								if (_currentPage == 4)
									Solve();
								LogPageChanged(_currentPage + 1);
								StartAnimation();
							}
						} else {
							_currentPage++;
							LogPageChanged(_currentPage + 1);
							StartAnimation();
						}
						break;
					default:
						if (_isCut[_currentPage * 3 + Selection.Y - 1]) return;
						LogCutWire(_currentPage * 3 + Selection.Y);
						_isCut[_currentPage * 3 + Selection.Y - 1] = true;
						if (!_shouldCut[_currentPage * 3 + Selection.Y - 1])
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
			internal override Components.ColourFlash.ReadData Details => _index >= 0 ? new(Selection, _sequence[_index].Word, _sequence[_index].Colour) : new(Selection, null, Components.ColourFlash.Colour.None);

			private int _index;
			private readonly Components.ColourFlash.ReadData[] _sequence;
			private readonly Timer _animationTimer = new(750);

			public ColourFlash(Simulation simulation) : base(simulation, DefuserConnector.GetComponentReader<Components.ColourFlash>(), 2, 1) {
				_sequence = [ new(null, "RED", Components.ColourFlash.Colour.Yellow), new(null, "YELLOW", Components.ColourFlash.Colour.Green), new(null, "GREEN", Components.ColourFlash.Colour.Blue),
					new(null, "BLUE", Components.ColourFlash.Colour.Magenta), new(null, "MAGENTA", Components.ColourFlash.Colour.White), new(null, "WHITE", Components.ColourFlash.Colour.Blue),
					new(null, "RED", Components.ColourFlash.Colour.Red), new(null, "BLUE", Components.ColourFlash.Colour.Blue) ];
				_animationTimer.Elapsed += AnimationTimer_Elapsed;
				_animationTimer.Start();
			}

			private void AnimationTimer_Elapsed(object? sender, ElapsedEventArgs e) {
				_index++;
				if (_index >= _sequence.Length) _index = -4;
			}

			public override void Interact() {
				switch (Selection.X) {
					case 0:
						LogYes(_index);
						if (_index == 0) {
							Solve();
							_animationTimer.Stop();
							_index = -1;
						} else
							StrikeFlash();
						break;
					case 1:
						LogNo(_index);
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

		public partial class CrazyTalk(Simulation simulation, string display, int downTime, int upTime) : Module<Components.CrazyTalk.ReadData>(simulation, DefuserConnector.GetComponentReader<Components.CrazyTalk>(), 1, 1) {
			internal override Components.CrazyTalk.ReadData Details => new(display, _switchIsDown);
			
			private bool _switchIsDown;
			private int _correctMoves;

			public override void Interact() {
				var elapsed = TimerComponent.Instance.Elapsed;
				_switchIsDown = !_switchIsDown;
				LogSwitchMoved(_switchIsDown ? "down" : "up", elapsed);
				if (elapsed.Ticks / TimeSpan.TicksPerSecond % 10 == (_switchIsDown ? downTime : upTime)) {
					_correctMoves++;
					if (_correctMoves >= 2)
						Solve();
				} else {
					_correctMoves = 0;
					StrikeFlash();
				}
			}

			#region Log templates

			[LoggerMessage(LogLevel.Information, "Switch moved {NewState} at {Time}.")]
			private partial void LogSwitchMoved(string newState, TimeSpan time);

			#endregion
		}

		public class EmojiMath : Module<Components.EmojiMath.ReadData> {
			internal override Components.EmojiMath.ReadData Details => new(Selection, _display);

			private readonly string _display;
			private readonly int _answer;
			private int _input;
			private bool _minus;

			public EmojiMath(Simulation simulation) : base(simulation, DefuserConnector.GetComponentReader<Components.EmojiMath>(), 4, 3) {
				var random = new Random();
				int a = random.Next(100), b = random.Next(100);
				var subtract = random.Next(2) != 0;
				_answer = subtract ? a - b : a + b;
				_input = 0;
				_minus = false;
				_display = string.Join(null, from c in $"{a}{(subtract ? '-' : '+')}{b}" select c switch { '0' => ":)", '1' => "=(", '2' => "(:", '3' => ")=", '4' => ":(", '5' => "):", '6' => "=)", '7' => "(=", '8' => ":|", '9' => "|:", _ => c.ToString() });
			}

			public override void Interact() {
				if (Selection.X == 3) {
					switch (Selection.Y) {
						case 0:
							LogButton('0');
							_input *= 10;
							return;
						case 1:
							LogButton('-');
							_minus = !_minus;
							return;
						default:
							if (_minus) _input = -_input;
							LogSubmit(_input);
							if (_input != _answer) StrikeFlash();
							Solve();
							return;
					}
				}

				var n = Selection.X + 1 + Selection.Y * 3;
				LogButton((char) ('0' + n));
				_input = _input * 10 + n;
			}
		}

		public class LetterKeys : Module<Components.LetterKeys.ReadData> {
			internal override Components.LetterKeys.ReadData Details => new(Selection, _display, _labels);

			private readonly char[] _labels;
			private readonly int _display;
			private readonly int _correctButton;

			public LetterKeys(Simulation simulation, int display, char correctLetter) : base(simulation, DefuserConnector.GetComponentReader<Components.LetterKeys>(), 2, 2) {
				_display = display;
				_labels = [.. from i in Enumerable.Range('A', 4) select (char) i];
				for (var i = 3; i > 0; i--) {
					var j = simulation.Random.Next(i + 1);
					(_labels[i], _labels[j]) = (_labels[j], _labels[i]);
				}
				_correctButton = Array.IndexOf(_labels, correctLetter);
			}

			public override void Interact() {
				var index = Selection.X + Selection.Y * 2;
				LogButton(_labels[index]);
				if (index == _correctButton)
					Solve();
				else
					StrikeFlash();
			}
		}

		public partial class LightsOut(Simulation simulation) : NeedyModule<Components.LightsOut.ReadData>(simulation, DefuserConnector.GetComponentReader<Components.LightsOut>(), 3, 3) {
			internal override Components.LightsOut.ReadData Details => new(Selection, IsActive ? (int) RemainingTime.TotalSeconds : null, _lights);

			private readonly bool[] _lights = new bool[9];

			protected override void OnActivate() {
				var random = new Random();
				var numMoves = random.Next(10) + 1;
				Array.Clear(_lights);
				for (; numMoves > 0; numMoves--) {
					var button = random.Next(9);
					Toggle(button);
				}
				LogLights(_lights);
			}

			private void Toggle(int button) {
				_lights[0] ^= button is 0 or 1 or 3;
				_lights[1] ^= button is 0 or 1 or 2 or 4;
				_lights[2] ^= button is 1 or 2 or 5;
				_lights[3] ^= button is 0 or 3 or 4 or 6;
				_lights[4] ^= button is 1 or 3 or 4 or 5 or 7;
				_lights[5] ^= button is 2 or 4 or 5 or 8;
				_lights[6] ^= button is 3 or 6 or 7;
				_lights[7] ^= button is 4 or 6 or 7 or 8;
				_lights[8] ^= button is 5 or 7 or 8;
			}

			public override void Interact() {
				Toggle(Selection.X + Selection.Y * 3);
				LogLights(_lights);
				if (!_lights.Any(b => b)) Deactivate();
			}

			protected override void OnTimerExpired() {
				base.OnTimerExpired();
				Array.Clear(_lights);
			}

			private void LogLights(bool[] state) {
				if (!Logger.IsEnabled(LogLevel.Information)) return;
				var str = string.Join(' ', from y in Enumerable.Range(0, 3) select string.Join(null, from x in Enumerable.Range(0, 3) select state[x + y * 3] ? '*' : '·'));
				LogLights(str);
			}

			#region Log templates

			[LoggerMessage(LogLevel.Information, "Lights state: {State}.")]
			private partial void LogLights(string state);

			#endregion
		}

		public class NeedyMath(Simulation simulation) : NeedyModule<Components.NeedyMath.ReadData>(simulation, DefuserConnector.GetComponentReader<Components.NeedyMath>(), 4, 3) {
			internal override Components.NeedyMath.ReadData Details => new(Selection, IsActive ? (int) RemainingTime.TotalSeconds : null, _display);

			private string _display = "";
			private int _answer;
			private int _input;
			private bool _minus;

			protected override void OnActivate() {
				var random = new Random();
				int a = random.Next(100), b = random.Next(100);
				var subtract = random.Next(2) != 0;
				_answer = subtract ? a - b : a + b;
				_input = 0;
				_minus = false;
				_display = $"{a}{(subtract ? '-' : '+')}{b}";
			}

			public override void Interact() {
				if (Selection.X == 3) {
					switch (Selection.Y) {
						case 0:
							LogButton('0');
							_input *= 10;
							return;
						case 1:
							LogButton('-');
							_minus = !_minus;
							return;
						default:
							if (!IsActive) return;
							if (_minus) _input = -_input;
							LogSubmit(_input);
							if (_input != _answer) StrikeFlash();
							Deactivate();
							_display = "";
							return;
					}
				}

				var n = Selection.X + 1 + Selection.Y * 3;
				LogButton((char) ('0' + n));
				_input = _input * 10 + n;
			}
		}

		public partial class PianoKeys(Simulation simulation) : Module<Components.PianoKeys.ReadData>(simulation, DefuserConnector.GetComponentReader<Components.PianoKeys>(), 12, 1) {
			private static readonly string[] Labels = ["C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"];

			internal override Components.PianoKeys.ReadData Details => new(Selection, [Components.PianoKeys.Symbol.CutCommonTime, Components.PianoKeys.Symbol.Natural, Components.PianoKeys.Symbol.Fermata]);

			private readonly int[] _correctSequence = [4, 6, 6, 6, 6, 4, 4, 4];
			private int _index;

			public override void Interact() {
				LogKeyPressed(Labels[Selection.X]);
				if (_index >= _correctSequence.Length) return;
				if (Selection.X == _correctSequence[_index]) {
					_index++;
					if (_index >= _correctSequence.Length)
						Solve();
				} else {
					StrikeFlash();
					_index = 0;
				}
			}

			#region Log templates

			[LoggerMessage(LogLevel.Information, "{Note} was played.")]
			private partial void LogKeyPressed(string note);

			#endregion
		}

		public class Semaphore(Simulation simulation) : Module<Components.Semaphore.ReadData>(simulation, DefuserConnector.GetComponentReader<Components.Semaphore>(), 3, 1) {
			private const int CorrectIndex = 5;

			internal override Components.Semaphore.ReadData Details => new(Selection, _signals[_index].LeftFlag, _signals[_index].RightFlag);

			private readonly Components.Semaphore.ReadData[] _signals = [
				new(null, Components.Semaphore.Direction.Up, Components.Semaphore.Direction.Right),
				new(null, Components.Semaphore.Direction.DownLeft, Components.Semaphore.Direction.Down),
				new(null, Components.Semaphore.Direction.Left, Components.Semaphore.Direction.Down),
				new(null, Components.Semaphore.Direction.Up, Components.Semaphore.Direction.UpRight),
				new(null, Components.Semaphore.Direction.UpLeft, Components.Semaphore.Direction.Down),
				new(null, Components.Semaphore.Direction.Up, Components.Semaphore.Direction.Down),
				new(null, Components.Semaphore.Direction.Up, Components.Semaphore.Direction.Right),
				new(null, Components.Semaphore.Direction.Up, Components.Semaphore.Direction.Down),
				new(null, Components.Semaphore.Direction.Down, Components.Semaphore.Direction.UpRight)
			];
			private int _index;

			public override void Interact() {
				switch (Selection.X) {
					case 0:
						if (_index > 0) _index--;
						break;
					case 2:
						if (_index < _signals.Length - 1) _index++;
						break;
					case 1:
						LogSubmit(Details);
						if (_index == CorrectIndex)
							Solve();
						else
							StrikeFlash();
						break;
				}
			}
		}
	
		public partial class Switches : Module<Components.Switches.ReadData> {
			private static readonly HashSet<int> InvalidStates = [0x04, 0x0B, 0x0F, 0x12, 0x13, 0x17, 0x18, 0x1A, 0x1C, 0x1E];

			internal override Components.Switches.ReadData Details => new(Selection, _currentState, _targetState);

			private readonly bool[] _currentState;
			private readonly bool[] _targetState;

			public Switches(Simulation simulation) : base(simulation, DefuserConnector.GetComponentReader<Components.Switches>(), 5, 1) {
				var random = new Random();
				int currentStateInt, targetStateInt;
				do {
					currentStateInt = random.Next(0, 0x20);
				} while (InvalidStates.Contains(currentStateInt));
				_currentState = [(currentStateInt & 0x10) != 0, (currentStateInt & 0x08) != 0, (currentStateInt & 0x04) != 0, (currentStateInt & 0x02) != 0, (currentStateInt & 0x01) != 0];
				do {
					targetStateInt = random.Next(0, 0x20);
				} while (targetStateInt == currentStateInt || InvalidStates.Contains(targetStateInt));
				_targetState = [(targetStateInt & 0x10) != 0, (targetStateInt & 0x08) != 0, (targetStateInt & 0x04) != 0, (targetStateInt & 0x02) != 0, (targetStateInt & 0x01) != 0];
				LogTargetState(string.Join(null, from state in _targetState select state ? '^' : 'v'));
				LogState(string.Join(null, from state in _currentState select state ? '^' : 'v'));
			}

			public override void Interact() {
				_currentState[Selection.X] ^= true;
				if (InvalidStates.Contains(ConvertToInt(_currentState))) {
					_currentState[Selection.X] ^= true;
					StrikeFlash();
				} else {
					LogState(string.Join(null, from state in _currentState select state ? '^' : 'v'));
					if (_currentState.SequenceEqual(_targetState))
						Solve();
				}
			}

			private static int ConvertToInt(bool[] state) => (state[0] ? 0x10 : 0) | (state[1] ? 0x08 : 0) | (state[2] ? 0x04 : 0) | (state[3] ? 0x02 : 0) | (state[4] ? 0x01 : 0);  

			#region Log templates

			[LoggerMessage(LogLevel.Information, "Target state: {state}")]
			private partial void LogTargetState(string state);

			[LoggerMessage(LogLevel.Information, "Switch state: {state}")]
			private partial void LogState(string state);

			#endregion
		}

		public class TurnTheKeys(Simulation simulation) : Module<Components.TurnTheKeys.ReadData>(simulation, DefuserConnector.GetComponentReader<Components.TurnTheKeys>(), 2, 1) {
			internal override Components.TurnTheKeys.ReadData Details => new(Selection, _priority, _isKey1Turned, _isKey2Turned);

			private readonly int _priority = simulation.Random.Next(10000);
			private bool _isKey1Turned;
			private bool _isKey2Turned;

			public override void Interact() {
				if (CheckStrike()) {
					StrikeFlash();
				} else {
					if (Selection.X == 0) _isKey1Turned = true; else _isKey2Turned = true;
					if (_isKey1Turned && _isKey2Turned) Solve();
				}
			}

			private bool CheckStrike() {
				foreach (var f in Simulation._moduleFaces) {
					foreach (var m in f.Slots.OfType<Module>()) {
						if (Selection.X == 0) {
							if (m == this) {
								if (!_isKey2Turned) return true;
							} else {
								switch (m) {
									case TurnTheKeys turnTheKeys:
										if (!turnTheKeys._isKey2Turned || (turnTheKeys._priority < _priority ? !turnTheKeys._isKey1Turned : turnTheKeys._isKey1Turned))
											return true;
										break;
									case Password or WhosOnFirst or CrazyTalk or Keypad:
										if (m.LightState != ModuleStatus.Solved) return true;
										break;
									case Maze or Memory or ComplicatedWires or WireSequence:
										if (m.LightState == ModuleStatus.Solved) return true;
										break;
								}
							}
						} else {
							if (m == this) {
								if (_isKey1Turned) return true;
							} else {
								switch (m) {
									case TurnTheKeys turnTheKeys:
										if (turnTheKeys._isKey1Turned || (turnTheKeys._priority < _priority ? turnTheKeys._isKey2Turned : !turnTheKeys._isKey2Turned))
											return true;
										break;
									case MorseCode or Wires or Button or ColourFlash:
										if (m.LightState != ModuleStatus.Solved) return true;
										break;
									case Semaphore or SimonSays or Switches:
										if (m.LightState == ModuleStatus.Solved) return true;
										break;
								}
							}
						}
					}
				}
				return false;
			}
		}

		public class WordScramble(Simulation simulation, string display) : Module<Components.WordScramble.ReadData>(simulation, DefuserConnector.GetComponentReader<Components.WordScramble>(), 4, 2) {
			internal override Components.WordScramble.ReadData Details => new(Selection, display.ToCharArray());

			private readonly StringBuilder _answer = new();

			public override void Interact() {
				if (Selection.X == 3) {
					switch (Selection.Y) {
						case 0:
							LogButton("Delete");
							if (_answer.Length != 0) _answer.Remove(_answer.Length - 1, 1);
							return;
						default:
							LogSubmit(_answer.ToString());
							Solve();
							return;
					}
				}

				var c = display[Selection.X + Selection.Y * 3];
				LogButton(c);
				_answer.Append(c);
			}
		}

		#endregion
	}
}
