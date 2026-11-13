using System.Collections.Generic;
using Assets.Scripts.Actors;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Game.Combat;
using Assets.Scripts.Game.Combat.EnemyDebuffs;
using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Objects.Pooling;
using UnityEngine;

namespace Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations
{
	public class ItemDragonfire : ItemBase
	{
		public float procChancePerAmount;

		private float procChance;

		private float burnChancePerAmount;

		private float burnChance;

		private string damageSource;

		protected override void OnInitOrAmountChanged()
		{
			procChance = StatScaling.HyperbolicScaling(amount * procChancePerAmount, 1f, 0.5f);
			burnChance = StatScaling.HyperbolicScaling(amount * burnChancePerAmount, 1f, 0.5f);
		}

		public override void ProcOnHitEffects(DamageContainer dc)
		{
			if (dc.element == EElement.Fire) TryProcBurn(dc);
			if (!ItemUtility.TryProc(dc.procCoefficient, procChance)) return;
			GameObject obj = PoolManager.Instance.firefieldPool.Get();
			if (obj == null) return;
			Vector3 offset = UnityEngine.Random.insideUnitSphere;
			offset.y = 0f;
			float radius = Mathf.Clamp(6f * PlayerStats.GetStat(EStat.SizeMultiplier), 1f, 12f);
			float duration = Mathf.Clamp(2f * PlayerStats.GetStat(EStat.DurationMultiplier), 1f, 3f);
			obj.GetComponent<Firefield>().Set(dc.enemy.GetCenterPosition() + offset * 0.5f, dc.enemy.GetFeetPosition(), radius, duration, dc.damage, null, damageSource);
		}

		public override bool HasOnHitEffectProc()
		{
			return true;
		}

		private void TryProcBurn(DamageContainer dc, float overrideProcCoefficient = -1f)
		{
			float coefficient = overrideProcCoefficient == -1f ? dc.procCoefficient : overrideProcCoefficient;
			if (ItemUtility.TryProc(coefficient, burnChance)) dc.enemy.AddDebuff(EDebuff.Burn, dc, 3f);
		}

		public ItemDragonfire(ItemInventory itemInventoryRef)
			: base(itemInventoryRef)
		{
			procChancePerAmount = 0.15f;
			burnChancePerAmount = 0.15f;
			damageSource = EItem.Dragonfire.ToString();
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
