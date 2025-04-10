namespace KtaneDefuserConnector.DataTypes;
public record struct GridCell(int X, int Y) {
	public override readonly string ToString() => $"{(char) ('A' + X)}{Y + 1}";
}
