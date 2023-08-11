using System;

namespace BombDefuserConnectorApi;
/// <summary>Identifies a specific component slot in the game.</summary>
public struct Slot : IEquatable<Slot> {
	/// <summary>The number of the bomb. 0 is the first bomb set up in the current game.</summary>
	public int Bomb;
	/// <summary>The number of the face on the bomb. 0 is the front face (the face that starts the game facing up).</summary>
	public int Face;
	/// <summary>The number of slots right of the left-most slot.</summary>
	public int X;
	/// <summary>The number of rows below the top row.</summary>
	public int Y;

	public Slot(int bomb, int face, int x, int y) {
		this.Bomb = bomb;
		this.Face = face;
		this.X = x;
		this.Y = y;
	}

	public static bool operator ==(Slot v1, Slot v2) => v1.X == v2.X && v1.Y == v2.Y && v1.Face == v2.Face && v1.Bomb == v2.Bomb;
	public static bool operator !=(Slot v1, Slot v2) => v1.X != v2.X || v1.Y != v2.Y || v1.Face != v2.Face || v1.Bomb != v2.Bomb;
	public readonly bool Equals(Slot other) => this == other;
	public override readonly bool Equals(object? other) => other is Slot slot && this == slot;

	public override readonly string ToString() => $"({this.Bomb}, {this.Face}, {this.X}, {this.Y})";

	public override readonly int GetHashCode() {
		var result = 17;
		result = result * 23 + this.X;
		result = result * 23 + this.Y;
		result = result * 23 + this.Face;
		result = result * 23 + this.Bomb;
		return result;
	}
}
