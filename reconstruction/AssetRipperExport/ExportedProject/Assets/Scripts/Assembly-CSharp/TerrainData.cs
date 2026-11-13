using UnityEngine;

[CreateAssetMenu]
public class TerrainData : UpdateableData
{
	public float uniformScale;

	public bool useFalloff;

	public float heightMultiplier;

	public AnimationCurve heightCurve;

	public float minHeight => heightMultiplier * uniformScale * heightCurve.Evaluate(0f);

	public float maxHeight => heightMultiplier * uniformScale * heightCurve.Evaluate(1f);
}
