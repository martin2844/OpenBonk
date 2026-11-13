using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;

namespace Assets.Scripts.Inventory__Items__Pickups.Weapons.WeaponPassives
{
	public class WeaponPassiveDice : WeaponPassive
	{
		private int stacks;

		private float critPer6 = .005f;

		private string movingStatName = "DiceCritChance";

		private static float maxRollsUpgradesPerMinute = 100f;

		private float rollCooldown;

		private float nextRollTime;

		private float accumulatedCritChance;

		public override void Init()
		{
			ProjectileDice.A_RollSix += OnStackAdded;
		}

		public override void Cleanup()
		{
			ProjectileDice.A_RollSix -= OnStackAdded;
		}

		private void OnStackAdded()
		{
			if (MyTime.time < nextRollTime) return;
			nextRollTime = MyTime.time + rollCooldown;
			accumulatedCritChance = ++stacks * critPer6;
			SetStat(new StatModifier { stat = EStat.CritChance, modifyType = EStatModifyType.Flat, modification = accumulatedCritChance });
		}

		public WeaponPassiveDice(WeaponBase weaponBase)
			: base(weaponBase)
		{
			rollCooldown = maxRollsUpgradesPerMinute / 60f;
		}
	}
}
