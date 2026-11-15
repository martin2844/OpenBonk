using UnityEngine;
using Assets.Scripts.Actors;
using Assets.Scripts.Managers;

public class ExplodeEnemyTrigger : MonoBehaviour
{
	public float radius;

	private void Start()
	{
		foreach (Collider collider in Physics.OverlapSphere(transform.position, radius, GameManager.Instance.whatIsEnemy))
		{
			if (collider.gameObject.layer != LayerMask.NameToLayer("Enemy") ||
				!EnemyManager.Instance.GetEnemy(collider, out var enemy)) continue;
			Vector3 delta = collider.transform.position - transform.position;
			float distanceFactor = (delta.magnitude - 1f) / radius;
			var damage = new DamageContainer(0f, "ExplodeEnemyTrigger (Bug probably)");
			damage.direction = delta.normalized + Vector3.up * 0.4f;
			damage.knockback = distanceFactor < 0.3f ? 42f : (1f - Mathf.Min(1f, distanceFactor)) * 60f;
			enemy.enemyMovement.Knockback(damage);
		}
	}
}
