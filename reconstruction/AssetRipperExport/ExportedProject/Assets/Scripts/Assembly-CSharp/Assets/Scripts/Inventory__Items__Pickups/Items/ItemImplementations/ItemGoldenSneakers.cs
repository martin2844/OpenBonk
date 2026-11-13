using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.GoldAndMoney;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Utility;
using UnityEngine;

namespace Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations
{
	public class ItemGoldenSneakers : ItemBase
	{
		private float goldPerMeter;

		private float goldPerMeterBase = .1f;

		private float checkInterval = .5f;

		private float nextCheckTime;

		private Vector3 lastPos;

		private float accumulatedGold;

		public ItemGoldenSneakers(ItemInventory itemInventoryRef)
			: base(itemInventoryRef)
		{
		}

		protected override void OnInitOrAmountChanged()
		{
			goldPerMeter = goldPerMeterBase + (amount - 1) * (.5f * goldPerMeterBase);
			lastPos = MyPlayer.Instance.transform.position;
		}

		public override void Tick()
		{
			if (MyTime.time < nextCheckTime) return;
			nextCheckTime = MyTime.time + checkInterval;
			Vector3 position = MyPlayer.Instance.transform.position;
			accumulatedGold += Vector3.Distance(lastPos, position) * goldPerMeter;
			if (accumulatedGold >= 1f)
			{
				int amountToSpawn = Mathf.FloorToInt(accumulatedGold);
				accumulatedGold -= amountToSpawn;
				MoneyUtility.SpawnMoney(amountToSpawn, position);
			}
			lastPos = position;
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
	}
}
