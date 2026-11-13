using System;
using System.Collections.Generic;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.Utility;
using UnityEngine;

namespace Assets.Scripts.Game.Combat.EnemySpecialAttacks
{
	[Serializable]
	public class EnemySpecialAttack : IComparer<EnemySpecialAttack>
	{
		public bool isEnabled;

		public int priority;

		public string attackName;

		public GameObject attackPrefab;

		[Header("Attack Settings")]
		public float attackChargeTime;

		public float attackRadius;

		public float attackCooldown;

		public float attackCooldownMax;

		public float initialCooldown;

		public float duration;

		public float nextSpecialAttackCooldown;

		public float triggerDistance;

		public float endLag;

		public float damageMultiplier;

		public float GetDamage(Enemy enemy)
		{
			return EnemyStats.GetDamage(enemy) * damageMultiplier;
		}

		public int Compare(EnemySpecialAttack x, EnemySpecialAttack y)
		{
			if (ReferenceEquals(x, y)) return 0;
			if (x == null) return 1;
			if (y == null) return -1;
			int result = x.priority.CompareTo(y.priority);
			return result != 0 ? result : global::Utility.MyRandom.random.Next(-1, 2);
		}
	}
}
