using System;
using System.Collections.Generic;
using SixLabors.ImageSharp;

namespace KtaneDefuserConnector;
public struct Quadrilateral {
	public Point TopLeft;
	public Point TopRight;
	public Point BottomLeft;
	public Point BottomRight;

	public Quadrilateral(Point topLeft, Point topRight, Point bottomLeft, Point bottomRight) {
		this.TopLeft = topLeft;
		this.TopRight = topRight;
		this.BottomLeft = bottomLeft;
		this.BottomRight = bottomRight;
	}
	public Quadrilateral(int topLeftX, int topLeftY, int topRightX, int topRightY, int bottomLeftX, int bottomLeftY, int bottomRightX, int bottomRightY) {
		this.TopLeft = new(topLeftX, topLeftY);
		this.TopRight = new(topRightX, topRightY);
		this.BottomLeft = new(bottomLeftX, bottomLeftY);
		this.BottomRight = new(bottomRightX, bottomRightY);
	}
	public Quadrilateral(IReadOnlyList<Point> points) {
		ArgumentNullException.ThrowIfNull(points);
		if (points.Count != 4) throw new ArgumentException("Points list must have length 4.", nameof(points));
		this.TopLeft = points[0];
		this.TopRight = points[1];
		this.BottomLeft = points[2];
		this.BottomRight = points[3];
	}
	public Quadrilateral(IEnumerable<Point> points) {
		ArgumentNullException.ThrowIfNull(points);
		var enumerator = points.GetEnumerator();
		this.TopLeft = enumerator.MoveNext() ? enumerator.Current : throw new ArgumentException("Points list must have length 4.", nameof(points));
		this.TopRight = enumerator.MoveNext() ? enumerator.Current : throw new ArgumentException("Points list must have length 4.", nameof(points));
		this.BottomLeft = enumerator.MoveNext() ? enumerator.Current : throw new ArgumentException("Points list must have length 4.", nameof(points));
		this.BottomRight = enumerator.MoveNext() ? enumerator.Current : throw new ArgumentException("Points list must have length 4.", nameof(points));
		if (enumerator.MoveNext()) throw new ArgumentException("Points list must have length 4.", nameof(points));
	}

	public Point this[int index] {
		readonly get => index switch { 0 => this.TopLeft, 1 => this.TopRight, 2 => this.BottomLeft, 3 => this.BottomRight, _ => throw new IndexOutOfRangeException() };
		set {
			switch (index) {
				case 0: this.TopLeft = value; break;
				case 1: this.TopRight = value; break;
				case 2: this.BottomLeft = value; break;
				case 3: this.BottomRight = value; break;
				default: throw new IndexOutOfRangeException();
			}
		}
	}

	public static implicit operator Quadrilateral(Rectangle rect) => new(rect.Left, rect.Top, rect.Right, rect.Top, rect.Left, rect.Bottom, rect.Right, rect.Bottom);
}
