using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using UnityEngine;
using Assets.Scripts.Objects.Pooling;
using Assets.Scripts.Utility;

namespace Assets.Scripts.Inventory__Items__Pickups.AbilitiesPassive.Extra
{
	public class BullseyeMarker : MonoBehaviour
	{
		private Enemy markedEnemy;

		private float doneAtTime;

		private void Awake()
		{
			Enemy.A_EnemyReleasedFromPool += OnEnemyReleasedFromPool;
			Enemy.A_EnemyDied += OnEnemyDied;
		}

		private void OnDestroy()
		{
			Enemy.A_EnemyReleasedFromPool -= OnEnemyReleasedFromPool;
			Enemy.A_EnemyDied -= OnEnemyDied;
		}

		public void Set(Enemy enemy, float duration)
		{
			gameObject.SetActive(true);
			markedEnemy = enemy;
			doneAtTime = MyTime.time + duration;
		}

		private void Update()
		{
			if (MyTime.time > doneAtTime) { Cleanup(); return; }
			transform.position = markedEnemy.GetCenterPosition();
			transform.rotation = Quaternion.LookRotation(PlayerCamera.Instance.transform.position - transform.position, Vector3.up) * Quaternion.AngleAxis(Time.time * 90f, Vector3.forward);
		}

		private void OnEnemyReleasedFromPool(Enemy enemy)
		{
			if (markedEnemy == enemy) Cleanup();
		}

		private void OnEnemyDied(Enemy enemy, DamageContainer deathSource)
		{
			if (markedEnemy == enemy) Cleanup();
		}

		private void Cleanup()
		{
			if (!gameObject.activeInHierarchy) return;
			gameObject.SetActive(false);
			PoolManager.Instance.bullseyePool.Release(gameObject);
		}
	}
}
