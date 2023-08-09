namespace BombDefuserConnectorApi;
public struct Slot {
	public int Bomb;
	public int Face;
	public int X;
	public int Y;

	public Slot(int bomb, int face, int x, int y) {
		this.Bomb = bomb;
		this.Face = face;
		this.X = x;
		this.Y = y;
	}

	public override readonly string ToString() => $"({this.Bomb}, {this.Face}, {this.X}, {this.Y})";
}
