using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Stats;

namespace Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations
{
	public class ItemShatteredWisdom : ItemBase
	{
		private static float damage;

		public static float procCoefficient = .5f;

		protected override void OnInitOrAmountChanged()
		{
			damage = amount * MyPlayer.Instance.GetBaseDamage();
		}

		public static float GetDamage()
		{
			return damage;
		}

		public ItemShatteredWisdom(ItemInventory itemInventoryRef)
			: base(itemInventoryRef)
		{
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

		public override void ProcOnHitEffects(DamageContainer dc)
		{
		}

		public override bool HasOnHitEffectProc()
		{
			return false;
		}
	}
}
