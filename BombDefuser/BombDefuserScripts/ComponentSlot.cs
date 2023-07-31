namespace BombDefuserScripts;
public struct ComponentSlot {
	public int Face;
	public int X;
	public int Y;

	public ComponentSlot(int face, int x, int y) {
		this.Face = face;
		this.X = x;
		this.Y = y;
	}

	public override readonly string ToString() => $"{Face} {X} {Y}";
}
