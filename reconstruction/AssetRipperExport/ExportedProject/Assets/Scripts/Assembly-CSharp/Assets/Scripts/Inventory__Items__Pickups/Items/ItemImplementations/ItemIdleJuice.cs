using Assets.Scripts.Actors;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using UnityEngine;
using UnityEngine.Localization;
using System.Collections.Generic;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;

namespace Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations
{
	public class ItemIdleJuice : ItemBase
	{
		private float damagePerAmount = 1f;

		private float maxDamage;

		private float damagePerSecond = .04f;

		private Vector3 campfirePos;

		private float setupTime = .6f;

		private float distThreshold = 1.75f;

		private float startCampfireTime;

		private bool isCampActive;

		private float currentDamage;

		private float nextUpdateDamageTime;

		private float updateDamageInterval = 1f;

		protected override void OnInitOrAmountChanged()
		{
			maxDamage = amount * damagePerAmount;
			// A reduced stack count can lower the cap while the camp is active.
			if (currentDamage > maxDamage)
			{
				currentDamage = maxDamage;
				SetStat(new StatModifier { stat = EStat.DamageMultiplier, modifyType = EStatModifyType.Flat, modification = currentDamage });
			}
		}

		public override void Tick()
		{
			if (MyPlayer.Instance == null) return;
			if (Vector3.Distance(campfirePos, MyPlayer.Instance.transform.position) > distThreshold)
			{
				campfirePos = MyPlayer.Instance.transform.position;
				startCampfireTime = MyTime.time + setupTime;
				if (isCampActive) RemoveCamp();
			}
			if (!isCampActive && MyTime.time > startCampfireTime) CreateCamp();
			if (isCampActive && currentDamage < maxDamage && MyTime.time > nextUpdateDamageTime)
			{
				nextUpdateDamageTime = MyTime.time + updateDamageInterval;
				currentDamage = Mathf.Min(currentDamage + damagePerSecond, maxDamage);
				SetStat(new StatModifier { stat = EStat.DamageMultiplier, modifyType = EStatModifyType.Flat, modification = currentDamage });
			}
		}

		private void CreateCamp()
		{
			currentDamage = 0f;
			campfirePos = MyPlayer.Instance.transform.position;
			startCampfireTime = MyTime.time + setupTime;
			isCampActive = true;
		}

		private void RemoveCamp()
		{
			isCampActive = false;
			SetStat(new StatModifier { stat = EStat.DamageMultiplier, modifyType = EStatModifyType.Flat, modification = 0f });
			currentDamage = 0f;
		}

		public ItemIdleJuice(ItemInventory itemInventoryRef)
			: base(itemInventoryRef)
		{
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

		public override void ProcOnHitEffects(DamageContainer dc)
		{
		}

		public override bool HasOnHitEffectProc()
		{
			return false;
		}

		public override string GetDescription(LocalizedString localizedString)
		{
			return localizedString.GetLocalizedString(new Dictionary<string, object> { ["value1"] = string.Format("+{0}%", damagePerAmount * 100f) });
		}
	}
}
