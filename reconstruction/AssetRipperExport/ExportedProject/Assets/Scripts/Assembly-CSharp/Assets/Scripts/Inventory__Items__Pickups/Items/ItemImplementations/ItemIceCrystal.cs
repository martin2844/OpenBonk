using System.Collections.Generic;
using Assets.Scripts.Actors;
using Assets.Scripts.Game.Combat.EnemyDebuffs;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using UnityEngine;

namespace Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations
{
	public class ItemIceCrystal : ItemBase
	{
		private float freezeTime;

		public float procChancePerAmount = .075f;

		private float procChance;

		public ItemIceCrystal(ItemInventory itemInventoryRef)
			: base(itemInventoryRef)
		{
		}

		protected override void OnInitOrAmountChanged()
		{
			procChance = StatScaling.HyperbolicScaling(amount * procChancePerAmount, 1f, .75f);
			freezeTime = Mathf.Clamp(2.5f + amount * .1f, 0f, 6f);
		}

		public override void ProcOnHitEffects(DamageContainer dc)
		{
			if (ItemUtility.TryProc(dc.procCoefficient, procChance)) dc.enemy.AddDebuff(EDebuff.Freeze, dc, freezeTime, 1);
		}

		public override bool HasOnHitEffectProc()
		{
			return true;
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
