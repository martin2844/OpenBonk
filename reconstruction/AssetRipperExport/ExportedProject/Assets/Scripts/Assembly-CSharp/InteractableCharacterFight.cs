using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Managers;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;
using UnityEngine;
using UnityEngine.Localization;

public class InteractableCharacterFight : BaseInteractable
{
	public CharacterData character;

	public EnemyData enemyData;

	private bool done;

	public LocalizedString interactString;

	public GameObject chargeFx;

	public GameObject explodeFx;

	public Material enemyMat2;

	public Enemy myEnemy;

	private void Awake()
	{
		Enemy.A_EnemyDied += OnEnemyDied;
	}

	private new void OnDestroy()
	{
		Enemy.A_EnemyDied -= OnEnemyDied;
		base.OnDestroy();
	}

	private void OnEnemyDied(Enemy enemy, DamageContainer dc)
	{
		if (myEnemy == null || enemy != myEnemy) return;
		if (character != null && character.GetUnlockRequirement() != null)
			MyAchievements.TryUnlock(character.GetUnlockRequirement().internalName);
		if (enemyMat2 != null && myEnemy.renderer != null && myEnemy.renderer.sharedMaterials.Length > 0)
			myEnemy.renderer.sharedMaterials = new[] { myEnemy.renderer.sharedMaterials[0] };
	}

	public override bool Interact()
	{
		if (done) return false;
		done = true;
		chargeFx.SetActive(true);
		chargeFx.transform.SetParent(null);
		Invoke(nameof(SpawnEnemy), 1f);
		return true;
	}

	private void SpawnEnemy()
	{
		explodeFx.SetActive(true);
		explodeFx.transform.SetParent(null);
		myEnemy = EnemyManager.Instance.SpawnBoss(enemyData.enemyName, 0, EEnemyFlag.Boss, transform.position, 1f);
		if (myEnemy == null) return;
		myEnemy.teleportTime = 0.25f;
		if (character != null && MyAchievements.IsUnlocked(character.achievementRequirement)) myEnemy.SetSwarmMultiplierHp(1.5f);
		if (enemyMat2 != null && myEnemy.renderer != null)
			myEnemy.renderer.sharedMaterials = new[] { myEnemy.renderer.sharedMaterial, new Material(enemyMat2) };
		gameObject.SetActive(false);
	}

	public override string GetInteractString()
	{
		return interactString.GetLocalizedString();
	}
}
