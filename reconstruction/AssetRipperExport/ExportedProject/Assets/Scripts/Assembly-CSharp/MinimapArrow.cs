using UnityEngine;
using Assets.Scripts.Actors.Player;

public class MinimapArrow : MonoBehaviour
{
	public Transform target;

	public MeshRenderer meshRenderer;

	private void Update()
	{
		UpdateBossArrow();
	}

	private void UpdateBossArrow()
	{
		var player = MyPlayer.Instance;
		if (player == null || player.minimapCamera == null || target == null || meshRenderer == null) return;
		Vector3 direction = target.position - player.transform.position;
		direction.y = 0f;
		float threshold = player.minimapCamera.orthographicSize * 0.95f;
		float distance = direction.magnitude;
		if (IsVisible())
		{
			if (distance < threshold) { meshRenderer.enabled = false; return; }
		}
		else
		{
			if (distance > threshold) meshRenderer.enabled = true;
			return;
		}
		float angle = 90f - Mathf.Atan2(direction.z, direction.x) * Mathf.Rad2Deg;
		transform.rotation = Quaternion.Euler(0f, angle, 0f);
		transform.position = player.minimapCamera.transform.position + direction.normalized * (player.minimapCamera.orthographicSize * 0.8f) + Vector3.down * 20f;
	}

	private bool IsVisible()
	{
		return meshRenderer.enabled;
	}
}
