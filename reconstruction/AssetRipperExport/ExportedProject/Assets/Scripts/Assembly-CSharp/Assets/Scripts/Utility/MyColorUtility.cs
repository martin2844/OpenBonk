using System.Collections.Generic;
using Assets.Scripts.Game.Combat;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts._Data.Progression;
using UnityEngine;

namespace Assets.Scripts.Utility
{
	public static class MyColorUtility
	{
		public const string white = "#ffffff";

		public static string positiveColorString = "#24ff36";

		public static string negativeColorString = "#ff3224";

		public static Color aegisColor = RankColor("#ffcd29");

		public static Color weakAegisColor = RankColor("#ffe694");

		public static Color bleedColor = RankColor("#a6001e");

		public static Color tier1Color = RankColor("#3EBE71");

		public static Color tier2Color = RankColor("#177FFF");

		public static Color tier3Color = RankColor("#FF8100");

		private static Dictionary<EStatCategory, Color> statCategoryColors = new Dictionary<EStatCategory, Color>
		{
			{ EStatCategory.Offensive, RankColor("#FF4500") },
			{ EStatCategory.Defensive, RankColor("#00CED1") },
			{ EStatCategory.Movement, RankColor("#32CD32") },
			{ EStatCategory.Utility, RankColor("#FFD700") },
			{ EStatCategory.Coolness, RankColor("#E50069") },
			{ EStatCategory.Difficulty, RankColor("#4008FF") }
		};

		private static Color hasteColor = RankColor("#00B4FF");

		private static Color magnetColor = RankColor("#FF7F19");

		private static Color shieldColor = RankColor("#00FF41");

		private static Color timeFreezeColor = RankColor("#00FF97");

		private static Color healthColor = RankColor("#FF1800");

		private static Color rageColor = RankColor("#FF42F0");

		private static Color stonksColor = RankColor("#FFF814");

		private static Color newColor = RankColor("#FFFFFF");

		private static Color commonColor = RankColor("#4dff97");

		private static Color uncommonColor = RankColor("#0ca7fa");

		private static Color rareColor = RankColor("#FF00F4");

		private static Color epicColor = RankColor("#ff2929");

		private static Color legendaryColor = RankColor("#FFE300");

		public static Color interactOutlineColor = RankColor("#ffd745");

		public static Color interactDisabledOutlineColor = RankColor("#bd2222");

		public static string requirementCompletedColor = "#0ceb2d";

		public static string requirementMissingColor = "#ff6a38";

		public static Color evadeColor = new Color(0f, 1f, 1f, 1f);

		public static Color evadePhantomColor = RankColor("#f06bff");

		public static Color critMegaColor = RankColor("#ff6d12");

		public static Color bonkColor = RankColor("#ff0055");

		public static Color poisonColor = RankColor("#d063ff");

		public static Color fireColor = RankColor("#fc9d3d");

		public static Color executeColor = RankColor("#b50000");

		public static Color echoColor = RankColor("#42ff91");

		public static Color bloodmarkColor = RankColor("#ff0000");

		private static Color warningRed = RankColor("#FF0C00");

		private static Color warningBlue = RankColor("#4275f5");

		private static Color warningMagenta = RankColor("#d91cff");

		private static Color warningWhite = RankColor("#ffffff");

		private static Color warningYellow = RankColor("#fff82b");

		private static Color easyColor = RankColor("#CCFFD0");

		private static Color mediumColor = RankColor("#009DFF");

		private static Color hardColor = RankColor("#FF1027");

		private static Color cookedColor = RankColor("#FFE000");

		private static Color rankTier1Color = RankColor("#E7E7E7");

		private static Color rankTier2Color = RankColor("#31FF6C");

		private static Color rankTier3Color = RankColor("#2AC8FF");

		private static Color rankTier4Color = RankColor("#FF25B5");

		private static Color rankTier5Color = RankColor("#FF3928");

		private static Color rankTier6Color = RankColor("#FFED28");

		private static Color RankColor(string hex)
		{
			ColorUtility.TryParseHtmlString(hex, out var color);
			return color;
		}

		public static void Init()
		{
		}

		public static Color GetStatCategoryColor(EStatCategory statCategory)
		{
			return statCategoryColors[statCategory];
		}

		private static Color StringToColor(string s)
		{
			ColorUtility.TryParseHtmlString(s, out var color);
			return color;
		}

