namespace BombDefuserConnectorApi;
/// <summary>Identifies a specific component slot in the game.</summary>
public struct Slot {
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

	public override readonly string ToString() => $"({this.Bomb}, {this.Face}, {this.X}, {this.Y})";
}
