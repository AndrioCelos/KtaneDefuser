namespace KtaneDefuserScripts;
public static class CenturionUtil {
	/*
	 * The Centurion's grid mapping for gamepad controls is strange, to say the least.
	 * We define face 0 (front) as the face that's facing up when the bomb is loaded, and face 1 (back) as the face that's down.
	 * The component slots on each face are mapped to a grid of 7 columns by 8 rows.
	 * The topmost row on face 0 consists of what's visually the bottom row, left to right, then the three leftmost slots of the second row from the bottom.
	 * The central slot is skipped and is instead mapped to the second slot of the eighth row.
	 * This is where the timer normally is, but if demand-based mod loading is used, a module can be placed there instead.
	 * On face 1, the same mapping is used except it's mirrored horizontally, and the central slot is not skipped and is instead part of the fourth row.
	 * 
	 * To identify modules on the Centurion, we will zoom in on six regions of each face. This is because the whole bomb does not fit on the screen normally,
	 * and zooming out could compromise the resolution too much.
	 */

	public static readonly (Slot centre, Slot[] visibleSlots)[][] SlotGroups = [
		[
			(new(0, 0, 5, 0), [new(0, 0, 0, 0), new(0, 0, 1, 0), new(0, 0, 4, 0), new(0, 0, 5, 0), new(0, 0, 2, 1), new(0, 0, 3, 1), new(0, 0, 4, 1)]),
			(new(0, 0, 0, 1), [new(0, 0, 2, 0), new(0, 0, 3, 0), new(0, 0, 6, 0), new(0, 0, 0, 1), new(0, 0, 1, 1), new(0, 0, 5, 1), new(0, 0, 6, 1), new(0, 0, 0, 2)]),
			(new(0, 0, 2, 3), [new(0, 0, 1, 2), new(0, 0, 2, 2), new(0, 0, 3, 2), new(0, 0, 1, 3), new(0, 0, 2, 3), new(0, 0, 3, 3), new(0, 0, 0, 4), new(0, 0, 1, 4), new(0, 0, 2, 4)]),
			(new(0, 0, 5, 3), [new(0, 0, 4, 2), new(0, 0, 5, 2), new(0, 0, 6, 2), new(0, 0, 0, 3), new(0, 0, 1, 7), new(0, 0, 4, 3), new(0, 0, 5, 3), new(0, 0, 6, 3), new(0, 0, 3, 4), new(0, 0, 4, 4), new(0, 0, 5, 4), new(0, 0, 6, 4)]),
			(new(0, 0, 0, 6), [new(0, 0, 0, 5), new(0, 0, 1, 5), new(0, 0, 2, 5), new(0, 0, 6, 5), new(0, 0, 0, 6), new(0, 0, 4, 6), new(0, 0, 5, 6)]),
			(new(0, 0, 2, 6), [new(0, 0, 3, 5), new(0, 0, 4, 5), new(0, 0, 5, 5), new(0, 0, 1, 6), new(0, 0, 2, 6), new(0, 0, 3, 6), new(0, 0, 6, 6), new(0, 0, 0, 7)])
		],
		[
			(new(0, 1, 5, 0), [new(0, 1, 0, 0), new(0, 1, 1, 0), new(0, 1, 4, 0), new(0, 1, 5, 0), new(0, 1, 6, 0), new(0, 1, 2, 1), new(0, 1, 3, 1), new(0, 1, 4, 1)]),
			(new(0, 1, 0, 1), [new(0, 1, 2, 0), new(0, 1, 3, 0), new(0, 1, 0, 1), new(0, 1, 1, 1), new(0, 1, 5, 1), new(0, 1, 6, 1), new(0, 1, 0, 2)]),
			(new(0, 1, 2, 3), [new(0, 1, 1, 2), new(0, 1, 2, 2), new(0, 1, 3, 2), new(0, 1, 4, 2), new(0, 1, 1, 3), new(0, 1, 2, 3), new(0, 1, 3, 3), new(0, 1, 4, 3), new(0, 1, 1, 4), new(0, 1, 2, 4), new(0, 1, 3, 4), new(0, 1, 4, 4)]),
			(new(0, 1, 6, 3), [new(0, 1, 5, 2), new(0, 1, 6, 2), new(0, 1, 0, 3), new(0, 1, 5, 3), new(0, 1, 6, 3), new(0, 1, 0, 4), new(0, 1, 5, 4), new(0, 1, 6, 4), new(0, 1, 0, 5)]),
			(new(0, 1, 1, 6), [new(0, 1, 1, 5), new(0, 1, 2, 5), new(0, 1, 3, 5), new(0, 1, 0, 6), new(0, 1, 1, 6), new(0, 1, 2, 6), new(0, 1, 5, 6), new(0, 1, 6, 6)]),
			(new(0, 1, 3, 6), [new(0, 1, 4, 5), new(0, 1, 5, 5), new(0, 1, 6, 5), new(0, 1, 3, 6), new(0, 1, 4, 6), new(0, 1, 0, 7), new(0, 1, 1, 7)])
		]
	];


