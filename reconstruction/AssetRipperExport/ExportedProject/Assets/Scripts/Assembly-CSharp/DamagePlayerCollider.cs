using UnityEngine;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Utility;

public class DamagePlayerCollider : MonoBehaviour
{
	public float knockbackForce = 10f;

	public float damage = 2f;

	public float refreshTime = .5f;

	private float readyAtTime;

	private void OnCollisionEnter(Collision collision)
	{
		if (collision.gameObject.layer != LayerMask.NameToLayer("Player") || readyAtTime > MyTime.time) return;
		var health = MyPlayer.Instance != null && MyPlayer.Instance.inventory != null ? MyPlayer.Instance.inventory.playerHealth : null;
		if (health == null) return;
		Vector3 direction = (collision.transform.position - transform.position + Vector3.up * .25f).normalized;
		health.DamagePlayerExternal(damage, knockbackForce, direction, false, "Cactus");
		readyAtTime = MyTime.time + refreshTime;
	}
}
