using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Managers;
using UnityEngine;

public class CombatExplosion : MonoBehaviour
{
	public float radius;
	[Header("Player")]
	public float playerDamage;
	public float playerKnockback;
	[Header("Enemy")]
	public float enemyKnockback;

	private void Start()
	{
		Vector3 position = transform.position;
		if ((playerDamage > 0f || playerKnockback > 0f) && Physics.CheckSphere(position, radius, GameManager.Instance.whatIsPlayer))
		{
			MyPlayer player = MyPlayer.Instance;
			Vector3 direction = (player.transform.position - position).normalized;
			if (playerDamage > 0f) player.inventory.playerHealth.DamagePlayerExternal(playerDamage, 0f, direction);
			if (playerKnockback > 0f) player.playerMovement.PushPlayer((direction + Vector3.up * 0.75f).normalized * playerKnockback);
		}
		if (enemyKnockback <= 0f) return;
		foreach (Collider collider in Physics.OverlapSphere(position, radius, GameManager.Instance.whatIsEnemy))
		{
			if (!EnemyManager.Instance.GetEnemy(collider, out Enemy enemy)) continue;
			Vector3 offset = collider.transform.position - position;
			float distanceFraction = (offset.magnitude - 1f) / radius;
			float factor = distanceFraction < 0.3f ? 0.7f : 1f - Mathf.Min(1f, distanceFraction);
			var dc = new DamageContainer(0f, DamageContainer.unknownDamageSource);
			dc.direction = offset.normalized + Vector3.up * 0.4f;
			dc.knockback = factor * enemyKnockback;
			enemy.enemyMovement.Knockback(dc);
		}
	}

	private void OnDrawGizmosSelected() { }
}
