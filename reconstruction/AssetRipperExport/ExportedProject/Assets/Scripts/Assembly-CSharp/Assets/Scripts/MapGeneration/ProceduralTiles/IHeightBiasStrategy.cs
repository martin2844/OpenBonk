namespace Assets.Scripts.MapGeneration.ProceduralTiles
{
	public interface IHeightBiasStrategy
	{
		float CalculateBias(int x, int y, int size, float outerBiasStrength, float strictness);
	}
}
