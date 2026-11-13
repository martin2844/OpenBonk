using UnityEngine;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Spawning;
using Assets.Scripts.Managers;

public class WorldColliderTrigger : MonoBehaviour
{
	private float maxSlopeAngle = 45f;

	private void OnCollisionStay(Collision collision)
	{
		if (collision == null || collision.gameObject == null || collision.contactCount == 0) return;
		int layer = collision.gameObject.layer;
		int playerLayer = LayerMask.NameToLayer("Player");
		int enemyLayer = LayerMask.NameToLayer("Enemy");
		if (layer != playerLayer && layer != enemyLayer) return;
		foreach (ContactPoint contact in collision.contacts)
		{
			if (!IsFloor(contact.normal)) continue;
			if (layer == playerLayer)
			{
				var movement = MyPlayer.Instance != null ? MyPlayer.Instance.playerMovement : null;
				if (movement == null || GameManager.Instance == null) return;
				Vector3 position = SpawnPositions.GetEnemySpawnPositionAroundPoint(movement.lastGroundedPosition, 0f, 20f, 50, true, 5f);
				if (position != Vector3.zero) movement.TeleportPlayerBackToBounds(position);
			}
			else if (EnemyManager.Instance != null && EnemyManager.Instance.GetEnemy(collision.collider, out var enemy) && enemy != null)
				enemy.TeleportToPlayer();
			return;
		}
	}

	private bool IsFloor(Vector3 v)
	{
		return Mathf.Abs(v.y + 1f) < 0.075f;
	}

	private bool IsCeiling(Vector3 v)
	{
		return Vector3.Angle(Vector3.up, v) < maxSlopeAngle;
	}
}
