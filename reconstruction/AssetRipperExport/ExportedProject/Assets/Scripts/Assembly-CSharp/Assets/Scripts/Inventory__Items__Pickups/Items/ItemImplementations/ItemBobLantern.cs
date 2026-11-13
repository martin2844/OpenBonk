using Assets.Scripts.Actors;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using UnityEngine;
using UnityEngine.Localization;
using System.Collections.Generic;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Utility;

namespace Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations
{
	public class ItemBobLantern : ItemBase
	{
		private float cooldownMin = 5f;

		private float cooldownMax = 45f;

		private float cooldownReductionPerAmount = 3f;

		private float cooldown;

		private float nextExplodeTime;

		private float radius;

		private float radiusMin = 50f;

		private float radiusMax = 250f;

		private float radiusPerAmount = 10f;

		private GameObject explosionPrefab;

		protected override void OnInitOrAmountChanged()
		{
			cooldown = Mathf.Clamp(cooldownMax - cooldownReductionPerAmount * amount, cooldownMin, cooldownMax);
			nextExplodeTime = MyTime.time + 2f;
			radius = Mathf.Clamp(radiusMin + radiusPerAmount * amount, radiusMin, radiusMax);
			RefreshExplosionSize();
		}

		public override void Tick()
		{
			if (MyTime.time >= nextExplodeTime) Explode();
		}

		private void RefreshExplosionSize()
		{
			if (explosionPrefab != null) explosionPrefab.GetComponent<LanternExplosion>()?.SetRadius(radius);
		}

		private void Explode()
		{
			nextExplodeTime = MyTime.time + cooldown;
			if (explosionPrefab == null)
			{
				if (EffectManager.Instance == null || EffectManager.Instance.lanternExplosion == null) return;
				explosionPrefab = Object.Instantiate(EffectManager.Instance.lanternExplosion);
				var killer = explosionPrefab.GetComponent<KillEnemyTrigger>();
				if (killer != null) killer.customDamageSource = EItem.BobsLantern.ToString();
				RefreshExplosionSize();
			}
			if (MyPlayer.Instance == null) return;
			explosionPrefab.transform.position = MyPlayer.Instance.transform.position;
			explosionPrefab.SetActive(true);
		}

		public ItemBobLantern(ItemInventory itemInventoryRef)
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
			return localizedString.GetLocalizedString(new Dictionary<string, object> { ["value1"] = string.Format("{0}", cooldownMax), ["value2"] = string.Format("{0}", cooldownReductionPerAmount), ["value3"] = string.Format("{0}", radiusPerAmount) });
		}
	}
}
