using System.Collections.Generic;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat;
using Assets.Scripts.Game.Combat.EnemyDebuffs;
using Assets.Scripts.Game.Combat.EnemyDebuffs.Implementations;
using Assets.Scripts.Objects.Pooling;
using UnityEngine;
using UnityEngine.Localization;

namespace Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations
{
	public class ItemSnek : ItemBase
	{
		private float poisonChancePerAmount = 4f;

		private float poisonBurstChancePerAmount = .05f;

		private float poisonChanceTotal;

		private float poisonBurstChanceTotal;

		public float damageRatio = .4f;

		public float damageRatioPerAmount = .4f;

		private int procsSinceLastTick;

		private HashSet<Enemy> queuedExplosionEnemies = new HashSet<Enemy>();

		private string damageSource;

		private DamageContainer reuseDc;

		protected override void OnInitOrAmountChanged()
		{
			poisonChanceTotal = poisonChancePerAmount * amount;
			poisonBurstChanceTotal = poisonBurstChancePerAmount * amount;
			damageRatio = .5f + damageRatioPerAmount * amount;
		}

		public override void ProcOnHitEffects(DamageContainer dc)
		{
			if (dc == null || dc.enemy == null || poisonChanceTotal <= 0f) return;
			int stacks = poisonChancePerAmount > 0f ? Mathf.FloorToInt(poisonChanceTotal) : 0;
			float fractional = poisonChanceTotal - stacks;
			if (fractional > 0f && ItemUtility.TryProc(dc.procCoefficient, fractional)) stacks++;
			if (stacks <= 0) return;
			dc.enemy.AddDebuff(EDebuff.Poison, dc, 5f, stacks);
			if (!ItemUtility.TryProc(dc.procCoefficient, poisonBurstChanceTotal)) return;
			reuseDc.Reuse(0f, damageSource);
			reuseDc.damage = damageRatio * dc.damage;
			reuseDc.enemy = dc.enemy;
			dc.enemy.DamageFromPlayerOther(reuseDc);
			queuedExplosionEnemies.Add(dc.enemy);
			if (PoolManager.Instance == null || PoolManager.Instance.snekPool == null || MyPlayer.Instance == null || dc.enemy.collider == null) return;
			GameObject effect = PoolManager.Instance.snekPool.Get();
			if (effect == null) return;
			Vector3 point = dc.enemy.collider.ClosestPointOnBounds(MyPlayer.Instance.transform.position);
			Vector3 direction = MyPlayer.Instance.transform.position - dc.enemy.transform.position;
			direction.y = 0f;
			effect.transform.position = point + Vector3.up * 3f + direction.normalized * .75f;
		}

		public override bool HasOnHitEffectProc()
		{
			return true;
		}

		public ItemSnek(ItemInventory itemInventoryRef)
			: base(itemInventoryRef)
		{
			damageSource = EItem.Snek.ToString();
			reuseDc = new DamageContainer(0f, damageSource);
		}

		public override void Init()
		{
		}

		public override void Cleanup()
		{
		}

		public override void Tick()
		{
			foreach (Enemy enemy in queuedExplosionEnemies)
			{
				if (enemy == null || enemy.IsDeadOrDyingNextFrame() || !enemy.HasDebuff(EDebuff.Poison)) continue;
				EnemyDebuff poison = enemy.debuffs[EDebuff.Poison];
				int stacks = poison.GetStacks();
				if (enemy.debuffsToAdd.TryGetValue(EDebuff.Poison, out AddDebuffContainer pending))
				{
					stacks += pending.stacks;
					enemy.debuffsToAdd.Remove(EDebuff.Poison);
				}
				float damage = DebuffPoison.GetPoisonDamagePerTick(stacks) * poison.ticksLeft;
				enemy.RemoveDebuff(EDebuff.Poison, false);
				reuseDc.Reuse(0f, damageSource);
				reuseDc.damage = damage;
				reuseDc.damageEffect = EDamageEffect.Poison;
				enemy.DamageFromPlayerOther(reuseDc);
			}
			queuedExplosionEnemies.Clear();
		}

		public override void PreAttack(DamageContainer dc, StatComponents itemAttackModifier)
		{
		}

		public override bool HasPreAttackProc()
		{
			return false;
		}

		public override string GetDescription(LocalizedString localizedString)
		{
			return localizedString.GetLocalizedString(new Dictionary<string, object>
			{
				["value1"] = string.Format("+{0}", poisonChancePerAmount),
				["value2"] = string.Format("{0}%", poisonBurstChancePerAmount * 100f)
			});
		}
	}
}
