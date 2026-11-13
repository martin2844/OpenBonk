using UnityEngine;

[CreateAssetMenu]
public class MeshSettings : UpdatableData
{
	public const int numSupportedLODs = 5;

	public const int numSupportedChunkSizes = 9;

	public const int numSupportedFlatshadedChunkSizes = 3;

	public static readonly int[] supportedChunkSizes;

	public float meshScale;

	public bool useFlatShading;

	[Range(0f, 8f)]
	public int chunkSizeIndex;

	[Range(0f, 2f)]
	public int flatshadedChunkSizeIndex;

	public int numVertsPerLine => 0;

	public float meshWorldSize => 0f;
}
