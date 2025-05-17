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
	 */

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

	/// <summary>Gets a quadrilateral representing the location of the specified slot on the screen. The slot should be on the side of the bomb we're already looking at.</summary>
	/// <returns><see langword='true'/> if the specified slot is visible; <see langword='false'/> otherwise.</returns>
	public static bool TryGetPoints(Slot slot, bool isZoomedOut, out Quadrilateral quadrilateral) => TryGetPoints(slot, isZoomedOut, GameState.Current.SelectedFace.SelectedSlot, out quadrilateral);
	public static bool TryGetPoints(Slot slot, bool isZoomedOut, Slot referenceSlot, out Quadrilateral quadrilateral) {
		if (GameState.Current.FocusState != FocusState.Module) {
			var (vx, vy) = GetVisualXY(slot);
			if (isZoomedOut) {
				quadrilateral = vy switch {
					4 => vx switch {
						-3 => new(new(768, 894), new(862, 894), new(765, 994), new(860, 994)),
						-1 => new(new(875, 894), new(968, 894), new(872, 994), new(968, 994)),
						1 => new(new(980, 893), new(1073, 893), new(981, 994), new(1075, 994)),
						3 => new(new(1086, 893), new(1178, 893), new(1088, 993), new(1182, 993)),
						_ => new()
					},
					_ => new()
				};
			} else {
				quadrilateral = vy switch {
					-4 => vx switch {
						-3 => new(new(717, 4), new(834, 4), new(717, 111), new(832, 111)),
						-1 => new(new(852, 4), new(969, 4), new(852, 111), new(969, 111)),
						1 => new(new(984, 4), new(1100, 4), new(984, 112), new(1101, 112)),
						3 => new(new(1115, 4), new(1232, 4), new(1118, 112), new(1232, 112)),
						_ => new()
					},
					-3 => vx switch {
						-4 => new(new(649, 119), new(768, 119), new(648, 231), new(765, 231)),
						-2 => new(new(784, 119), new(902, 119), new(783, 231), new(902, 231)),
						0 => new(new(917, 119), new(1036, 119), new(917, 231), new(1036, 231)),
						2 => new(new(1051, 119), new(1169, 119), new(1052, 231), new(1170, 231)),
						4 => new(new(1186, 120), new(1303, 120), new(1189, 231), new(1303, 231)),
						_ => new()
					},
					-2 => vx switch {
						-5 => new(new(577, 238), new(695, 238), new(575, 354), new(692, 354)),
						-3 => new(new(712, 238), new(832, 238), new(712, 354), new(831, 354)),
						-1 => new(new(848, 238), new(970, 238), new(848, 354), new(969, 354)),
						1 => new(new(984, 238), new(1106, 238), new(985, 354), new(1106, 354)),
						3 => new(new(1122, 238), new(1241, 238), new(1124, 354), new(1242, 354)),
						5 => new(new(1259, 239), new(1377, 239), new(1262, 355), new(1378, 355)),
						_ => new()
					},
					-1 => vx switch {
						-6 => new(new(499, 361), new(623, 361), new(497, 483), new(616, 483)),
						-4 => new(new(638, 361), new(761, 361), new(638, 483), new(758, 483)),
						-2 => new(new(777, 361), new(901, 361), new(777, 483), new(900, 483)),
						0 => new(new(915, 361), new(1039, 361), new(915, 483), new(1040, 483)),
						2 => new(new(1055, 361), new(1177, 361), new(1056, 483), new(1178, 483)),
						4 => new(new(1193, 361), new(1316, 361), new(1197, 483), new(1317, 483)),
						6 => new(new(1332, 361), new(1453, 361), new(1338, 483), new(1459, 483)),
						_ => new()
					},
					0 => vx switch {
						-6 => new(new(491, 489), new(618, 489), new(485, 616), new(611, 616)),
						-4 => new(new(632, 489), new(758, 489), new(628, 616), new(755, 616)),
						-2 => new(new(774, 489), new(900, 489), new(772, 615), new(899, 615)),
						0 => new(new(915, 489), new(1041, 489), new(915, 615), new(1041, 615)),
						2 => new(new(1057, 489), new(1183, 489), new(1058, 615), new(1184, 615)),
						4 => new(new(1198, 489), new(1320, 489), new(1202, 615), new(1328, 615)),
						6 => new(new(1340, 489), new(1462, 489), new(1345, 615), new(1470, 615)),
						_ => new()
					},
					1 => vx switch {
						-6 => new(new(483, 621), new(610, 621), new(476, 751), new(605, 751)),
						-4 => new(new(627, 621), new(755, 621), new(622, 751), new(751, 751)),
						-2 => new(new(771, 621), new(899, 621), new(770, 751), new(898, 751)),
						0 => new(new(914, 621), new(1042, 621), new(914, 751), new(1043, 751)),
						2 => new(new(1058, 621), new(1185, 621), new(1060, 750), new(1190, 750)),
						4 => new(new(1202, 621), new(1329, 621), new(1206, 750), new(1334, 750)),
						6 => new(new(1345, 621), new(1471, 621), new(1352, 750), new(1479, 750)),
						_ => new()
					},
					2 => vx switch {
						-5 => new(new(548, 759), new(678, 759), new(541, 891), new(673, 891)),
						-3 => new(new(694, 758), new(825, 758), new(690, 891), new(823, 891)),
						-1 => new(new(841, 759), new(971, 759), new(840, 890), new(971, 890)),
						1 => new(new(987, 758), new(1117, 758), new(987, 890), new(1118, 890)),
						3 => new(new(1134, 758), new(1263, 758), new(1136, 890), new(1267, 889)),
						5 => new(new(1280, 758), new(1408, 758), new(1285, 889), new(1415, 889)),
						_ => new()
					},
					3 => vx switch {
						-4 => new(new(615, 902), new(748, 902), new(608, 1036), new(744, 1036)),
						-2 => new(new(764, 901), new(897, 901), new(761, 1035), new(896, 1035)),
						0 => new(new(913, 901), new(1046, 901), new(913, 1035), new(1046, 1035)),
						2 => new(new(1062, 901), new(1194, 901), new(1064, 1034), new(1198, 1034)),
						4 => new(new(1211, 900), new(1342, 900), new(1216, 1034), new(1349, 1033)),
						_ => new()
					},
					4 => vx switch {
						-3 => new(new(685, 1050), new(820, 1050), new(683, 1080), new(819, 1080)),
						-1 => new(new(838, 1050), new(972, 1050), new(836, 1080), new(972, 1080)),
						1 => new(new(988, 1049), new(1121, 1049), new(988, 1080), new(1121, 1080)),
						3 => new(new(1140, 1049), new(1270, 1049), new(1141, 1080), new(1271, 1080)),
						_ => new()
					},
					_ => new()
				};
			}
		} else {
			if (slot.Bomb != referenceSlot.Bomb || slot.Face != referenceSlot.Face)
				throw new ArgumentException("Specified slot must be on the currently-selected bomb face.", nameof(slot));
			var (vx1, vy1) = GetVisualXY(referenceSlot);
			var (vx2, vy2) = GetVisualXY(slot);
			var dx = vx2 - vx1;
			var dy = vy2 - vy1;
			quadrilateral = dy switch {
				-1 => dx switch {
					-5 => new(new(  17, 66), new( 317, 66), new(   0, 357), new( 299, 357)),
					-4 => new(new( 180, 66), new( 479, 66), new( 168, 357), new( 469, 357)),
					-3 => new(new( 344, 66), new( 646, 66), new( 338, 357), new( 641, 357)),
					-2 => new(new( 513, 66), new( 810, 66), new( 506, 357), new( 810, 357)),
					-1 => new(new( 681, 66), new( 980, 66), new( 681, 357), new( 980, 357)),
					 0 => new(new( 847, 66), new(1148, 66), new( 847, 357), new(1149, 357)),
					 1 => new(new(1014, 66), new(1311, 66), new(1015, 357), new(1312, 357)),
					 2 => new(new(1182, 66), new(1480, 66), new(1185, 357), new(1487, 357)),
					 3 => new(new(1353, 66), new(1642, 66), new(1351, 357), new(1654, 357)),
					 4 => new(new(1517, 66), new(1820, 66), new(1525, 357), new(1824, 357)),
					_ => new()
				},
				0 => dx switch {
					-4 => new(new( 167, 370), new( 469, 370), new( 155, 676), new( 460, 676)),
					-2 => new(new( 503, 370), new( 811, 370), new( 499, 675), new( 807, 675)),
					 0 => new(new( 844, 370), new(1149, 370), new( 844, 675), new(1149, 675)),
					 2 => new(new(1189, 370), new(1492, 370), new(1190, 675), new(1498, 675)),
					 4 => new(new(1532, 370), new(1827, 370), new(1536, 675), new(1831, 675)),
					_ => new()
				},
				1 => dx switch {
					-4 => new(new( 153, 688), new( 462, 688), new( 138, 1003), new( 452, 1003)),
					-3 => new(new( 323, 688), new( 634, 688), new( 314, 1003), new( 626, 1003)),
					-2 => new(new( 501, 688), new( 806, 688), new( 495, 1002), new( 805, 1002)),
					-1 => new(new( 670, 688), new( 982, 688), new( 670, 1002), new( 982, 1002)),
					 0 => new(new( 843, 688), new(1154, 688), new( 843, 1002), new(1155, 1002)),
					 1 => new(new(1017, 688), new(1326, 688), new(1018, 1001), new(1329, 1001)),
					 2 => new(new(1194, 687), new(1499, 687), new(1195, 1001), new(1505, 1001)),
					 3 => new(new(1368, 687), new(1671, 687), new(1369, 1001), new(1687, 1001)),
					 4 => new(new(1543, 687), new(1845, 687), new(1544, 1001), new(1855, 1001)),
					_ => new()
				},
				_ => new()
			};
		}

		return quadrilateral.TopLeft.X != 0;
	}

	public static (int zoom, Quadrilateral quadrilateral) GetTimerPoints() {
		if (GameState.Current.FocusState == FocusState.Bomb) return (-2, new(new(939, 542), new(1010, 542), new(939, 583), new(1011, 583)));
		var (vx, vy) = GetVisualXY(GameState.Current.SelectedFace.SelectedSlot);
		return vy switch {
			-4 => vx switch {
				 -3 => (-2, new(new(1121,  993), new(1210,  993), new(1122, 1050), new(1212, 1050))),
				 -1 => (-2, new(new( 994,  993), new(1084,  993), new( 995, 1051), new(1085, 1050))),
				  1 => (-2, new(new( 868,  994), new( 958,  994), new( 867, 1051), new( 958, 1050))),
				  3 => (-2, new(new( 742,  995), new( 831,  995), new( 740, 1051), new( 831, 1051))),
				_ => throw new InvalidOperationException()
			},
			-3 => vx switch {
				 -4 => ( 0, new(new(1276, 1013), new(1400, 1013), new(1277, 1080), new(1402, 1080))),
				 -2 => ( 0, new(new(1097, 1013), new(1224, 1013), new(1096, 1080), new(1225, 1080))),
				  0 => ( 0, new(new( 919, 1014), new(1046, 1014), new( 919, 1080), new(1048, 1080))),
				  2 => ( 0, new(new( 741, 1015), new( 868, 1014), new( 740, 1080), new( 869, 1080))),
				  4 => ( 0, new(new( 562, 1015), new( 690, 1015), new( 560, 1080), new( 689, 1080))),
				_ => throw new InvalidOperationException()
			},
			-2 => vx switch {
				 -5 => ( 2, new(new(1528,  974), new(1708,  974), new(1532, 1080), new(1714, 1080))),
				 -3 => ( 2, new(new(1277,  975), new(1458,  974), new(1280, 1080), new(1463, 1080))),
				 -1 => ( 2, new(new(1027,  975), new(1208,  975), new(1028, 1080), new(1210, 1080))),
				  1 => ( 2, new(new( 777,  975), new( 956,  975), new( 775, 1080), new( 958, 1080))),
				  3 => ( 2, new(new( 526,  976), new( 706,  976), new( 522, 1080), new( 705, 1080))),
				  5 => ( 2, new(new( 275,  976), new( 454,  976), new( 268, 1080), new( 451, 1080))),
				_ => throw new InvalidOperationException()
			},
			-1 => vx switch {
				 -6 => ( 2, new(new(1636,  738), new(1810,  738), new(1644,  842), new(1821,  842))),
				 -4 => ( 4, new(new(1582,  825), new(1837,  825), new(1589,  976), new(1846,  976))),
				 -2 => ( 4, new(new(1230,  826), new(1483,  826), new(1234,  976), new(1491,  976))),
				  0 => ( 4, new(new( 878,  826), new(1133,  826), new( 877,  977), new(1135,  977))),
				  2 => ( 4, new(new( 525,  827), new( 778,  827), new( 520,  978), new( 779,  978))),
				  4 => ( 4, new(new( 169,  827), new( 425,  827), new( 163,  978), new( 423,  978))),
				  6 => ( 2, new(new( 167,  740), new( 343,  740), new( 159,  844), new( 338,  844))),
				_ => throw new InvalidOperationException()
			},
			0 => vx switch {
				 -6 => ( 2, new(new(1622,  512), new(1793,  512), new(1628,  611), new(1803,  611))),
				 -4 => ( 4, new(new(1569,  500), new(1815,  500), new(1576,  644), new(1827,  644))),
				 -2 => ( 4, new(new(1225,  500), new(1471,  500), new(1228,  644), new(1480,  644))),
				  2 => ( 4, new(new( 534,  500), new( 782,  500), new( 530,  645), new( 782,  645))),
				  4 => ( 4, new(new( 188,  500), new( 436,  500), new( 180,  645), new( 433,  645))),
				  6 => ( 2, new(new( 183,  513), new( 355,  513), new( 175,  613), new( 350,  613))),
				_ => throw new InvalidOperationException()
			},
			1 => vx switch {
				 -6 => ( 2, new(new(1608,  295), new(1776,  295), new(1615,  390), new(1785,  390))),
				 -4 => ( 4, new(new(1556,  187), new(1798,  187), new(1563,  325), new(1809,  325))),
				 -2 => ( 4, new(new(1219,  187), new(1462,  187), new(1221,  325), new(1469,  325))),
				  0 => ( 4, new(new( 880,  187), new(1125,  187), new( 880,  325), new(1128,  325))),
				  2 => ( 4, new(new( 541,  187), new( 785,  187), new( 537,  325), new( 786,  325))),
				  4 => ( 4, new(new( 203,  187), new( 448,  187), new( 195,  325), new( 444,  325))),
				  6 => ( 2, new(new( 198,  295), new( 368,  295), new( 191,  391), new( 363,  391))),
				_ => throw new InvalidOperationException()
			},
			2 => vx switch {
				 -5 => ( 0, new(new(1321,  225), new(1435,  225), new(1325,  288), new(1441,  288))),
				 -3 => ( 2, new(new(1251,   87), new(1415,   87), new(1254,  178), new(1422,  178))),
				 -1 => ( 2, new(new(1021,   86), new(1188,   86), new(1022,  178), new(1190,  178))),
				  1 => ( 2, new(new( 790,   86), new( 956,   86), new( 789,  178), new( 957,  178))),
				  3 => ( 2, new(new( 560,   86), new( 725,   86), new( 556,  177), new( 724,  177))),
				  5 => ( 2, new(new( 329,   86), new( 494,   86), new( 323,  177), new( 491,  177))),
				_ => throw new InvalidOperationException()
			},
			3 => vx switch {
				 -4 => ( 0, new(new(1236,   86), new(1348,   86), new(1239,  147), new(1353,  147))),
				 -2 => ( 0, new(new(1080,   86), new(1192,   86), new(1081,  146), new(1194,  146))),
				  0 => ( 0, new(new( 923,   86), new(1035,   86), new( 923,  146), new(1036,  146))),
				  2 => ( 0, new(new( 766,   86), new( 878,   86), new( 764,  146), new( 878,  146))),
				  4 => ( 0, new(new( 607,   85), new( 722,   85), new( 605,  146), new( 720,  146))),
				_ => throw new InvalidOperationException()
			},
			4 => vx switch {
				 -3 => (-2, new(new(1095,  132), new(1171,  132), new(1096,  172), new(1173,  172))),
				 -1 => (-2, new(new( 988,  132), new(1064,  132), new( 988,  172), new(1065,  172))),
				  1 => (-2, new(new( 881,  132), new( 957,  132), new( 880,  171), new( 958,  171))),
				  3 => (-2, new(new( 774,  131), new( 850,  131), new( 773,  171), new( 850,  171))),
				_ => throw new InvalidOperationException()
			},
			_ => throw new InvalidOperationException()
		};
	}
}