		public static Color PickupToColor(EPickup ePickup)
		{
			Color color;
			switch (ePickup)
			{
				case EPickup.Health: color = healthColor; break;
				case EPickup.Nuke: color = Color.black; break;
				case EPickup.Time: color = timeFreezeColor; break;
				case EPickup.Shield: color = shieldColor; break;
				case EPickup.Rage: color = rageColor; break;
				case EPickup.Haste: color = hasteColor; break;
				case EPickup.Stonks: color = stonksColor; break;
				case EPickup.Magnet: color = magnetColor; break;
				default: color = Color.white; break;
			}
			return Color.Lerp(color, Color.white, .2f);
		}

		public static Color RarityToColor(ERarity rarity)
		{
			switch (rarity)
			{
				case ERarity.New: return newColor;
				case ERarity.Uncommon: return uncommonColor;
				case ERarity.Rare: return rareColor;
				case ERarity.Epic: return epicColor;
				case ERarity.Legendary: return legendaryColor;
				default: return commonColor;
			}
		}

		public static Color GetRarityColorBackground(ERarity rarity)
		{
			return Color.Lerp(RarityToColor(rarity), Color.black, .8f);
		}

		public static Color GetRarityColorBackground(EItemRarity rarity)
		{
			return Color.Lerp(GetItemRarityColor(rarity), Color.black, .8f);
		}

		public static Color GetItemRarityColor(EItemRarity rarity)
		{
			switch (rarity)
			{
				case EItemRarity.Common: return commonColor;
				case EItemRarity.Rare: return uncommonColor;
				case EItemRarity.Epic: return rareColor;
				case EItemRarity.Legendary: return legendaryColor;
				case EItemRarity.Corrupted: return Color.red;
				case EItemRarity.Quest: return Color.white;
				default: return Color.gray;
			}
		}

		public static Color GetDamageEffectColor(EDamageEffect effect)
		{
			switch (effect)
			{
				case EDamageEffect.Bonk: return bonkColor;
				case EDamageEffect.Megacrit: return critMegaColor;
				case EDamageEffect.Poison: return poisonColor;
				case EDamageEffect.Fire: return fireColor;
				case EDamageEffect.Execute: return executeColor;
				case EDamageEffect.Cursed: return new Color(.5f, .5f, .5f, 1f);
				case EDamageEffect.Echo: return echoColor;
				case EDamageEffect.Bloodmark: return bloodmarkColor;
				default: throw new System.ArgumentOutOfRangeException(nameof(effect), effect, "Unknown damage effect");
			}
		}

		public static string GetStatTextColor(bool isPositive)
		{
			return isPositive ? "#24ff36" : "#ff3224";
		}

		public static Color GetRedToGreenGradient(float t)
		{
			return Color.HSVToRGB(Mathf.Clamp01(t) / 3f, 1f, 1f);
		}

		public static Color GetHealthBarColor(EHpBarColor color)
		{
			switch (color)
			{
				case EHpBarColor.White: return Color.white;
				case EHpBarColor.Yellow: return new Color(1f, 235f / 255f, 4f / 255f, 1f);
				case EHpBarColor.Blue: return Color.blue;
				case EHpBarColor.Magenta: return Color.magenta;
				default: return Color.red;
			}
		}

		public static Color GetWarningColor(EHpBarColor color)
		{
			switch (color)
			{
				case EHpBarColor.White: return warningWhite;
				case EHpBarColor.Yellow: return warningYellow;
				case EHpBarColor.Blue: return warningBlue;
				case EHpBarColor.Magenta: return warningMagenta;
				default: return warningRed;
			}
		}

		public static Color GetTierColor(int tier)
		{
			return tier == 1 ? tier2Color : tier == 2 ? tier3Color : tier1Color;
		}

		public static Color GetRankColor(int rank)
		{
			switch (Mathf.Clamp(rank / 40, 0, 5))
			{
				case 1: return rankTier2Color;
				case 2: return rankTier3Color;
				case 3: return rankTier4Color;
				case 4: return rankTier5Color;
				case 5: return rankTier6Color;
				default: return rankTier1Color;
			}
		}

		public static Color DifficultyToColor(EAchievementDifficulty difficulty)
		{
			switch (difficulty)
			{
				case EAchievementDifficulty.Easy: return easyColor;
				case EAchievementDifficulty.Medium: return mediumColor;
				case EAchievementDifficulty.Hard: return hardColor;
				case EAchievementDifficulty.Cooked: return cookedColor;
				default: return commonColor;
			}
		}

		public static Color GetHexToColor(string hex)
		{
			ColorUtility.TryParseHtmlString(hex, out var color);
			return color;
		}

		public static string ColorToHex(Color color)
		{
			return ColorUtility.ToHtmlStringRGBA(color);
		}
	}
}
