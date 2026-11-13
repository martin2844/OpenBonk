using System;
using System.Collections.Generic;
using Assets.Scripts.Actors;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Game.Combat;
using Assets.Scripts.Game.Combat.EnemyDebuffs;
using Assets.Scripts.Objects.Pooling;
using UnityEngine;

namespace Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations
{
	public class ItemIceCube : ItemBase
	{
		public float procChancePerAmount;

		private float procChance;

		private float freezeChancePerAmount;

		private float freezeChance;

		public float damageRatio;

		public float damageRatioPerAmount;

		private string damageSource;

		private DamageContainer reuseDc;

		public static Action A_FreezeEnemy;

		public ItemIceCube(ItemInventory itemInventoryRef)
			: base(itemInventoryRef)
		{
			procChancePerAmount = 0.2f;
			freezeChancePerAmount = 0.4f;
			damageRatio = 0.8f;
			damageRatioPerAmount = 0.4f;
			damageSource = EItem.IceCube.ToString();
			reuseDc = new DamageContainer(0f, damageSource);
		}

		protected override void OnInitOrAmountChanged()
		{
			procChance = StatScaling.HyperbolicScaling(amount * procChancePerAmount, 1f, 0.5f);
			freezeChance = StatScaling.HyperbolicScaling(amount * freezeChancePerAmount, 1f, 0.6f);
			damageRatio = amount * damageRatioPerAmount;
		}

		public override void ProcOnHitEffects(DamageContainer dc)
		{
			if (dc.element == EElement.Ice) TryProcFreeze(dc);
			if (!ItemUtility.TryProc(dc.procCoefficient, procChance)) return;
			GameObject effect = PoolManager.Instance.freezeFxPool.Get();
			if (effect != null)
			{
				Vector3 offset = UnityEngine.Random.insideUnitSphere;
				offset.y = 0f;
				effect.transform.position = dc.enemy.GetCenterPosition() + offset * 0.5f;
			}
			reuseDc.damage = damageRatio * dc.damage;
			reuseDc.enemy = dc.enemy;
			reuseDc.element = EElement.Ice;
			dc.enemy.DamageFromPlayerOther(reuseDc);
			TryProcFreeze(reuseDc, 1f);
		}

		public override bool HasOnHitEffectProc()
		{
			return true;
		}

		private void TryProcFreeze(DamageContainer dc, float overrideProcCoefficient = -1f)
		{
			float coefficient = overrideProcCoefficient == -1f ? dc.procCoefficient : overrideProcCoefficient;
			if (!ItemUtility.TryProc(coefficient, freezeChance)) return;
			dc.enemy.AddDebuff(EDebuff.Freeze, dc, 3f);
			A_FreezeEnemy?.Invoke();
		}

		public override void Init()
		{
		}

		public override void Cleanup()
		{
		}

		public override void Tick()
		{
		}

		public override void PreAttack(DamageContainer dc, StatComponents itemAttackModifier)
		{
		}

		public override bool HasPreAttackProc()
		{
			return false;
		}

		protected override Dictionary<string, object> GetLocalizationKeys()
		{
			return new Dictionary<string, object> { ["value1"] = string.Format("{0}%", procChancePerAmount * 100f) };
		}
	}
}
