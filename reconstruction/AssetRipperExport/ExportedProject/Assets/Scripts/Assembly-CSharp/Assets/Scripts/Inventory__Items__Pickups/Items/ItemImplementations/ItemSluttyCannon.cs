using System.Collections.Generic;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Objects.Pooling;
using UnityEngine;

namespace Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations
{
	public class ItemSluttyCannon : ItemBase
	{
		private float procChance;

		public float procChancePerAmount = 0.2f;

		public float damageRatio = 1f;

		public float damageRatioPerAmount = 0.4f;

		private string damageSource = EItem.SluttyCannon.ToString();

		private Dictionary<GameObject, Rocket> rocketLookup = new Dictionary<GameObject, Rocket>(200);

		private int maxProcsPerTick = 4;

		private int numProcsThisTick;

		protected override void OnInitOrAmountChanged()
		{
			procChance = Mathf.Min(0.6f, StatScaling.HyperbolicScaling(amount * procChancePerAmount, 1f, 0.5f));
			damageRatio = 1f + amount * damageRatioPerAmount;
		}

		public override void ProcOnHitEffects(DamageContainer dc)
		{
			if (numProcsThisTick >= maxProcsPerTick || dc == null || !ItemUtility.TryProc(dc.procCoefficient, procChance)) return;
			numProcsThisTick++;
			if (PoolManager.Instance == null || PoolManager.Instance.rocketPool == null || MyPlayer.Instance == null) return;
			GameObject obj = PoolManager.Instance.rocketPool.Get();
			if (obj == null) return;
			if (!rocketLookup.TryGetValue(obj, out Rocket projectile))
			{
				projectile = obj.GetComponent<Rocket>();
				rocketLookup.Add(obj, projectile);
			}
			if (projectile != null)
				projectile.Set(MyPlayer.Instance.transform.position + Vector3.up, damageRatio * dc.damage, 0f, null, true, damageSource);
		}

		public override bool HasOnHitEffectProc()
		{
			return true;
		}

		public override void Tick()
		{
			numProcsThisTick = 0;
		}

		public override void Init()
		{
		}

		public override void Cleanup()
		{
		}

		public override void PreAttack(DamageContainer dc, StatComponents itemAttackModifier)
		{
		}

		public override bool HasPreAttackProc()
		{
			return false;
		}

		public ItemSluttyCannon(ItemInventory itemInventoryRef)
			: base(itemInventoryRef)
		{
		}

		protected override Dictionary<string, object> GetLocalizationKeys()
		{
			return new Dictionary<string, object> { ["value1"] = string.Format("{0}%", procChancePerAmount * 100f) };
		}
	}
}
