using UnityEngine;

public class TileStairs : MonoBehaviour
{
	public Renderer renderer;

	public void Set(StageData stageData)
	{
		renderer.sharedMaterial = stageData.m_stairs;
	}
}
