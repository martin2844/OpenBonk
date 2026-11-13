using System.Collections.Generic;
using UnityEngine;

public class ChainLightning : MonoBehaviour
{
	public LineRenderer lineRenderer;

	// A pooled effect is reused for different numbers of bounces. Cache small
	// exact-sized arrays per effect so normal reuse avoids List.ToArray garbage.
	private readonly Dictionary<int, Vector3[]> positionBuffers = new Dictionary<int, Vector3[]>();

	public void Set(List<Vector3> positions)
	{
		gameObject.SetActive(true);
		int count = positions.Count;
		lineRenderer.positionCount = count;
		Vector3[] buffer;
		if (count <= 64)
		{
			if (!positionBuffers.TryGetValue(count, out buffer))
			{
				buffer = new Vector3[count];
				positionBuffers.Add(count, buffer);
			}
		}
		else buffer = new Vector3[count];
		positions.CopyTo(buffer);
		lineRenderer.SetPositions(buffer);
		CancelInvoke(nameof(DisableSelf));
		Invoke(nameof(DisableSelf), 0.18f);
	}

	private void DisableSelf()
	{
		// The Android reconstruction uses a timed pool for this effect. Returning
		// it here prevents disabling it before ReturnToPool.Update can run.
		var returner = GetComponent<ReturnToPool>();
		if (returner != null && returner.pool != null) returner.pool.Release(gameObject);
		else gameObject.SetActive(false);
	}

	private void OnDisable()
	{
		CancelInvoke(nameof(DisableSelf));
	}
}
