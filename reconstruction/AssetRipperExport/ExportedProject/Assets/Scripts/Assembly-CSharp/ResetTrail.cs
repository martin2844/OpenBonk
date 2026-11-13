using UnityEngine;

public class ResetTrail : MonoBehaviour
{
	public TrailRenderer trailRenderer;

	private void OnDisable()
	{
		if (trailRenderer != null) trailRenderer.Clear();
	}

	private void OnValidate()
	{
		if (trailRenderer == null) trailRenderer = GetComponent<TrailRenderer>();
	}
}
