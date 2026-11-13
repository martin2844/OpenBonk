using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Game.Combat.EnemySpecialAttacks.Implementations;
using Assets.Scripts.Objects.Pooling;
using Assets.Scripts.Utility;
using UnityEngine;
using Assets.Scripts.Actors.Enemies;

namespace Assets.Scripts.Game.Combat.EnemySpecialAttacks
{
	public class SpecialAttackController
	{
		private static bool enabled = true;

		private HashSet<EnemySpecialAttack> attacks;

		private Dictionary<EnemySpecialAttack, float> cooldowns;

		private float updateRate;

		private float nextCheckTime;

		private Enemy enemy;

		private bool isAttacking;

		private float attackOverAtTime;

		private EnemySpecialAttack currentAttack;

		public SpecialAttackController(Enemy enemy)
		{
			this.enemy = enemy;
			updateRate = 1f;
			nextCheckTime = MyTime.time + 3f + (float)global::Utility.MyRandom.random.NextDouble() * updateRate + (float)global::Utility.MyRandom.random.NextDouble() * 5f;
			attacks = new HashSet<EnemySpecialAttack>();
			cooldowns = new Dictionary<EnemySpecialAttack, float>();
			foreach (EnemySpecialAttack attack in enemy.enemyData.specialAttacks)
				if (attacks.Add(attack)) cooldowns.Add(attack, MyTime.time + attack.initialCooldown);
		}

		public void Tick()
		{
			if (!enabled || GameManager.Instance.IsTimeFreeze()) return;
			if (isAttacking)
			{
				if (MyTime.time > attackOverAtTime) FinishAttack();
				return;
			}
			if (enemy.IsTeleporting() || nextCheckTime > MyTime.time || enemy.state != EEnemyState.Default) return;
			nextCheckTime = MyTime.time + updateRate;
			foreach (EnemySpecialAttack attack in attacks.OrderBy(a => a.priority).ThenBy(_ => global::Utility.MyRandom.random.Next()))
			{
				if ((Application.isEditor && !attack.isEnabled) || attack.attackPrefab == null) continue;
				if (attack.attackPrefab.GetComponent<EnemySpecialAttackPrefab>() == null) continue;
				if (cooldowns[attack] > MyTime.time || enemy.enemyMovement.distanceToTarget > attack.triggerDistance) continue;
				UseSpecialAttack(attack);
				break;
			}
		}

		private void UseSpecialAttack(EnemySpecialAttack attack)
		{
			GameObject obj = PoolManager.Instance.GetEnemyAttack(attack);
			if (obj == null) return;
			obj.GetComponent<EnemySpecialAttackPrefab>().Set(attack, enemy);
			isAttacking = true;
			currentAttack = attack;
			enemy.StartSpecialAttack();
			attackOverAtTime = MyTime.time + attack.attackChargeTime + attack.endLag;
			float cooldown = attack.attackCooldownMax > attack.attackCooldown ? Random.Range(attack.attackCooldown, attack.attackCooldownMax) : attack.attackCooldown;
			cooldowns[attack] = MyTime.time + cooldown;
		}

		private void FinishAttack()
		{
			isAttacking = false;
			enemy.EndSpecialAttack();
			nextCheckTime = MyTime.time + currentAttack.nextSpecialAttackCooldown;
		}
	}
}
