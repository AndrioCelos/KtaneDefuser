using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Aiml.Media;
using Aiml.Tags;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace BombDefuserConnector.Components;
public class Semaphore : ComponentReader<Semaphore.ReadData> {
	public override string Name => "Semaphore";
	protected internal override bool UsesNeedyFrame => false;

	protected internal override float IsModulePresent(Image<Rgba32> image) {
		var points = GetDisplayPoints(image, LightsState.On);
		return Math.Abs(points.TopRight.Y - points.TopLeft.Y) <= 5 && Math.Abs(points.BottomRight.Y - points.BottomLeft.Y) <= 5
			&& Math.Abs(points.BottomLeft.X - points.TopLeft.X) <= 5 && Math.Abs(points.BottomRight.X - points.TopRight.X) <= 5
			? 0.75f : 0;
	}

	protected internal override ReadData Process(Image<Rgba32> image, LightsState lightsState, ref Image<Rgba32>? debugImage) {
		var points = GetDisplayPoints(image, lightsState);
		if (debugImage is not null) ImageUtils.DebugDrawPoints(debugImage, points);

		var displayBitmap = ImageUtils.PerspectiveUndistort(image, points, InterpolationMode.NearestNeighbour, new(180, 142));
		debugImage?.Mutate(c => c.DrawImage(displayBitmap, 1));

		Predicate<Rgba32> isWhite = lightsState switch {
			LightsState.Buzz => c => c.R >= 32 && c.B >= 32,
			LightsState.Off => c => c.R >= 7 && c.B >= 7,
			LightsState.Emergency => c => c.R >= 160 && c.B >= 160,
			_ => c => c.G >= 224
		};

		Predicate<Rgba32> isBlue = lightsState switch {
			LightsState.Buzz => c => c.R < 20 && c.G is >= 20 and < 30 && c.B >= 30,
			LightsState.Off => c => c.R < 2 && c.B >= 8,
			LightsState.Emergency => c => c.R < 16 && c.B >= 160,
			_ => c => c.R < 64 && c.B >= 192
		};

		// Find the white and blue triangles that are parts of a flag.
		// NOTE: This assumes the flags will never overlap. They shouldn't, because the space signal isn't used.
		// For each triangle, we find its centre of mass and its axis-aligned bounding box. The box is used to calculate the centre, and make sure it won't be counted again.
		List<(Point centre, Rectangle boundingBox)> whiteShapes = [], blueShapes = [];

		displayBitmap.ProcessPixelRows(p => {
			for (var y = 0; y < p.Height; y++) {
				var r = p.GetRowSpan(y);
				for (var x = 0; x < p.Width; x++) {
					if (isWhite(r[x]) && !whiteShapes.Any(s => s.boundingBox.Contains(x, y)))
						whiteShapes.Add(FindTriangle(p, x, y, isWhite));
					if (isBlue(r[x]) && !blueShapes.Any(s => s.boundingBox.Contains(x, y)))
						blueShapes.Add(FindTriangle(p, x, y, isBlue));
				}
			}
		});

		if (debugImage is not null) {
			foreach (var (centre, rect) in whiteShapes) {
				debugImage[centre.X, centre.Y] = Color.Magenta;
				debugImage.Mutate(c => c.Draw(new SolidPen(Color.Magenta), rect));
			}
			foreach (var (centre, rect) in blueShapes) {
				debugImage[centre.X, centre.Y] = Color.Blue;
				debugImage.Mutate(c => c.Draw(new SolidPen(Color.Blue), rect));
			}
		}

		blueShapes.RemoveAll(s => s.boundingBox.Width < 8 || s.boundingBox.Height < 8);
		if (blueShapes.Count != 2) throw new ArgumentException("Could not find the flags.");

		// Determine the flag positions from the triangle centres and bounding boxes.
		// Note: Flags may be mirrored in the case of the left flag held at up-right (W, X) or down-right (Z), or the right flag being held at down-left (H, 8) or up-left (O).
		Direction leftFlag = 0, rightFlag = 0;

		foreach (var blueShape in blueShapes) {
			var whiteShape = whiteShapes.MinBy(s => Math.Abs(s.centre.X - blueShape.centre.X) + Math.Abs(s.centre.Y - blueShape.centre.Y));

			if (Math.Abs(whiteShape.boundingBox.X - blueShape.boundingBox.X) < blueShape.boundingBox.Width / 2 && Math.Abs(whiteShape.boundingBox.Y - blueShape.boundingBox.Y) < blueShape.boundingBox.Height / 2) {
				// Both triangles have similar bounding boxes, meaning the flag is being held in a cardinal direction.
				// Determine which flag it is and the direction by the relative positions of the centre points and the absolute position of the white shape.
				if (whiteShape.centre.X > blueShape.centre.X) {
					if (whiteShape.centre.Y > blueShape.centre.Y)
						leftFlag = Direction.Up;
					else if (whiteShape.centre.X >= 64)
						rightFlag = Direction.Down;
					else
						leftFlag = Direction.Left;
				} else {
					if (whiteShape.centre.Y > blueShape.centre.Y)
						rightFlag = Direction.Up;
					else if (whiteShape.centre.X >= 128)
						rightFlag = Direction.Right;
					else
						leftFlag = Direction.Down;
				}
			} else {
				// The triangles have different bounding boxes, meaning the flag is being held diagonally.
				int dx = whiteShape.centre.X - blueShape.centre.X, dy = whiteShape.centre.Y - blueShape.centre.Y;
				if (Math.Abs(dx) > Math.Abs(dy)) {
					if (dx > 0) {
						if (blueShape.centre.X >= 56) rightFlag = Direction.UpLeft;
						else leftFlag = Direction.UpLeft;
					} else {
						if (blueShape.centre.X >= 140) rightFlag = Direction.UpRight;
						else leftFlag = Direction.UpRight;

					}
				} else {
					if (dy > 0) {
						// Shouldn't happen.
						if (blueShape.centre.X >= 90) leftFlag = Direction.UpRight;
						else rightFlag = Direction.UpLeft;
					} else {
						if (blueShape.centre.X >= 110) rightFlag = Direction.DownRight;
						else if (blueShape.centre.X >= 68) rightFlag = Direction.DownLeft;
						else leftFlag = Direction.DownLeft;
					}
				}
			}
		}

		return new(leftFlag, rightFlag);
	}

