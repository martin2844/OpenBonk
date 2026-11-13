using System.Collections.Generic;

public class Tile
{
	public int X { get; }

	public int Y { get; }

	public bool IsAvailable { get; set; }

	public bool IsVisited { get; set; }

	public List<Tile> Neighbors { get; }

	public Tile(int x, int y)
	{
	}
}