	/// <summary>Returns the quadrilateral representing the location of the specified slot on the screen. The slot should be on the side of the bomb we're already looking at.</summary>
	public static Quadrilateral GetPoints(Slot slot, Slot? referenceSlot = null) {
		if (referenceSlot is null && GameState.Current.FocusState != FocusState.Module)
			throw new InvalidOperationException("A module must be selected to read components on the Centurion.");
		var selectedSlot = referenceSlot ?? GameState.Current.SelectedFace.SelectedSlot;
		if (slot.Bomb != selectedSlot.Bomb || slot.Face != selectedSlot.Face)
			throw new ArgumentException("Specified slot must be on the currently-selected bomb face.", nameof(slot));
		var (vx1, vy1) = GetVisualXY(selectedSlot);
		var (vx2, vy2) = GetVisualXY(slot);
		var dx = vx2 - vx1;
		var dy = vy2 - vy1;
		return dy switch {
			-1 => dx switch {
				-5 => new(new(17, 66), new(317, 66), new(0, 357), new(299, 357)),
				-4 => new(new(180, 66), new(479, 66), new(168, 357), new(469, 357)),
				-3 => new(new(344, 66), new(646, 66), new(338, 357), new(641, 357)),
				-2 => new(new(513, 66), new(810, 66), new(506, 357), new(810, 357)),
				-1 => new(new(681, 66), new(980, 66), new(681, 357), new(980, 357)),
				0 => new(new(847, 66), new(1148, 66), new(847, 357), new(1149, 357)),
				1 => new(new(1014, 66), new(1311, 66), new(1015, 357), new(1312, 357)),
				2 => new(new(1182, 66), new(1480, 66), new(1185, 357), new(1487, 357)),
				3 => new(new(1353, 66), new(1642, 66), new(1351, 357), new(1654, 357)),
				4 => new(new(1517, 66), new(1820, 66), new(1525, 357), new(1824, 357)),
				_ => throw new ArgumentOutOfRangeException(nameof(slot))
			},
			0 => dx switch {
				-4 => new(new(167, 370), new(469, 370), new(155, 676), new(460, 676)),
				-2 => new(new(503, 370), new(811, 370), new(499, 675), new(807, 675)),
				0 => new(new(844, 370), new(1149, 370), new(844, 675), new(1149, 675)),
				2 => new(new(1189, 370), new(1492, 370), new(1190, 675), new(1498, 675)),
				4 => new(new(1532, 370), new(1827, 370), new(1536, 675), new(1831, 675)),
				_ => throw new ArgumentOutOfRangeException(nameof(slot))
			},
			1 => dx switch {
				-4 => new(new(153, 688), new(462, 688), new(138, 1003), new(452, 1003)),
				-3 => new(new(323, 688), new(634, 688), new(314, 1003), new(626, 1003)),
				-2 => new(new(501, 688), new(806, 688), new(495, 1002), new(805, 1002)),
				-1 => new(new(670, 688), new(982, 688), new(670, 1002), new(982, 1002)),
				0 => new(new(843, 688), new(1154, 688), new(843, 1002), new(1155, 1002)),
				1 => new(new(1017, 688), new(1326, 688), new(1018, 1001), new(1329, 1001)),
				2 => new(new(1194, 687), new(1499, 687), new(1195, 1001), new(1505, 1001)),
				3 => new(new(1368, 687), new(1671, 687), new(1369, 1001), new(1687, 1001)),
				4 => new(new(1543, 687), new(1845, 687), new(1544, 1001), new(1855, 1001)),
				_ => throw new ArgumentOutOfRangeException(nameof(slot))
			},
			_ => throw new ArgumentOutOfRangeException(nameof(slot))
		};
	}

	/// <summary>Returns the visual position on the face of the specified slot.</summary>
	/// <returns><c>x</c> is the horizontal position in half-slots right of the centre; <c>y</c> is the vertical position in full slots below the centre.</returns>
	private static (int x, int y) GetVisualXY(Slot slot) {
		var (x, y) = slot.Face == 0
		? slot.Y switch {
			0 => slot.X >= 4 ? (-12 + slot.X * 2, 3) : (-3 + slot.X * 2, 4),
			1 => slot.X >= 2 ? (-9 + slot.X * 2, 2) : (2 + slot.X * 2, 3),
			2 => slot.X >= 1 ? (-8 + slot.X * 2, 1) : (5, 2),
			3 => slot.X switch { >= 4 => (-6 + slot.X * 2, 0), >= 1 => (-8 + slot.X * 2, 0), _ => (6, 1) },
			4 => (-6 + slot.X * 2, -1),
			5 => slot.X >= 6 ? (-4, -3) : (-5 + slot.X * 2, -2),
			6 => slot.X >= 4 ? (-11 + slot.X * 2, -4) : (-2 + slot.X * 2, -3),
			_ => slot.X >= 1 ? (0, 0) : (3, -4),
		}
		: slot.Y switch {
			0 => slot.X >= 4 ? (12 - slot.X * 2, 3) : (3 - slot.X * 2, 4),
			1 => slot.X >= 2 ? (9 - slot.X * 2, 2) : (-2 - slot.X * 2, 3),
			2 => slot.X >= 1 ? (8 - slot.X * 2, 1) : (-5, 2),
			3 => slot.X >= 1 ? (8 - slot.X * 2, 0) : (-6, 1),
			4 => slot.X >= 1 ? (8 - slot.X * 2, -1) : (-6, 0),
			5 => slot.X >= 1 ? (7 - slot.X * 2, -2) : (-6, -1),
			6 => slot.X >= 5 ? (13 - slot.X * 2, -4) : (4 - slot.X * 2, -3),
			_ => (-1 - slot.X * 2, -4)
		};
		return (x, y);
	}
}
