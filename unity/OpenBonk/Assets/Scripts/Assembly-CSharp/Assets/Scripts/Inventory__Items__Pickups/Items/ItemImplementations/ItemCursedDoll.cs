using System.Collections.Generic;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using UnityEngine.Localization;
using UnityEngine;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat;
using Assets.Scripts.Managers;
using Assets.Scripts.Objects.Pooling;
using Assets.Scripts.Utility;

namespace Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations
{
	public class ItemCursedDoll : ItemBase
	{
		private int maxNumCursedEnemies;

		private float damageMaxHpPercentage = .3f;

		private int enemiesCursedPerDoll = 2;

		private int maxNumCursesPerCheck = 5;

		private DamageContainer reuseDc;

		private string damageSource;

		private HashSet<Enemy> cursedEnemies;

		private float attackCooldown = 1f;

		private float nextAttackTime;

		protected override void OnInitOrAmountChanged()
		{
			maxNumCursedEnemies = amount * enemiesCursedPerDoll;
		}

		public override void Tick()
		{
			if (MyTime.time < nextAttackTime) return;
			nextAttackTime = MyTime.time + attackCooldown;
			if (cursedEnemies.Count < maxNumCursedEnemies && EnemyManager.Instance != null)
			{
				var enemies = EnemyManager.Instance.enemies.Values;
				if (enemies.Count == 0) return;
				int added = 0;
				foreach (Enemy enemy in enemies)
				{
					if (enemy == null || enemy.IsDead() || enemy.IsTeleporting() || cursedEnemies.Contains(enemy)) continue;
					cursedEnemies.Add(enemy);
					if (++added >= maxNumCursesPerCheck || cursedEnemies.Count >= maxNumCursedEnemies) break;
				}
			}
			foreach (Enemy enemy in cursedEnemies)
			{
				if (enemy == null || enemy.IsDead()) continue;
				GameObject effect = PoolManager.Instance != null && PoolManager.Instance.cursedHitPool != null ? PoolManager.Instance.cursedHitPool.Get() : null;
				if (effect != null) effect.transform.position = enemy.GetHeadPosition();
				reuseDc.damage = enemy.IsBoss() ? MyPlayer.Instance.GetBaseDamage() * .7f : damageMaxHpPercentage * enemy.maxHp;
				reuseDc.enemy = enemy;
				reuseDc.damageEffect = EDamageEffect.Cursed;
				enemy.DamageFromPlayerOther(reuseDc);
			}
			cursedEnemies.RemoveWhere(enemy => enemy == null || enemy.IsDead());
		}

		private void OnEnemyDied(Enemy enemy)
		{
			cursedEnemies.Remove(enemy);
		}

		public override void Init()
		{
			Enemy.A_EnemyReleasedFromPool += OnEnemyDied;
		}

		public override void Cleanup()
		{
			Enemy.A_EnemyReleasedFromPool -= OnEnemyDied;
		}

		public ItemCursedDoll(ItemInventory itemInventoryRef)
			: base(itemInventoryRef)
		{
			damageSource = EItem.CursedDoll.ToString();
			reuseDc = new DamageContainer(1f, damageSource);
			cursedEnemies = new HashSet<Enemy>();
		}

		public override void PreAttack(DamageContainer dc, StatComponents itemAttackModifier)
		{
		}

		public override bool HasPreAttackProc()
		{
			return false;
		}

		public override void ProcOnHitEffects(DamageContainer dc)
		{
		}

		public override bool HasOnHitEffectProc()
		{
			return false;
		}

		public override string GetDescription(LocalizedString localizedString)
		{
			return localizedString.GetLocalizedString(new Dictionary<string, object> { ["value1"] = string.Format("{0}%", damageMaxHpPercentage * 100f) });
		}
	}
}
