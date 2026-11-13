using UnityEngine;
using Assets.Scripts.Utility;
using Assets.Scripts.Objects.Pooling;

namespace Assets.Scripts.Game.Combat.EnemySpecialAttacks
{
	public class EnemyAttackEffectPrefab : MonoBehaviour
	{
		public EEnemyAttack eAttack;

		public float aliveTime;

		private float returnTime;

		private void OnEnable()
		{
			returnTime = MyTime.time + aliveTime;
		}

		private void Update()
		{
			if (MyTime.time > returnTime) PoolManager.Instance.ReturnEnemyAttackFx(this);
		}
	}
}
