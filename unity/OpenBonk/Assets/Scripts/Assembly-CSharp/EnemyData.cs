using Actors.Enemies;
using Assets.Scripts.Game.Combat.EnemySpecialAttacks;
using Assets.Scripts._Data.MapsAndStages;
using UnityEngine;
using UnityEngine.Localization;

[CreateAssetMenu(menuName = "Me/Enemy", order = 1)]
public class EnemyData : ScriptableObject
{
	[Header("Meta")]
	public EEnemy enemyName;

	[Header("Rendering")]
	public Material material;

	public AnimatedMeshScriptableObject animation;

	public Vector3 rendererOffset;

	public Vector3 rendererRotationOffset;

	public float rendererScale;

	public float colliderRadius;

	public float overrideHeight;

	public Vector3 colliderCenter;

	[Header("Stats")]
	public int hp;

	public int damage;

	public int shield;

	public bool isPoison;

	public float knockbackResistance;

	public bool nukeProtection;

	[Header("Movement")]
	public float mass;

	public float speed;

	public bool isFlying;

	public float teleportCooldown;

	public bool isRunningFromPlayer;

	public float minStayAtDistance;

	public float maxStayAtDistance;

	[Header("Other")]
	public int xp;

	public float despawnTime;

	public bool canBeElite;

	public bool canBeExecuted;

	public float maxSuckTime;

	[Header("Special Attacks")]
	public EnemySpecialAttack[] specialAttacks;

	[Header("Spawning")]
	public EMap maps;

	public int minStage;

	public float creditCost;

	public float canSpawnAfterTime;

	public LocalizedString forceName;

	public string GetName()
	{
		return forceName != null && !forceName.IsEmpty ? forceName.GetLocalizedString() : Assets.Scripts.UI.Localization.LocalizationUtility.GetEnemyName(enemyName);
	}

	public int GetGold()
	{
		return 1;
	}

	public int GetXp()
	{
		return xp;
	}

	public int GetEliteXp()
	{
		return Mathf.RoundToInt(xp * Inventory__Items__Pickups.XpUtility.eliteEnemyXpMultiplier);
	}

	public float GetKnockback()
	{
		return 3f;
	}

	public float GetDamage()
	{
		return damage;
	}
}
