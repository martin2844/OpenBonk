using UnityEngine;
using Assets.Scripts.Objects.Pooling;

public class PickupEffect : MonoBehaviour
{
	private float time = 0.3f;

	private void OnEnable()
	{
		var player = GameManager.Instance.GetPlayer();
		if (player != null) transform.SetParent(player.transform);
		Invoke(nameof(DisableSelf), time);
	}

	private void DisableSelf()
	{
		if (!gameObject.activeInHierarchy) return;
		transform.SetParent(null);
		gameObject.SetActive(false);
		PoolManager.Instance.pickupeffectPool.Release(gameObject);
	}
}
