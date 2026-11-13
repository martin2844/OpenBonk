using Assets.Scripts.Actors;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;
using Utility;
using UnityEngine;

namespace Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations
{
	public class ItemCampfire : ItemBase
	{
		private float healthRegenPerMinutePerAmount = 1100f;

		private float healthRegen;

		public Campfire campfire;

		private Vector3 campfirePos;

		private float setupTime = 0.6f;

		private float distThreshold = 1.75f;

		private float startCampfireTime;

		private bool isCampActive;

		protected override void OnInitOrAmountChanged()
		{
			healthRegen = healthRegenPerMinutePerAmount * amount;
		}

		public ItemCampfire(ItemInventory itemInventoryRef)
			: base(itemInventoryRef)
		{
		}

		public override void Init()
		{
		}

		public override void Cleanup()
		{
			RemoveCamp();
		}

		public override void Tick()
		{
			if (MyPlayer.Instance == null) return;
			Vector3 position = MyPlayer.Instance.transform.position;
			if (Vector3.Distance(campfirePos, position) > distThreshold)
			{
				campfirePos = position;
				startCampfireTime = MyTime.time + setupTime;
				if (isCampActive) RemoveCamp();
			}
			if (!isCampActive && MyTime.time > startCampfireTime) CreateCamp();
		}

		private void CreateCamp()
		{
			if (MyPlayer.Instance == null || MyPlayer.Instance.playerRenderer == null || GameManager.Instance == null) return;
			Vector3 position = RaycastUtility.RayToGround(campfirePos + MyPlayer.Instance.playerRenderer.transform.forward * 4f, 5f);
			if (position.sqrMagnitude < 1e-10f)
			{
				campfirePos = MyPlayer.Instance.transform.position;
				startCampfireTime = MyTime.time + setupTime;
				return;
			}
			if (campfire == null)
			{
				if (EffectManager.Instance == null || EffectManager.Instance.campfire == null) return;
				campfire = Object.Instantiate(EffectManager.Instance.campfire).GetComponent<Campfire>();
				if (campfire == null) return;
			}
			isCampActive = true;
			campfire.StartFire(position);
			SetStat(new StatModifier { stat = EStat.HealthRegen, modifyType = EStatModifyType.Flat, modification = healthRegen });
		}

		private void RemoveCamp()
		{
			if (!isCampActive) return;
			isCampActive = false;
			if (campfire != null) campfire.EndFire();
			SetStat(new StatModifier { stat = EStat.HealthRegen, modifyType = EStatModifyType.Flat, modification = 0f });
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
	}
}
