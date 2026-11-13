using UnityEngine;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Objects.Pooling;
using Assets.Scripts.Utility;

public class Borgar : MonoBehaviour
{
	private int flatHeal;

	private float ratioHeal;

	private float timeoutAtTime;

	private float timeoutTime = 30f;

	public void Set(int flatHeal, float ratioHeal)
	{
		this.flatHeal = flatHeal;
		this.ratioHeal = ratioHeal;
		timeoutAtTime = MyTime.time + timeoutTime;
	}

	private void FixedUpdate()
	{
	}

	private void OnCollisionEnter(Collision collision)
	{
		if (collision.gameObject.layer != LayerMask.NameToLayer("Player")) return;
		Collect();
	}

	private void Collect()
	{
		var health = MyPlayer.Instance.inventory.playerHealth;
		health.Heal((int)Mathf.Max(flatHeal, health.GetCombinedMaxHp() * ratioHeal));
		DestroyBorgar();
		var effectPool = PoolManager.Instance.eatPool;
		if (effectPool == null) return;
		var effect = effectPool.Get();
		if (effect == null) return;
		effect.transform.position = transform.position;
		effect.SetActive(true);
	}

	private void Update()
	{
		if (MyTime.time > timeoutAtTime) DestroyBorgar();
	}

	private void DestroyBorgar()
	{
		if (!gameObject.activeInHierarchy) return;
		gameObject.SetActive(false);
		PoolManager.Instance.borgorPool.Release(gameObject);
	}
}
