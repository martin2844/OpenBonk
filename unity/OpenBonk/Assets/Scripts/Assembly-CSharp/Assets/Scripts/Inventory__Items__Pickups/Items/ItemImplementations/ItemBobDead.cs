using System.Collections.Generic;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;
using UnityEngine;

namespace Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations
{
	public class ItemBobDead : ItemBase
	{
		private string damageSource = EItem.BobDead.ToString();

		public const int maxGhosts = 80;

		private float unitsPerProjectile = 14f;

		private float minSpawnTime = .05f;

		private float nextCheckTime;

		private float accumulatedDistance;

		private Vector3 lastPos;

		protected override void OnInitOrAmountChanged()
		{
			if (MyPlayer.Instance != null) lastPos = MyPlayer.Instance.transform.position;
		}

		public override void Tick()
		{
			if (MyTime.time < nextCheckTime || MyPlayer.Instance == null) return;
			nextCheckTime = MyTime.time + minSpawnTime;
			Vector3 pos = MyPlayer.Instance.transform.position;
			accumulatedDistance += Vector3.Distance(lastPos, pos);
			if (accumulatedDistance >= unitsPerProjectile)
			{
				accumulatedDistance = 0f;
				for (int i = 0; i < amount; i++) SpawnGhost();
			}
			lastPos = pos;
		}

		private float GetDamage()
		{
			return MyPlayer.Instance.GetBaseDamage() * 1.5f;
		}

		private void SpawnGhost()
		{
			if (EffectManager.Instance != null && MyPlayer.Instance != null)
				EffectManager.Instance.SpawnGhostProjectile(GetDamage(), GetDuration(), damageSource);
		}

		private float GetDuration()
		{
			return PlayerStats.GetStat(EStat.DurationMultiplier) * 10f;
		}

		public ItemBobDead(ItemInventory itemInventoryRef)
			: base(itemInventoryRef)
		{
		}

		public override void ProcOnHitEffects(DamageContainer dc)
		{
		}

		public override bool HasOnHitEffectProc()
		{
			return false;
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

		protected override Dictionary<string, object> GetLocalizationKeys()
		{
			return new Dictionary<string, object> { ["value1"] = string.Format("{0}", unitsPerProjectile) };
		}
	}
}
