﻿using System;
using KtaneDefuserConnector.DataTypes;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace KtaneDefuserConnector.Components;
public class Maze : ComponentReader<Maze.ReadData> {
	public override string Name => "Maze";

	private static bool IsMarking(Rgba32 pixel, LightsState lightsState) => lightsState switch {
		LightsState.Buzz => HsvColor.FromColor(pixel) is { H: <= 190, V: <= 0.25f },
		LightsState.Off => HsvColor.FromColor(pixel) is { H: <= 215, V: <= 0.1f },
		_ => pixel.G >= 85
	};

	protected internal override ReadData Process(Image<Rgba32> image, LightsState lightsState, ref Image<Rgba32>? debugImage) {
		GridCell? start = null, goal = null, circle1 = null, circle2 = null;
		var debugImage2 = debugImage;
		image.ProcessPixelRows(a => {
			for (var y = 0; y < 6; y++) {
				var row = a.GetRowSpan(74 + 23 * y);
				for (var x = 0; x < 6; x++) {
					var p = row[58 + 23 * x];
					if (debugImage2 is not null) debugImage2[x, y] = Color.Blue;
					if (p.R >= 128) {
						if (p.G >= 128) {
							if (debugImage2 is not null) debugImage2[x, y] = Color.White;
							if (start is not null) throw new ArgumentException("Found more than one start location.");
							start = new(x, y);
						} else {
							if (debugImage2 is not null) debugImage2[x, y] = Color.Red;
							if (goal is not null) throw new ArgumentException("Found more than one goal location.");
							goal = new(x, y);
						}
					}
					// Look left for a marking.
					var found = false;
					for (var dx = 0; dx < 8; dx++) {
						if (!IsMarking(row[58 - 16 + 23 * x + dx], lightsState)) continue;
						found = true;
						break;
					}
					if (!found) continue;
					// Look right for a marking.
					found = false;
					for (var dx = 0; dx < 8; dx++) {
						if (!IsMarking(row[58 + 8 + 23 * x + dx], lightsState)) continue;
						found = true;
						break;
					}
					if (!found) continue;
					// Look up for a marking.
					var row2 = a.GetRowSpan(65 + 23 * y);
					for (var dx = 0; dx < 8; dx++) {
						if (!IsMarking(row2[58 - 4 + 23 * x + dx], lightsState)) continue;
						if (debugImage2 is not null) debugImage2[x + 1, y] = Color.Green;
						if (circle1 is null)
							circle1 = new(x, y);
						else
							circle2 = circle2 is null ? new(x, y) : throw new ArgumentException("Found more than two circle locations.");
						break;
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
