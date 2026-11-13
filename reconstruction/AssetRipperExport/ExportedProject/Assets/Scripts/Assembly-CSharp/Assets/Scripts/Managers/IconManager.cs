using Assets.Scripts.Game.Combat.EnemyDebuffs;
using UnityEngine;

namespace Assets.Scripts.Managers
{
	public class IconManager : MonoBehaviour
	{
		public Texture poisonIcon;

		public Texture burnIcon;

		public Texture thornsIcon;

		public Texture echoIcon;

		public Texture bloodmarkIcon;

		public Texture zapIcon;

		public Texture shadowStepIcon;

		public Texture bullseyeIcon;

		public Texture questionMark;

		public Texture[] rankIcons;

		public Texture rankFrameIcon;

		public static IconManager Instance;

		private void Awake()
		{
			if (Instance != null) { Destroy(gameObject); return; }
			Instance = this;
		}

		public Texture GetDebuffIcon(EDebuff debuff)
		{
			switch (debuff)
			{
				case EDebuff.Poison: return poisonIcon;
				case EDebuff.Burn: return burnIcon;
				case EDebuff.Echo: return echoIcon;
				case EDebuff.Bloodmark: return bloodmarkIcon;
				default: return null;
			}
		}
	}
}
