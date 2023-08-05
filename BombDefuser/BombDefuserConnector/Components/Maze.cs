using System;
using BombDefuserConnector.DataTypes;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace BombDefuserConnector.Components;
public class Maze : ComponentReader<Maze.ReadData> {
	public override string Name => "Maze";
	protected internal override bool UsesNeedyFrame => false;

	protected internal override float IsModulePresent(Image<Rgb24> image) {
		// Maze: look for the display
		var count = 0;
		for (var y = 60; y < 208; y++) {
			for (var x = 40; x < 180; x++) {
				var color = image[x, y];
				if (color.R >= 128) count += ImageUtils.ColorProximity(color, 214, 0, refB: 0, 40);
				else {
					count += ImageUtils.ColorProximity(color, 5, 12, 33, 24, 70, 90, 40);
				}
			}
		}

		return count / 828800f;
	}

	protected internal override ReadData Process(Image<Rgb24> image, ref Image<Rgb24>? debugImage) {
		GridCell? start = null, goal = null, circle1 = null, circle2 = null;
		image.ProcessPixelRows(a => {
			for (var y = 0; y < 6; y++) {
				var row = a.GetRowSpan(76 + 23 * y);
				for (var x = 0; x < 6; x++) {
					var p = row[60 + 23 * x];
					if (p.R >= 128) {
						if (p.G >= 128) {
							if (start is not null) throw new ArgumentException("Found more than one start location.");
							start = new(x, y);
						} else {
							if (goal is not null) throw new ArgumentException("Found more than one goal location.");
							goal = new(x, y);
						}
					}
					var foundLeft = false;
					for (var dx = 0; dx < 8; dx++) {
						if (row[44 + 23 * x + dx].G >= 96) {
							foundLeft = true;
							break;
						}
					}
					if (!foundLeft) continue;
					for (var dx = 0; dx < 8; dx++) {
						if (row[64 + 23 * x + dx].G >= 96) {
							if (circle1 is null)
								circle1 = new(x, y);
							else if (circle2 is null)
								circle2 = new(x, y);
							else
								throw new ArgumentException("Found more than two circle locations.");
							break;
						}
					}
				}
			}
		});
		return new(
			start ?? throw new ArgumentException("Could not find the start location."),
			goal ?? throw new ArgumentException("Could not find the goal location."),
			circle1 ?? throw new ArgumentException("Could not find the first circle location."),
			circle2
		);
	}

	public record ReadData(GridCell Start, GridCell Goal, GridCell Circle1, GridCell? Circle2);
}
