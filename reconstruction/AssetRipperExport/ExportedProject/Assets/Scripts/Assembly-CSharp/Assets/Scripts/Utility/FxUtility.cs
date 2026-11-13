using System.Collections.Generic;

namespace Assets.Scripts.Utility
{
	public static class FxUtility
	{
		public static Dictionary<EWeapon, float> weaponCooldowns = new Dictionary<EWeapon, float>();

		public static Dictionary<EWeapon, float> muzzleCooldowns = new Dictionary<EWeapon, float>();

		public static void Init()
		{
			GameManager.A_RunStarted += OnRunStarted;
		}

		public static void Cleanup()
		{
			GameManager.A_RunStarted -= OnRunStarted;
		}

		private static void OnRunStarted()
		{
			weaponCooldowns = new Dictionary<EWeapon, float>();
			muzzleCooldowns = new Dictionary<EWeapon, float>();
		}
	}
}
