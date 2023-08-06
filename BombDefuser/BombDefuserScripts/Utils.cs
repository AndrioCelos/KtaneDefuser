using System.Text;
using AimlCSharpInterface;

namespace BombDefuserScripts;
internal static class Utils {
	public static Point[] CurrentModulePoints { get; } = new Point[] { new( 836,  390), new(1120,  390), new( 832,  678), new(1124,  678) };
	private static readonly Point[][] modulePointsLists = new[] {
		new Point[] { new(220, 100), new(496, 100), new(193, 359), new(479, 359) },
		new Point[] { new(535, 100), new(806, 101), new(522, 359), new(801, 360) },
		new Point[] { new(840, 101), new(1113, 101), new(836, 360), new(1119, 360) },
		new Point[] { new(1147, 101), new(1407, 101), new(1154, 360), new(1421, 360) },
		new Point[] { new(1456, 102), new(1718, 102), new(1474, 360), new(1745, 360) },
		new Point[] { new(190, 392), new(477, 392), new(160, 678), new(459, 678) },
		new Point[] { new(520, 392), new(800, 392), new(501, 677), new(794, 677) },
		CurrentModulePoints,
		new Point[] { new(1155, 390), new(1425, 390), new(1163, 676), new(1442, 676) },
		new Point[] { new(1476, 390), new(1748, 390), new(1497, 676), new(1779, 676) },
		new Point[] { new(157, 706), new(457, 705), new(124, 1019), new(436, 1018) },
		new Point[] { new(501, 705), new(794, 705), new(481, 1018), new(787, 1017) },
		new Point[] { new(829, 705), new(1125, 704), new(828, 1018), new(1134, 1016) },
		new Point[] { new(1164, 704), new(1444, 704), new(1173, 1016), new(1465, 1015) },
		new Point[] { new(1499, 704), new(1782, 703), new(1521, 1015), new(1816, 1014) }
	};
	private static readonly Point[][] bombPointsLists = new[] {
		new Point[] { new(572, 291), new(821, 291), new(559, 534), new(816, 534) },
		new Point[] { new(852, 291), new(1096, 291), new(848, 534), new(1101, 534) },
		new Point[] { new(1127, 292), new(1369, 292), new(1134, 533), new(1383, 533) },
		new Point[] { new(558, 558), new(816, 558), new(544, 822), new(811, 822) },
		new Point[] { new(848, 558), new(1099, 558), new(845, 821), new(1106, 821) },
		new Point[] { new(1134, 558), new(1385, 558), new(1141, 821), new(1400, 821) }
	};
	internal static readonly Point[][] sideWidgetPointsLists = new[] {
		new Point[] { new( 813,  465), new( 817,  228), new( 988,  465), new( 988,  228) },
		new Point[] { new( 988,  465), new( 988,  228), new(1163,  465), new(1158,  228) },
		new Point[] { new( 808,  772), new( 812,  515), new( 988,  772), new( 988,  515) },
		new Point[] { new( 988,  772), new( 988,  515), new(1168,  772), new(1164,  515) }
	};
	internal static readonly Point[][] topBottomWidgetPointsLists = new[] {
		new Point[] { new( 588,  430), new( 784,  430), new( 587,  541), new( 784,  541) },
		new Point[] { new( 824,  430), new(1140,  430), new( 824,  541), new(1140,  541) },
		new Point[] { new(1181,  430), new(1389,  430), new(1182,  540), new(1390,  541) },
		new Point[] { new( 580,  566), new( 783,  566), new( 578,  678), new( 782,  678) },
		new Point[] { new( 821,  566), new(1140,  566), new( 821,  678), new(1140,  678) },
		new Point[] { new(1181,  566), new(1390,  566), new(1182,  678), new(1392,  678) }
	};

	public static IReadOnlyList<Point> GetPoints(ComponentSlot slot) {
		if (GameState.Current.FocusState == FocusState.Module) {
			var selectedSlot = GameState.Current.SelectedFace.SelectedSlot;
			var dx = slot.X - selectedSlot.X;
			var dy = slot.Y - selectedSlot.Y;
			return modulePointsLists[(dy + 1) * 5 + dx + 2];
		} else
			return bombPointsLists[slot.Y * 3 + slot.X];
	}

	public static int ParseOrdinal(string ordinal) => (int) Enum.Parse<Ordinal>(ordinal, true);
	public static string ToOrdinal(int n) => $"{n}{(n % 10) switch { 1 => "st", 2 => "nd", 3 => "rd", _ => "th" }}";