	/// <summary>Determines the centre of mass and minimal axis-aligned bounding box of the triangle containing the specified point.</summary>
	private static (Point centre, Rectangle boundingBox) FindTriangle(PixelAccessor<Rgba32> pixelAccessor, int x, int y, Predicate<Rgba32> predicate) {
		var clearEdges = 0;
		var rectangle = new Rectangle(x, y, 1, 1);
		var edge = 0;
		do {
			var found = false;
			if (edge < 2) {
				var x2 = edge == 0 ? rectangle.Left : rectangle.Right;
				for (var y2 = rectangle.Top; y2 < rectangle.Bottom; y2++) {
					if (predicate(pixelAccessor.GetRowSpan(y2)[x2])) {
						found = true;
						break;
					}
				}
				if (found) {
					if (edge == 0) rectangle.X--;
					rectangle.Width++;
				}
			} else {
				var r = pixelAccessor.GetRowSpan(edge == 0 ? rectangle.Top : rectangle.Bottom);
				for (var x2 = rectangle.Left; x2 < rectangle.Right; x2++) {
					if (predicate(r[x2])) {
						found = true;
						break;
					}
				}
				if (found) {
					if (edge == 0) rectangle.Y--;
					rectangle.Height++;
				}
			}

			if (found)
				clearEdges = 0;
			else
				clearEdges++;

			edge++;
			if (edge >= 4) edge = 0;
		} while (clearEdges < 4);

		// Approximate the triangle centre.
		int sx = 0, sy = 0, count = 0;
		for (var y2 = rectangle.Top; y2 < rectangle.Bottom; y2++) {
			var r = pixelAccessor.GetRowSpan(y2);
			for (var x2 = rectangle.Left; x2 < rectangle.Right; x2++) {
				if (predicate(r[x2])) {
					sx += x2;
					sy += y2;
					count++;
				}
			}
		}

		return (new(sx / count, sy / count), rectangle);
	}

	private static Quadrilateral GetDisplayPoints(Image<Rgba32> image, LightsState lightsState)
		=> ImageUtils.FindCorners(image, new(12, 48, 200, 180), lightsState switch {
			LightsState.Buzz => c => HsvColor.FromColor(c) is var hsv && hsv.S < 0.25f && hsv.V < 0.05f,
			LightsState.Off => c => c.R < 2 && c.G < 2 && c.B < 2,
			LightsState.Emergency => c => HsvColor.FromColor(c) is var hsv && hsv.H < 15 && hsv.S < 0.6f && hsv.V < 0.2f,
			_ => c => HsvColor.FromColor(c) is var hsv && hsv.V < 0.15f
		}, 12);

	public enum Direction {
		Down,
		DownLeft,
		Left,
		UpLeft,
		Up,
		UpRight,
		Right,
		DownRight
	}

	public record ReadData(Direction LeftFlag, Direction RightFlag);
}
