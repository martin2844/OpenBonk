using System.Collections.Generic;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Managers;
using Assets.Scripts.MapGeneration;
using Assets.Scripts.Game.Spawning;
using Assets.Scripts.Utility;
using UnityEngine;
using UnityEngine.Localization;

public class InteractableEgg : BaseInteractable
{
	public class FrogContainer
	{
		public Transform target;

		public float lastFoundRandomPositionTime;

		public float lockDirectionUntilTime;

		public FrogContainer(Transform t)
		{
			target = t;
		}
	}

	public EnemyData frogEnemyData;

	public MyAchievement unlockAchievement;

	public LocalizedString localizationEgg;

	public MeshRenderer renderer;

	public Collider collider;

	public Material frogMinimapMaterial;

	private List<Enemy> frogEnemies;

	private Dictionary<Enemy, FrogContainer> frogTargets;

	public GameObject breakFx;

	private bool done;

	private float playerDistanceThreshold = 40f;

	private float edgeDistanceThreshold = 10f;

	private float lockDirectionTime = 10f;

	private float newRandomDirectionInterval = 8f;

	public InteractableEgg() { showOutline = true; }

	private void Awake()
	{
		Enemy.A_EnemyReleasedFromPool += OnEnemyReleasedFromPool;
	}

	private new void OnDestroy()
	{
		Enemy.A_EnemyReleasedFromPool -= OnEnemyReleasedFromPool;
		base.OnDestroy();
	}

	public override bool Interact()
	{
		if (done) return false;
		done = true;
		breakFx.SetActive(true);
		renderer.enabled = false;
		collider.enabled = false;
		frogEnemies = new List<Enemy>();
		frogTargets = new Dictionary<Enemy, FrogContainer>();
		for (int i = 1; i < 4; i++)
		{
			Enemy frog = EnemyManager.Instance.SpawnEnemy(frogEnemyData, transform.position + Random.insideUnitSphere * 2f, 0, true, EEnemyFlag.None, false, 1f);
			frog.teleportTime = 0.05f;
			frogEnemies.Add(frog);
			Transform target = new GameObject("FrogTarget" + i).transform;
			frogTargets.Add(frog, new FrogContainer(target));
			frog.FollowTarget(target);
			target.position = frog.transform.position;
			MyPlayer.Instance.minimapCameraScript.AddEnemyMinimapIcon(frog, frogMinimapMaterial, 0.8f);
			EffectManager.Instance.AttachEnemyHpBar(frog);
		}
		return true;
	}

	private void Update()
	{
		if (frogEnemies == null || frogEnemies.Count == 0) return;
		foreach (Enemy frog in frogEnemies)
		{
			if (frog.state == EEnemyState.Default) frog.state = EEnemyState.FollowTarget;
			FrogContainer container = frogTargets[frog];
			Vector3 position = frog.transform.position;
			Vector3 playerPosition = MyPlayer.Instance.transform.position;
			if (Vector3.Distance(position, playerPosition) > playerDistanceThreshold)
			{
				Vector3 flatFrog = position; flatFrog.y = 0f;
				Vector3 flatTarget = container.target.position; flatTarget.y = 0f;
				if (Vector3.Distance(flatFrog, flatTarget) < 2f || MyTime.time >= container.lastFoundRandomPositionTime + newRandomDirectionInterval)
				{
					container.target.position = SpawnPositions.GetRandomSpawnPositionOnMap();
					container.lastFoundRandomPositionTime = MyTime.time;
				}
				continue;
			}
			Vector3 direction = (position - playerPosition).normalized;
			if (container.lockDirectionUntilTime > MyTime.time)
			{
				direction = MapInfo.mapCenter - position;
				direction.y = 0f;
			}
			else if (position.x > MapInfo.mapBoundsUpper.x - 10f || position.z > MapInfo.mapBoundsUpper.z - 10f || position.x < MapInfo.mapBoundsLower.x + 10f || position.z < MapInfo.mapBoundsLower.z + 10f)
			{
				container.lockDirectionUntilTime = MyTime.time + lockDirectionTime;
			}
			container.target.position = position + direction.normalized * 20f;
		}
	}

	private void OnEnemyReleasedFromPool(Enemy enemy)
	{
		if (frogEnemies == null || !frogEnemies.Remove(enemy) || frogEnemies.Count != 0) return;
		if (MyAchievements.IsUnlocked(unlockAchievement))
			EffectManager.Instance.SpawnChest(EffectManager.Instance.openChestNormal, enemy.GetHeadPosition() + Vector3.up * 2f);
		else MyAchievements.TryUnlock(unlockAchievement.internalName);
	}

	public override string GetInteractString()
	{
		return localizationEgg.GetLocalizedString();
	}

	public override bool CanInteract()
	{
		return !done;
	}
}