	internal static async Task SelectFaceAsync(Interrupt interrupt, int face, SelectFaceAlignMode alignMode) {
		if (GameState.Current.SelectedFaceNum == face) return;  // The requested side is already selected; do nothing.
		interrupt.SendInputs("rx:1");
		if (alignMode == SelectFaceAlignMode.CheckWidgets) {
			await AimlTasks.Delay(0.375);
			interrupt.SendInputs("rx:0");
			using var ss = DefuserConnector.Instance.TakeScreenshot();
			Edgework.RegisterWidgets(interrupt.Context, true, ss);
			interrupt.SendInputs("rx:1");
			await AimlTasks.Delay(0.125);
		} else
			await AimlTasks.Delay(0.75);

		GameState.Current.FocusState = FocusState.Bomb;
		GameState.Current.SelectedFaceNum = face;
		GameState.Current.SelectedModuleNum = null;

		// Correct the bomb rotation.
		if (alignMode == SelectFaceAlignMode.None)
			interrupt.SendInputs("rx:0");
		else {
			// If the face we're moving to is known to have modules on it, pressing A B is faster.
			if (GameState.Current.SelectedFace.HasModules) {
				interrupt.SendInputs("rx:0 a b");
				await AimlTasks.Delay(0.5);
			} else {
				interrupt.SendInputs("rx:0 b a");
				await AimlTasks.Delay(1.5);
			}
		}
	}

	internal static async Task SelectModuleAsync(Interrupt interrupt, int moduleNum, bool waitForFocus) {
		if (GameState.Current.SelectedModuleNum == moduleNum)
			return;  // The requested module is already selected.
		var builder = new StringBuilder();
		var slot = GameState.Current.Modules[moduleNum].Slot;
		await SelectFaceAsync(interrupt, slot.Face, SelectFaceAlignMode.None);
		// If another module is selected, deselect it first.
		if (GameState.Current.SelectedModuleNum is not null)
			builder.Append("b ");
		// Move to the correct row.
		var currentSlot = GameState.Current.SelectedFace.SelectedSlot;
		if (slot.Y != currentSlot.Y) {
			if (slot.Y < currentSlot.Y)
				builder.Append("up ");
			else
				builder.Append("down ");

			// Find which slot this input will select.
			currentSlot.Y = slot.Y;
			if (GameState.Current.SelectedFace[currentSlot]?.Reader is null or BombDefuserConnector.Components.Timer) {
				for (var d = 0; d < 2; d++) {
					var x2 = currentSlot.X + (d == 0 ? -1 : 1);
					if (x2 >= 0 && x2 < 3 && GameState.Current.SelectedFace[x2, currentSlot.Y]?.Reader is not (null or BombDefuserConnector.Components.Timer)) {
						currentSlot.X = x2;
						break;
					}
				}
			}
		}
		// Move to the correct module within that row.
		while (slot.X < currentSlot.X) {
			builder.Append("left ");
			do { currentSlot.X--; } while (GameState.Current.SelectedFace[currentSlot]?.Reader is null or BombDefuserConnector.Components.Timer);
		}
		while (slot.X > currentSlot.X) {
			builder.Append("right ");
			do { currentSlot.X++; } while (GameState.Current.SelectedFace[currentSlot]?.Reader is null or BombDefuserConnector.Components.Timer);
		}
		builder.Append('a');
		await interrupt.SendInputsAsync(builder.ToString());
		if (waitForFocus) await AimlTasks.Delay(0.75);

		GameState.Current.SelectedFace.SelectedSlot = slot;
		GameState.Current.SelectedModuleNum = moduleNum;
		GameState.Current.FocusState = FocusState.Module;
	}

	internal static bool CanReadModuleImmediately(int moduleIndex) => GameState.Current.Modules[moduleIndex].Slot.Face == GameState.Current.SelectedFaceNum;
}

[AimlSet]
public enum Ordinal {
	[AimlSetItem("zeroth"), AimlSetItem("0th")]
	Zeroth,
	[AimlSetItem("first"), AimlSetItem("1st")]
	First,
	[AimlSetItem("second"), AimlSetItem("2nd")]
	Second,
	[AimlSetItem("third"), AimlSetItem("3rd")]
	Third,
	[AimlSetItem("fourth"), AimlSetItem("4th")]
	Fourth,
	[AimlSetItem("fifth"), AimlSetItem("5th")]
	Fifth,
	[AimlSetItem("sixth"), AimlSetItem("6th")]
	Sixth,
	[AimlSetItem("seventh"), AimlSetItem("7th")]
	Seventh,
	[AimlSetItem("eighth"), AimlSetItem("8th")]
	Eighth,
	[AimlSetItem("ninth"), AimlSetItem("9th")]
	Ninth,
	[AimlSetItem("tenth"), AimlSetItem("10th")]
	Tenth
}