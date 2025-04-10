using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using SixLabors.ImageSharp;

namespace KtaneDefuserConnector;
public struct Quadrilateral : IEquatable<Quadrilateral> {
	public Point TopLeft;
	public Point TopRight;
	public Point BottomLeft;
	public Point BottomRight;

	public Quadrilateral(Point topLeft, Point topRight, Point bottomLeft, Point bottomRight) {
		TopLeft = topLeft;
		TopRight = topRight;
		BottomLeft = bottomLeft;
		BottomRight = bottomRight;
	}
	public Quadrilateral(int topLeftX, int topLeftY, int topRightX, int topRightY, int bottomLeftX, int bottomLeftY, int bottomRightX, int bottomRightY) {
		TopLeft = new(topLeftX, topLeftY);
		TopRight = new(topRightX, topRightY);
		BottomLeft = new(bottomLeftX, bottomLeftY);
		BottomRight = new(bottomRightX, bottomRightY);
	}
	public Quadrilateral(IReadOnlyList<Point> points) {
		ArgumentNullException.ThrowIfNull(points);
		if (points.Count != 4) throw new ArgumentException("Points list must have length 4.", nameof(points));
		TopLeft = points[0];
		TopRight = points[1];
		BottomLeft = points[2];
		BottomRight = points[3];
	}
	public Quadrilateral(IEnumerable<Point> points) {
		ArgumentNullException.ThrowIfNull(points);
		var enumerator = points.GetEnumerator();
		TopLeft = enumerator.MoveNext() ? enumerator.Current : throw new ArgumentException("Points list must have length 4.", nameof(points));
		TopRight = enumerator.MoveNext() ? enumerator.Current : throw new ArgumentException("Points list must have length 4.", nameof(points));
		BottomLeft = enumerator.MoveNext() ? enumerator.Current : throw new ArgumentException("Points list must have length 4.", nameof(points));
		BottomRight = enumerator.MoveNext() ? enumerator.Current : throw new ArgumentException("Points list must have length 4.", nameof(points));
		if (enumerator.MoveNext()) throw new ArgumentException("Points list must have length 4.", nameof(points));
	}

	public Point this[int index] {
		readonly get => index switch { 0 => TopLeft, 1 => TopRight, 2 => BottomLeft, 3 => BottomRight, _ => throw new IndexOutOfRangeException() };
		set {
			switch (index) {
				case 0: TopLeft = value; break;
				case 1: TopRight = value; break;
				case 2: BottomLeft = value; break;
				case 3: BottomRight = value; break;
				default: throw new IndexOutOfRangeException();
			}
		}
	}

	public bool Equals(Quadrilateral other) => TopLeft.Equals(other.TopLeft) && TopRight.Equals(other.TopRight) && BottomLeft.Equals(other.BottomLeft) && BottomRight.Equals(other.BottomRight);

	public override bool Equals(object? obj) => obj is Quadrilateral other && Equals(other);

	public override int GetHashCode() => HashCode.Combine(TopLeft, TopRight, BottomLeft, BottomRight);

	public static bool operator ==(Quadrilateral left, Quadrilateral right) => left.Equals(right);

	public static bool operator !=(Quadrilateral left, Quadrilateral right) => !left.Equals(right);

	public static implicit operator Quadrilateral(Rectangle rect) => new(rect.Left, rect.Top, rect.Right, rect.Top, rect.Left, rect.Bottom, rect.Right, rect.Bottom);
}
